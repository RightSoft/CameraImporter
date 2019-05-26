namespace CameraImporter.Model.Genetec
{
    public class GenetecCamera : BaseCamera
    {
        public string Ip { get; set; }
        public short Port { get; set; } = 80;
        public string CameraName { get; set; }
        public string CameraType { get; set; }
        public string MacAddress { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Manufacturer { get; set; } = "onvif";
        public string ProductType { get; set; } = "All";

        public string ServerName { get; set; }
    }
}
