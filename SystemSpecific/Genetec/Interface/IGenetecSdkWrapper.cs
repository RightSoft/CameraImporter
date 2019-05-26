using System;
using System.Collections.Generic;
using CameraImporter.Model.Genetec;
using CameraImporter.Shared.Interface;
using CameraImporter.ViewModel;

namespace CameraImporter.SystemSpecific.Genetec.Interface
{
    public interface IGenetecSdkWrapper
    {
        bool AddCamera(GenetecCamera cameraData, ILogger logger);
        bool UpdateAddedCameraSettings(GenetecCamera cameraData, ILogger logger);
        bool CheckIfServerExists(string settingsDataServerName, out string availableServerNames);
        List<GenetecCamera> CheckIfImportedCamerasExists(List<GenetecCamera> cameraListToBeProcessed, ILogger logger);
        void Login(SettingsData settingsData);
        event EventHandler<IsLoggedInEventArgs> IsLoggedIn;
        event EventHandler<AvailableArchiversFoundEventArgs> AvailableArchiversFound;
        void Init();
        void Dispose();
        void FetchAvailableArchivers();
        void FetchAvailableCameras();
    }
}
