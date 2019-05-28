using CameraImporter.Model.Genetec;

namespace CameraImporter.ViewModel
{
    public class SettingsData
    {
        public string ServerAddress { get; set; }
        public EntityModel Archiver { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
