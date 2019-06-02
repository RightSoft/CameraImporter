using System.IO;
using CameraImporter.Shared.Interface;
using CameraImporter.Utils;

namespace CameraImporter.Shared
{
    public class FileSystemWrapper : IFileSystemWrapper
    {
        public string[] GetFilesInFolder(string path)
        {
            return Directory.GetFiles(path);
        }

        public string GetFileTextContent(string path, string decryptionKey)
        {
            var encryptedContent = File.ReadAllText(path);
            EncryptionUtil util = new EncryptionUtil(decryptionKey);
            return util.Decrypt(encryptedContent);
        }

        public bool IsFileExpectedFileFormat(string file, string expectedFileFormat)
        {
            return Path.GetExtension(file) == expectedFileFormat;
        }

        public void WriteTextContent(string path, string content)
        {
            File.WriteAllText(path, content);
        }
    }
}