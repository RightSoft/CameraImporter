using CameraImporter.Model;
using CameraImporter.Model.Genetec;
using CameraImporter.Shared.Interface;
using CameraImporter.SystemSpecific.Genetec;
using CameraImporter.SystemSpecific.Genetec.Interface;
using CameraImporter.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CameraImporter.Shared
{
    public class FileProcessor : IFileProcessor
    {
        private readonly IFileLoader _fileLoader;
        private readonly ICsvToCameraParser _csvToCameraParser;
        private readonly ILogger _logger;
        private readonly IGenetecSdkWrapper _genetecSdkWrapper;

        private int _processStepCount;
        private SettingsData _settingsData;
        private List<GenetecCamera> _cameraListToBeProcessed;
        private List<GenetecCamera> _existingCamerasToBeUpdated;

        public event EventHandler<int> ProgressBarStepsChanged;
        public event EventHandler<int> ProgressBarMaximumStepsChanged;
        public event EventHandler<List<EntityModel>> AvailableArchiversFound;
        public event EventHandler<List<GenetecCamera>> ExistingCameraListFound;
        public event EventHandler<ApplicationStateEnum> ApplicationStateChanged;

        public FileProcessor(IFileLoader fileLoader,
            ICsvToCameraParser csvToCameraParser,
            ILogger logger,
            IGenetecSdkWrapper genetecSdkWrapper)
        {
            Debug.WriteLine("file processor");
            _fileLoader = fileLoader;
            _csvToCameraParser = csvToCameraParser;
            _logger = logger;
            _genetecSdkWrapper = genetecSdkWrapper;

            _genetecSdkWrapper.IsLoggedIn += OnLoggedInChanged;
            _genetecSdkWrapper.AvailableArchiversFound += OnAvailableArchiversFound;
            _genetecSdkWrapper.ExistingCameraListFound += OnExistingCameraListFound;
            _genetecSdkWrapper.Init();
        }

        private void OnExistingCameraListFound(object sender, ExistingCameraListFoundEventArgs e)
        {
            if (!e.IsExistingCamerasFound)
            {
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                return;
            }

            List<GenetecCamera> alreadyExistingCameras =
                _genetecSdkWrapper.CheckIfImportedCamerasExists(_cameraListToBeProcessed, _logger);

            if (alreadyExistingCameras.Any())
            {
                foreach (var alreadyExistingCamera in alreadyExistingCameras)
                {
                    _cameraListToBeProcessed.Remove(alreadyExistingCamera);
                }

                ExistingCameraListFound?.Invoke(this, alreadyExistingCameras);
            }
        }

        private void OnAvailableArchiversFound(object sender, AvailableArchiversFoundEventArgs e)
        {
            if (!e.IsArchiversFound)
            {
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                _logger.Log("No Archiver found. Add an Archiver to proceed", LogLevel.Error);
                return;
            }

            if (e.AvailableArchivers.Count == 1)
            {
                _logger.Log("Only one Archiver found. The import will automatically continue using this Archiver", LogLevel.Info);
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.CheckingExistingCameras, 1);
                _genetecSdkWrapper.FetchAvailableCameras();
            }

            if (e.AvailableArchivers.Count > 1)
            {
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                _logger.Log("Multiple Archivers found. Please select which Archiver you want to proceed", LogLevel.Warning);
                return;
            }

            AvailableArchiversFound?.Invoke(this, e.AvailableArchivers);
        }

        private void OnLoggedInChanged(object sender, IsLoggedInEventArgs e)
        {
            LogLevel logLevel = LogLevel.Info;

            if (!e.IsLoggedIn) { logLevel = LogLevel.Error; }

            _logger.Log(e.Message, logLevel);
            ProgressBarStepsChanged?.Invoke(this, ++_processStepCount);

            if (!e.IsLoggedIn)
            {
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                return;
            }

            _genetecSdkWrapper.FetchAvailableArchivers();
        }

        public void Process(SettingsData settingsData)
        {
            _settingsData = settingsData;
            _existingCamerasToBeUpdated = new List<GenetecCamera>();

            try
            {
                if (!TryParseCameras())
                {
                    return;
                }

                IncreaseCurrentProgressBarState();
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.LoggingIn, 1);

                if (ValidateLoginInformation(settingsData))
                {
                    Login(settingsData);
                }
                else
                {

                    //// process
                    //if ()
                    //{
                    //    if (!_genetecSdkWrapper.CheckIfServerExists(settingsData.ServerName, out string availableServerNames))
                    //    {
                    //        _logger.Log($"Server {settingsData.ServerName} doesn't exists. Please check the Server Name textbox on the left pane. Available server names: {availableServerNames}", LogLevel.Error);
                    //        ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                    //        return;
                    //    }

                    //    IncreaseCurrentProgressBarState();
                    //    ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ConfiguringStorage, _cameraListToBeProcessed.Count);

                    //    if (CheckCameraExistingStatus())
                    //    {
                    //        return;
                    //    }

                    //    AddCameras();
                    //    UpdateCameraSettings();
                    //    LogImportingCompleted();
                    //}

                }

                //ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, _cameraListToBeProcessed.Count);
            }
            catch (Exception e)
            {
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                _logger.Log(e.Message, LogLevel.Error);
            }
        }

        private bool ValidateLoginInformation(SettingsData settingsData)
            => settingsData != null &&
                   !string.IsNullOrEmpty(settingsData.UserName) &&
                   !string.IsNullOrEmpty(settingsData.ServerAddress);

        private void Login(SettingsData settingsData)
        {
            ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.LoggingIn, 1);
            _genetecSdkWrapper.Login(settingsData);
        }

        public void AddExistingCamerasToUpdateList(List<GenetecCamera> existingCamerasToBeUpdated)
        {
            _existingCamerasToBeUpdated = existingCamerasToBeUpdated;

            try
            {
                AddCameras();

                //add existing cameras for settings update
                _cameraListToBeProcessed.AddRange(_existingCamerasToBeUpdated);
                UpdateCameraSettings();
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, _cameraListToBeProcessed.Count);
                LogImportingCompleted();
            }
            catch (Exception e)
            {
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                _logger.Log(e.Message, LogLevel.Error);
            }
        }

        private bool TryParseCameras()
        {
            ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ImportingFile, 1);

            CheckSettingsDataIsValid(_settingsData);
            var fileContent = _fileLoader.Load();
            _cameraListToBeProcessed = _csvToCameraParser.Parse(fileContent);

            if (!_cameraListToBeProcessed.Any())
            {
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                _logger.Log("Can\'t import, camera list is empty.", LogLevel.Error);
                return false;
            }

            return true;
        }

        private void AddCameras()
        {
            ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.AddingCameras, _cameraListToBeProcessed.Count);

            foreach (var camera in _cameraListToBeProcessed)
            {
                _genetecSdkWrapper.AddCamera(camera, _logger);
                IncreaseCurrentProgressBarState();
            }
        }

        private void UpdateCameraSettings()
        {

        }

        private void CheckSettingsDataIsValid(SettingsData settingsData)
        {
            string exceptionOnProperty = String.Empty;

            if (!string.IsNullOrEmpty(exceptionOnProperty))
            {
                throw new Exception(ExceptionMessage.SettingsDataIsNotValid + " " + exceptionOnProperty);
            }
        }

        private void ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum applicationState, int maximumStepsCount)
        {
            _processStepCount = 0;
            ApplicationStateChanged?.Invoke(this, applicationState);
            ProgressBarStepsChanged?.Invoke(this, _processStepCount);
            ProgressBarMaximumStepsChanged?.Invoke(this, maximumStepsCount);
        }

        private void IncreaseCurrentProgressBarState()
        {
            ProgressBarStepsChanged?.Invoke(this, ++_processStepCount);
        }

        private void LogImportingCompleted()
        {
            _logger.Log("Importing completed.", LogLevel.Info);
        }
    }
}
