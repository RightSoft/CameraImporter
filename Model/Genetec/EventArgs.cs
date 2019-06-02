using System;
using System.Collections.Generic;

namespace CameraImporter.Model.Genetec
{
    public class ExistingCameraListFoundEventArgs
    {
        public bool IsExistingCamerasFound;
        public List<EntityModel> ExistingCameras;

        public ExistingCameraListFoundEventArgs(bool isExistingCamerasFound, List<EntityModel> existingCameras)
        {
            IsExistingCamerasFound = isExistingCamerasFound;
            ExistingCameras = existingCameras;
        }
    }

    public class AvailableArchiversFoundEventArgs : EventArgs
    {
        public bool IsArchiversFound;
        public List<EntityModel> AvailableArchivers;

        public AvailableArchiversFoundEventArgs(bool isArchiversFound, List<EntityModel> availableArchivers)
        {
            IsArchiversFound = isArchiversFound;
            AvailableArchivers = availableArchivers;
        }
    }
}
