using System;
using System.Linq;
using CameraImporter.Shared.Interface;

namespace CameraImporter.Shared
{
    public class FileLoader : IFileLoader
    {
        private const string DECRYPTION_KEY = "F]NGYTEM:8#<!]F";

        private readonly IFileSystemWrapper _fileSystem;
        private readonly IFileSystemWrapper _fileSystemWrapper;

        public FileLoader(IFileSystemWrapper fileSystem, IFileSystemWrapper fileSystemWrapper)
        {
            _fileSystem = fileSystem;
            _fileSystemWrapper = fileSystemWrapper;
        }

        public string Load()
        {
            var files = _fileSystem.GetFilesInFolder(Environment.CurrentDirectory);
            var numberOfFiles = files.Count(file => _fileSystemWrapper.IsFileExpectedFileFormat(file, Constants.ImportedFileFormat));

            if (numberOfFiles == 0)
            {
                throw new Exception(ExceptionMessage.NoFileFound);
            }

            if (numberOfFiles > 1)
            {
                throw new Exception(ExceptionMessage.MultipleFilesFound);
            }

            var decryptionKeyFiles = files.Where(file => _fileSystemWrapper.IsFileExpectedFileFormat(file,
                    Constants.DecryptionKeyFileFormat))
                .ToList();

            if (decryptionKeyFiles.Count() > 1)
            {
                throw new Exception(ExceptionMessage.MultipleDecryptionKeyFilesFound);
            }

            var decryptionKey = DECRYPTION_KEY;
            if (decryptionKeyFiles.Count() == 1)
            {
                decryptionKey = System.IO.File.ReadAllText(decryptionKeyFiles.First());
            }

            return _fileSystem.GetFileTextContent(files.First(file => _fileSystemWrapper.IsFileExpectedFileFormat(file,
                    Constants.ImportedFileFormat)),
                decryptionKey);
        }
    }
}