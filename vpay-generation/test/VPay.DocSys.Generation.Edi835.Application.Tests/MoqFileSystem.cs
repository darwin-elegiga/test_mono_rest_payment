using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;

namespace VPay.DocSys.Generation.Edi835.Application.Tests.TestModels
{
    [Serializable]
    public class MoqFileSystem : IFileSystem
    {
        private MockFileSystem _mockFileSystem;

        public MoqFileSystem() : this(null) { }

        public MoqFileSystem(IDictionary<string, MockFileData> files, string currentDirectory = "")
        {

            _mockFileSystem = new MockFileSystem(files, currentDirectory);

            if (string.IsNullOrEmpty(currentDirectory))
                currentDirectory = System.IO.Path.GetTempPath();

            PathMoq = new Mock<MockPath>(_mockFileSystem) { CallBase = true };
            FileMoq = new Moq.Mock<MockFile>(_mockFileSystem) { CallBase = true };
            DirectoryMoq = new Moq.Mock<MockDirectory>(_mockFileSystem, FileMoq.Object, currentDirectory) { CallBase = true };
            FileInfoMoq = new Moq.Mock<MockFileInfoFactory>(_mockFileSystem) { CallBase = true };
            DirectoryInfoMoq = new Moq.Mock<MockDirectoryInfoFactory>(_mockFileSystem) { CallBase = true };
            DriveInfoMoq = new Moq.Mock<MockDriveInfoFactory>(_mockFileSystem) { CallBase = true };
        }

        public IFile File
        {
            get { return FileMoq.Object; }
        }

        public IFileStreamFactory FileStream { get; }

        public IFileSystemWatcherFactory FileSystemWatcher { get; }

        public IDirectory Directory
        {
            get { return DirectoryMoq.Object; }
        }

        public IFileInfoFactory FileInfo
        {
            get { return FileInfoMoq.Object; }
        }

        public IPath Path
        {
            get { return PathMoq.Object; }
        }

        public IDirectoryInfoFactory DirectoryInfo
        {
            get { return DirectoryInfoMoq.Object; }
        }

        public IDriveInfoFactory DriveInfo
        {
            get { return DriveInfoMoq.Object; }
        }

        public PathVerifier PathVerifier
        {
            get { return _mockFileSystem.PathVerifier; }
        }

        public Mock<MockFile> FileMoq { get; }
        public Mock<MockDirectory> DirectoryMoq { get; }
        public Mock<MockFileInfoFactory> FileInfoMoq { get; }
        public Mock<MockPath> PathMoq { get; }
        public Mock<MockDirectoryInfoFactory> DirectoryInfoMoq { get; }
        public Mock<MockDriveInfoFactory> DriveInfoMoq { get; }

        public MockFileData GetFile(string path)
        {
            return _mockFileSystem.GetFile(path);
        }

        public void AddFile(string path, MockFileData mockFile)
        {
            _mockFileSystem.AddFile(path, mockFile);
        }

        public void AddDirectory(string path)
        {
            _mockFileSystem.AddDirectory(path);
        }

        public void AddFileFromEmbeddedResource(string path, Assembly resourceAssembly, string embeddedResourcePath)
        {
            _mockFileSystem.AddFileFromEmbeddedResource(path, resourceAssembly, embeddedResourcePath);
        }

        public void AddFilesFromEmbeddedNamespace(string path, Assembly resourceAssembly, string embeddedRresourcePath)
        {
            _mockFileSystem.AddFilesFromEmbeddedNamespace(path, resourceAssembly, embeddedRresourcePath);
        }

        public void RemoveFile(string path)
        {
            _mockFileSystem.RemoveFile(path);
        }

        public bool FileExists(string path)
        {
            return _mockFileSystem.FileExists(path);
        }

        public IEnumerable<string> AllPaths
        {
            get
            {
                return _mockFileSystem.AllPaths;
            }
        }

        public IEnumerable<string> AllFiles
        {
            get { 
                return _mockFileSystem.AllFiles;
            }
        }

        public IEnumerable<string> AllDirectories
        {
            get
            {
                return _mockFileSystem.AllDirectories;
            }
        }

        public Mock<MockPath> PathFieldMoq => PathMoq;

        public IFileVersionInfoFactory FileVersionInfo => throw new NotImplementedException();

        //public IFileVersionInfoFactory FileVersionInfo
        //{
        //    get { return _mockFileSystem.FileVersionInfo; }
        //}
    }
}
