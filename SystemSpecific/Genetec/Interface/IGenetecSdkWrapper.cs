using CameraImporter.Model;
using CameraImporter.Model.Genetec;
using CameraImporter.Shared.Interface;
using CameraImporter.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CameraImporter.SystemSpecific.Genetec.Interface
{
    public interface IGenetecSdkWrapper
    {
        Task<bool> AddCamera(GenetecCamera cameraData, ILogger logger, SettingsData settingsData);
        void UpdateAddedCameraSettings(GenetecCamera cameraData, ILogger logger);
        List<GenetecCamera> CheckIfImportedCamerasExists(List<GenetecCamera> cameraListToBeProcessed, ILogger logger);
        void Login(SettingsData settingsData);
        void LogOff();
        event EventHandler<IsLoggedInEventArgs> IsLoggedIn;
        event EventHandler<AvailableArchiversFoundEventArgs> AvailableArchiversFound;
        event EventHandler<ExistingCameraListFoundEventArgs> ExistingCameraListFound;
        event EventHandler<AddingCameraCompletedEventArgs> AddingCameraCompleted;
        void Init();
        void Dispose();
        void FetchAvailableArchivers();
        void FetchAvailableCameras();
        void ChangeUnitName(GenetecCamera camera, Guid unitGuid);
    }
}
