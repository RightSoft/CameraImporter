namespace CameraImporter.Shared
{
    public static class ExceptionMessage
    {
        public static string NoFileFound = "Can't find " + Constants.ImportedFileFormat + " file!";
        public static string MultipleFilesFound = "Multiple " + Constants.ImportedFileFormat + " files are found";
        public static string MultipleDecryptionKeyFilesFound = "Multiple " + Constants.DecryptionKeyFileFormat + " files are found";
        public static string ContentIsNullOrEmpty = "Content is empty!";
        public static string SettingsDataIsNotValid = "Settings property is not valid: ";
        public static string ServerNotFound = "Server not found";
        public static string InvalidCredentials = "Invalid credentials for:";
        public static string InternalConnectionError = "Internal error connecting to:";
        public static string LoginSucceed = "Login succeed";
        public static string DeviceSettingGatheringError = "Can't gather device settings from Milestone server";
        public static string CameraMismatch = "Camera mismatch. Newly added camera doesn't match the uploaded file.";
        public static string DecryptContentFailed = "Decrypting import file content failed.";
    }

    public static class Constants
    {
        public static string ImportedFileFormat = ".auto";
        public static string DecryptionKeyFileFormat = ".key";
    }
}