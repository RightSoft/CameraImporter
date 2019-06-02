using CameraImporter.Extensions;
using CameraImporter.Model;
using CameraImporter.Model.Genetec;
using CameraImporter.Shared.Interface;
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

        private bool _isUpdating;
        private bool _isMultiArchierMode;
        private int _processStepCount;
        private int _processedCameraCount;
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
            _genetecSdkWrapper.AddingCameraCompleted += OnAddingCameraCompleted;
            _genetecSdkWrapper.Init();
        }

        private void OnAddingCameraCompleted(object sender, EntityModel e)
        {
            _processedCameraCount++;
            IncreaseCurrentProgressBarState();

            var addedCamera = _cameraListToBeProcessed.FirstOrDefault(p => p.Ip.Equals(e.EntityName));

            if (addedCamera != null)
            {
                //we do this because enrollment doesn't return camera guid and we have to query the server 3 times to get that value
                //these last 17 characters are the same for the unit and children
                addedCamera.Guid = e.EntityGuid.ToString().Right(17);

                _genetecSdkWrapper.ChangeUnitName(addedCamera, e.EntityGuid);
                _logger.Log($"Camera added successfully: {addedCamera.CameraName}", LogLevel.Info);
            }

            if (_processedCameraCount == _cameraListToBeProcessed.Count)
            {
                _isUpdating = true;
                _genetecSdkWrapper.FetchAvailableCameras();
            }
        }

        private void OnExistingCameraListFound(object sender, ExistingCameraListFoundEventArgs e)
        {
            if (!_isUpdating)
            {
                if (!e.IsExistingCamerasFound)
                {
                    _logger.Log("There are no cameras added to Genetec prior to this import process.", LogLevel.Info);

                    IncreaseCurrentProgressBarState();

                    AddCameras();
                }
                else
                {
                    List<GenetecCamera> matchingExistingCameras =
                        _genetecSdkWrapper.CheckIfImportedCamerasExists(_cameraListToBeProcessed, _logger);

                    if (matchingExistingCameras.Any())
                    {
                        foreach (var alreadyExistingCamera in matchingExistingCameras)
                        {
                            _cameraListToBeProcessed.Remove(alreadyExistingCamera);
                        }

                        IncreaseCurrentProgressBarState();

                        ExistingCameraListFound?.Invoke(this, matchingExistingCameras);
                    }
                }
            }
            else
            {
                UpdateCameraSettings();
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                _isUpdating = false;
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
                _logger.Log(
                    $"Only one archiver found (Archiver Name: {e.AvailableArchivers.First().EntityName} The import will automatically continue using this archiver",
                    LogLevel.Info);
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.CheckingExistingCameras, 1);

                ProcessWithSelectedArchiver();

            }

            if (e.AvailableArchivers.Count > 1)
            {
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                _logger.Log("Multiple archivers found. Please select which archiver you want to proceed", LogLevel.Warning);
                _isMultiArchierMode = true;
            }

            AvailableArchiversFound?.Invoke(this, e.AvailableArchivers);
        }

        public void Process(SettingsData settingsData)
        {
            _settingsData = settingsData;

            if (_isMultiArchierMode)
            {
                ProcessWithSelectedArchiver();
                return;
            }

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
            }
            catch (Exception e)
            {
                ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.ApplicationIdle, 1);
                _logger.Log(e.Message, LogLevel.Error);
            }
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

        private bool ValidateLoginInformation(SettingsData settingsData)
            => settingsData != null &&
                   !string.IsNullOrEmpty(settingsData.UserName) &&
                   !string.IsNullOrEmpty(settingsData.ServerAddress);

        private void Login(SettingsData settingsData)
        {
            ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.LoggingIn, 1);
            _genetecSdkWrapper.Login(settingsData);
        }

        public void ProcessWithSelectedArchiver()
        {
            _genetecSdkWrapper.FetchAvailableCameras();
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
                if (!_genetecSdkWrapper.AddCamera(camera, _logger, _settingsData).Result)
                {
                    _logger.Log($"Adding camera failed: {camera.CameraName}", LogLevel.Warning);
                }
            }
        }

        private void UpdateCameraSettings()
        {
            ChangeProgressBarToInitialStateOfAProcess(ApplicationStateEnum.UpdatingSettings, _cameraListToBeProcessed.Count);

            _processStepCount = 0;
            ProgressBarStepsChanged?.Invoke(this, _processStepCount);
            foreach (var camera in _cameraListToBeProcessed)
            {
                _genetecSdkWrapper.UpdateAddedCameraSettings(camera, _logger);
                IncreaseCurrentProgressBarState();
            }

            LogImportingCompleted();
        }

        private void CheckSettingsDataIsValid(SettingsData settingsData)
        {
            string exceptionOnProperty = string.Empty;

            if (string.IsNullOrEmpty(settingsData.ServerAddress))
            {
                exceptionOnProperty = "Server Address";
            }

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
