namespace CameraImporter.Shared.Interface
{
    public interface IFileSystemWrapper
    {
        string[] GetFilesInFolder(string path);
        string GetFileTextContent(string first, string path);
        void WriteTextContent(string path, string content);
        bool IsFileExpectedFileFormat(string file, string expectedFileFormat);
    }
}