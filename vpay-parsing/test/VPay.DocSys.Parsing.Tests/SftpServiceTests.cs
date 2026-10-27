using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Moq;
using Xunit;
using VPay.DocSys.Parsing;
using VPay.DocSys.Parsing.Common;
using VPay.Extensions.Ssh;
using System.IO.Abstractions.TestingHelpers;

namespace VPay.DocSys.Parsing.Tests
{
    public class file_transfer_service_should
    {
        #region [-- constant values --]

        private const string HOSTNAME = "fake-host.vpayusa.net";
        private const string LOCALPATH = "/my/local/path/file.tar";
        private const string REMOTEPATH = "/my/remote/path/file.tar";
        private const string USERNAME = "fake-user";
        private const string PASSWORD = "fake-password";
        private const string EMPTYVALUE = "";
        private const short PERMISSIONS = 666;

        #endregion

        private readonly IFileSystem _fileSystem;

        private readonly Mock<IFile> _mockFile;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ISftpClient> _mockSftpClient;
        private readonly Mock<ISshClientFactory> _mockSshClientFactory;

        private readonly ISftpService SUT;

        public file_transfer_service_should()
        {
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { "/ssh/id_rsa", new MockFileData("FAKE PRIVATE KEY") },
            });

            _mockFile = new Mock<IFile>();
            _mockFile.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            _mockFile.Setup(f => f.OpenRead(It.IsAny<string>())).Returns((string path) => _fileSystem.File.OpenRead(path));

            _mockFileSystem = new Mock<IFileSystem>();
            _mockFileSystem.SetupGet<IFile>(fs => fs.File).Returns(_mockFile.Object);
            _mockFileSystem.SetupGet<IPath>(fs => fs.Path).Returns(_fileSystem.Path);

            _mockSftpClient = new Mock<ISftpClient>();

            _mockSftpClient.Setup(c => c.Connect())
                .Verifiable();

