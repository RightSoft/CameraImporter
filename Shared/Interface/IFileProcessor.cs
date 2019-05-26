using System;
using System.Collections.Generic;
using CameraImporter.Model;
using CameraImporter.Model.Genetec;
using CameraImporter.ViewModel;

namespace CameraImporter.Shared.Interface
{
    public interface IFileProcessor
    {
        void Process(SettingsData settingsData);
        void AddExistingCamerasToUpdateList(List<GenetecCamera> existingCamerasToBeUpdated);

        event EventHandler<int> ProgressBarStepsChanged;
        event EventHandler<ApplicationStateEnum> ApplicationStateChanged;
        event EventHandler<int> ProgressBarMaximumStepsChanged;
        event EventHandler<List<GenetecCamera>> ExistingCameraListFound;
        event EventHandler<List<EntityModel>> AvailableArchiversFound;
    }
}
