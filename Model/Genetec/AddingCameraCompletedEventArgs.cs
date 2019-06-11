using Genetec.Sdk;

namespace CameraImporter.Model.Genetec
{
    public class AddingCameraCompletedEventArgs : EntityModel
    {
        public EnrollmentResult EnrollmentResult { get; set; }
    }
}