            _mockSftpClient.Setup(c =>
                    c.UploadFileAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                .Verifiable();

            _mockSftpClient.Setup(c => c.Disconnect())
                .Verifiable();

            _mockSshClientFactory = new Mock<ISshClientFactory>();

            _mockSshClientFactory.Setup(f =>
                    f.CreateSftpClient(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                .Returns(_mockSftpClient.Object)
                .Verifiable();

            _mockSshClientFactory.Setup(f =>
                    f.CreateSftpClient(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<PrivateKey[]>()))
                .Returns(_mockSftpClient.Object)
                .Verifiable();


            SUT = new SftpService(
                _mockFileSystem.Object,
                _mockSshClientFactory.Object,
                NullLogger<SftpService>.Instance);
        }

        [Theory]
        [InlineData(HOSTNAME, LOCALPATH, REMOTEPATH, USERNAME, PASSWORD)]
        public async Task transfer_file_to_remote_host_using_password(string remoteHost, string localPath, string remotePath, string userName, string password)
        {
            await SUT.TransferFile(
                localPath: localPath,
                remoteHost: remoteHost,
                remotePath: remotePath,
                userName: userName,
                password: password);

            _mockSshClientFactory.Verify(f =>
                f.CreateSftpClient(
                    It.Is<string>(s => s == remoteHost),
                    It.Is<string>(s => s == userName),
                    It.Is<string>(s => s == password)));

            _mockSftpClient.Verify(c =>
                c.Connect());

            _mockSftpClient.Verify(c =>
                c.UploadFileAsync(
                    It.Is<string>(s => s == localPath),
                    It.Is<string>(s => s == remotePath),
                    It.Is<bool>(b => b),
                    It.IsAny<CancellationToken>()));

            _mockSftpClient.Verify(c =>
                c.Disconnect());

            _mockSftpClient.Verify(c =>
                c.Dispose());

            _mockFile.VerifyNoOtherCalls();
            _mockFileSystem.VerifyNoOtherCalls();
            _mockSftpClient.VerifyNoOtherCalls();
            _mockSshClientFactory.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(HOSTNAME, LOCALPATH, REMOTEPATH, USERNAME)]
        public async Task transfer_file_to_remote_host_using_private_key(string remoteHost, string localPath, string remotePath, string userName)
        {
            await SUT.TransferFile(
                localPath: localPath,
                remoteHost: remoteHost,
                remotePath: remotePath,
                userName: userName);

            _mockSshClientFactory.Verify(f =>
                f.CreateSftpClient(
                    It.Is<string>(s => s == remoteHost),
                    It.Is<string>(s => s == userName),
                    It.Is<PrivateKey[]>(v => v.Length == 1 && v[0].KeyStream != null)));

            _mockFile.Verify(f =>
                f.OpenRead(
                    It.Is<string>(v => v == SftpService.DefaultRsaFilePath)));

            _mockSftpClient.Verify(c =>
                c.Connect());

            _mockSftpClient.Verify(c =>
                c.UploadFileAsync(
                    It.Is<string>(s => s == localPath),
                    It.Is<string>(s => s == remotePath),
                    It.Is<bool>(b => b),
                    It.IsAny<CancellationToken>()));

            _mockSftpClient.Verify(c =>
                c.Disconnect());

            _mockSftpClient.Verify(c =>
                c.Dispose());

            _mockFile.VerifyNoOtherCalls();
            _mockFileSystem.VerifyNoOtherCalls();
            _mockSftpClient.VerifyNoOtherCalls();
            _mockSshClientFactory.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(HOSTNAME, LOCALPATH, REMOTEPATH, USERNAME, PASSWORD, PERMISSIONS)]
        public async Task transfer_file_to_remote_host_and_change_permissions(string remoteHost, string localPath, string remotePath, string userName, string password, short permissions)
        {
            await SUT.TransferFile(
                localPath: localPath,
                remoteHost: remoteHost,
                remotePath: remotePath,
                userName: userName,
                password: password,
                permissions: permissions);

            _mockSshClientFactory.Verify(f =>
                f.CreateSftpClient(
                    It.Is<string>(s => s == remoteHost),
                    It.Is<string>(s => s == userName),
                    It.Is<string>(s => s == password)));

            _mockSftpClient.Verify(c =>
                c.Connect());

            _mockSftpClient.Verify(c =>
                c.UploadFileAsync(
                    It.Is<string>(s => s == localPath),
                    It.Is<string>(s => s == remotePath),
                    It.Is<bool>(b => b),
                    It.IsAny<CancellationToken>()));

            _mockSftpClient.Verify(c =>
                c.ChangePermissions(
                    It.Is<string>(s => s == remotePath),
                    It.Is<short>(s => s == permissions)));

            _mockSftpClient.Verify(c =>
                c.Disconnect());

            _mockSftpClient.Verify(c =>
                c.Dispose());

            _mockFile.VerifyNoOtherCalls();
            _mockFileSystem.VerifyNoOtherCalls();
            _mockSftpClient.VerifyNoOtherCalls();
            _mockSshClientFactory.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null, LOCALPATH, REMOTEPATH, USERNAME, PASSWORD)]
        [InlineData(HOSTNAME, null, REMOTEPATH, USERNAME, PASSWORD)]
        [InlineData(HOSTNAME, LOCALPATH, null, USERNAME, PASSWORD)]
        [InlineData(HOSTNAME, LOCALPATH, REMOTEPATH, null, PASSWORD)]
        [InlineData(HOSTNAME, LOCALPATH, REMOTEPATH, USERNAME, null)]
        public async Task throw_exception_on_null_arguments(string remoteHost, string localPath, string remotePath, string userName, string password)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
            {
                return SUT.TransferFile(
                    localPath: localPath,
                    remoteHost: remoteHost,
                    remotePath: remotePath,
                    userName: userName,
                    password: password);
            });
        }

        [Theory]
        [InlineData(EMPTYVALUE, LOCALPATH, REMOTEPATH, USERNAME, PASSWORD)]
        [InlineData(HOSTNAME, EMPTYVALUE, REMOTEPATH, USERNAME, PASSWORD)]
        [InlineData(HOSTNAME, LOCALPATH, EMPTYVALUE, USERNAME, PASSWORD)]
        [InlineData(HOSTNAME, LOCALPATH, REMOTEPATH, EMPTYVALUE, PASSWORD)]
        [InlineData(HOSTNAME, LOCALPATH, REMOTEPATH, USERNAME, EMPTYVALUE)]
        public async Task throw_exception_on_empty_arguments(string remoteHost, string localPath, string remotePath, string userName, string password)
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return SUT.TransferFile(
                    localPath: localPath,
                    remoteHost: remoteHost,
                    remotePath: remotePath,
                    userName: userName,
                    password: password);
            });
        }
    }
}
