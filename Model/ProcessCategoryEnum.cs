using System.ComponentModel;

namespace CameraImporter.Model
{
    public enum ApplicationStateEnum
    {
        [Description("Application Idle")]
        ApplicationIdle,
        [Description("Logging in")]
        LoggingIn,
        [Description("Logged in")]
        LoggedIn,
        [Description("Importing File")]
        ImportingFile,
        //for milestone
        [Description("Configuring Storage")]
        ConfiguringStorage,
        //for genetec
        [Description("Configuring Archiver")]
        ConfiguringArchiver,
        [Description("Adding Cameras")]
        AddingCameras,
        [Description("Checking Existing Cameras")]
        CheckingExistingCameras,
        [Description("Updating Settings")]
        UpdatingSettings
    }
}
