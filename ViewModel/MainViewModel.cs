using CameraImporter.Extensions;
using CameraImporter.Model;
using CameraImporter.Model.Genetec;
using CameraImporter.Shared;
using CameraImporter.Shared.Interface;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using MilestoneImporter.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CameraImporter.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IFileProcessor _fileProcessor;
        private readonly ILogger _logger;
        private readonly IFileSystemWrapper _fileSystemWrapper;
        private ApplicationStateEnum _processCategoryEnum;

        public ObservableCollection<EntityModel> AvailableArchivers { get; set; }

        private SettingsData _settingsData;

        private int _progressBarMaximumSteps;
        private double _progressBarCurrentStepsCount;
        private bool _canProcess = true;
        private bool _canExportLog = true;
        private bool _isArchiverSelectionEnabled;
        private bool _isApplicationProcessing;
        private string _logText = "";
        private string _processingCategory;
        private bool _isCameraUpdatePopupVisible;
        private EntityModel _selectedArchiverModel;

        public bool IsCameraUpdatePopupVisible
        {
            get => _isCameraUpdatePopupVisible;
            set => Set(nameof(IsCameraUpdatePopupVisible), ref _isCameraUpdatePopupVisible, value);
        }

        public bool IsArchiverSelectionEnabled
        {
            //get => _isArchiverSelectionEnabled;
            get => true;
            set => Set(nameof(IsArchiverSelectionEnabled), ref _isArchiverSelectionEnabled, value);
        }

        public string LogText
        {
            get => _logText;
            set => Set(nameof(LogText), ref _logText, value);
        }

        public double ProgressBarCurrentStepsCount
        {
            get => _progressBarCurrentStepsCount;
            set => Set(nameof(ProgressBarCurrentStepsCount), ref _progressBarCurrentStepsCount, value);
        }

        public int ProgressBarMaximumSteps
        {
            get => _progressBarMaximumSteps;
            set => Set(nameof(ProgressBarMaximumSteps), ref _progressBarMaximumSteps, value);
        }

        public string ProcessingCategory
        {
            get => _processingCategory;
            set => Set(nameof(ProcessingCategory), ref _processingCategory, value);
        }

        public bool IsApplicationProcessing
        {
            get => _isApplicationProcessing;
            set => Set(nameof(IsApplicationProcessing), ref _isApplicationProcessing, value);
        }

        public ApplicationStateEnum ProcessCategoryEnum
        {
            get => _processCategoryEnum;
            set => Set(nameof(ProcessCategoryEnum), ref _processCategoryEnum, value);
        }

        public SettingsData SettingsData
        {
            get => _settingsData;
            set => Set(nameof(SettingsData), ref _settingsData, value);
        }

        public EntityModel SelectedArchiverModel
        {
            get { return _selectedArchiverModel; }
            set
            {
                if (value == _selectedArchiverModel)
                {
                    return;
                }

                _selectedArchiverModel = value;
                //OnPropertyChanged(nameof(SelectedArchiverModel));
            }
        }

        public RelayCommand ProcessCommand { get; private set; }
        public RelayCommand ExportLogCommand { get; private set; }
        public RelayCommand ClearLogCommand { get; private set; }

        public MainViewModel(IFileProcessor fileProcessor, ILogger logger, IFileSystemWrapper fileSystemWrapper)
        {
            _logger = logger;
            _fileSystemWrapper = fileSystemWrapper;
            _fileProcessor = fileProcessor;
            _fileProcessor.AvailableArchiversFound += OnAvailableArchiversFound;
            _fileProcessor.ExistingCameraListFound += OnExistingCameraListFound;

            _logger.LogUpdated += OnLogUpdated;

            ProcessCommand = new RelayCommand(async () =>
            {
                await ExecuteProcess();
            }, () => _canProcess);

            ExportLogCommand = new RelayCommand(ExecuteExportLog, () => _canExportLog);
            ClearLogCommand = new RelayCommand(ExecuteClearLogCommand);

            SetDefaultSettingsData();
        }

        private void OnAvailableArchiversFound(object sender, List<EntityModel> e)
        {
            Debug.WriteLine("archiver list");
            AvailableArchivers = new ObservableCollection<EntityModel>(e);
        }

        private void OnExistingCameraListFound(object sender, List<GenetecCamera> existingCameraList)
        {
            var cameraConfirmationViewModel = SimpleIoc.Default.GetInstance<ExistingCameraConfirmationPopupViewModel>();
            cameraConfirmationViewModel.ExistingCameras = existingCameraList;
            cameraConfirmationViewModel.ExistingCamerasText = string.Concat(existingCameraList.Select(p => $"{p.CameraName}{Environment.NewLine}"));
            cameraConfirmationViewModel.UpdateConfirmedForExistingCameras += OnUpdateConfirmedForExistingCameras;

            IsCameraUpdatePopupVisible = true;
        }

        private void OnUpdateConfirmedForExistingCameras(object sender, List<GenetecCamera> existingCameras)
        {
            //Application.Current.Dispatcher.Invoke(new Action(() =>
            //{
            //    IsCameraUpdatePopupVisible = false;
            //}));
            //Task.Run(() =>
            //{
            //    _fileProcessor.AddExistingCamerasToUpdateList(existingCameras);
            //    var cameraConfirmationViewModel = SimpleIoc.Default.GetInstance<ExistingCameraConfirmationPopupViewModel>();
            //    cameraConfirmationViewModel.UpdateConfirmedForExistingCameras -= OnUpdateConfirmedForExistingCameras;
            //});
        }

        public void ExecuteClearLogCommand()
        {
            _logger.ClearLog();
            LogText = string.Empty;
        }

        private void SetDefaultSettingsData()
        {
            var defaultSettingsData = new SettingsData
            {
                ServerAddress = "localhost",
                UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last(),
                Password = string.Empty
            };
            SettingsData = defaultSettingsData;
        }

        private void OnLogUpdated(object sender, LogUpdatedEventArgs args)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                LogText = this.LogText + args.logData.Message + "\n";
            }));
        }

        public Task ExecuteProcess()
        {
            return Task.Run(() =>
            {
                _fileProcessor.ProgressBarStepsChanged += OnProgressBarStepsIncreased;
                _fileProcessor.ProgressBarMaximumStepsChanged += OnProgressBarMaximumStepsIncreased;
                _fileProcessor.ApplicationStateChanged += OnApplicationStateChanged;
                _fileProcessor.Process(SettingsData);
            });
        }

        private void OnApplicationStateChanged(object sender, ApplicationStateEnum applicationState)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                ProcessingCategory = applicationState.GetDescription();
                IsApplicationProcessing = applicationState != ApplicationStateEnum.ApplicationIdle;
            }));
        }

        private void OnProgressBarStepsIncreased(object sender, int processedSteps)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => { ProgressBarCurrentStepsCount = processedSteps; }));
        }

        private void OnProgressBarMaximumStepsIncreased(object sender, int maximumSteps)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => { ProgressBarMaximumSteps = maximumSteps; }));
        }

        public void ExecuteExportLog()
        {
            var logFileFullPath = Path.Combine(Environment.CurrentDirectory, $"milestoneimporterlog[{DateTime.Now.ToString("yyyymmddhhss")}].txt");
            _fileSystemWrapper.WriteTextContent(logFileFullPath, _logger.ToString());
            Process.Start(logFileFullPath);
        }
    }
}