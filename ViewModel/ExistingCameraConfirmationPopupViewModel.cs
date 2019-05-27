using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using CameraImporter.Model.Genetec;

namespace MilestoneImporter.ViewModel
{
    public class ExistingCameraConfirmationPopupViewModel : ViewModelBase
    {
        private List<GenetecCamera> _existingCameras = new List<GenetecCamera>();
        public List<GenetecCamera> ExistingCameras
        {
            get => _existingCameras;
            set => Set(nameof(ExistingCameras), ref _existingCameras, value);
        }

        private string _existingCamerasText = string.Empty;
        public string ExistingCamerasText
        {
            get => $"These cameras already exist on the server: {Environment.NewLine}{_existingCamerasText}{Environment.NewLine}Do you want to overwrite the properties?";
            set => Set(nameof(ExistingCamerasText), ref _existingCamerasText, value);
        }

        public RelayCommand UpdateSettingsCommand { get; private set; }
        public RelayCommand NotUpdateSettingsCommand { get; private set; }
        public event EventHandler<List<GenetecCamera>> UpdateConfirmedForExistingCameras;

        public ExistingCameraConfirmationPopupViewModel()
        {
            UpdateSettingsCommand = new RelayCommand(ExecuteUpdateSettingsCommand);
            NotUpdateSettingsCommand = new RelayCommand(ExecuteNotUpdateSettingsCommand);
        }

        private void ExecuteNotUpdateSettingsCommand()
        {
            UpdateConfirmedForExistingCameras?.Invoke(this, new List<GenetecCamera>());
        }

        private void ExecuteUpdateSettingsCommand()
        {
            UpdateConfirmedForExistingCameras?.Invoke(this, ExistingCameras);
        }
    }
}
