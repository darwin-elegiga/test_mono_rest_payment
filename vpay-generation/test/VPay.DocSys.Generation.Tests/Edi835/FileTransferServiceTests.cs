using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Moq;
using Xunit;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Edi835;
using VPay.DocSys.Generation.Edi835.Application;
using VPay.Extensions.Ssh;

namespace VPay.DocSys.Generation.Edi835.ApplicationTests
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

        #endregion

        private readonly Mock<ISftpClient> _mockSftpClient;
        private readonly Mock<ISshClientFactory> _mockSshClientFactory;

        private readonly IFileTransferService SUT;

        public file_transfer_service_should()
        {
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

            SUT = new FileTransferService(
                _mockSshClientFactory.Object,
                NullLogger<FileTransferService>.Instance);
        }

        [Theory]
        [InlineData(HOSTNAME, LOCALPATH, REMOTEPATH, USERNAME, PASSWORD)]
        public async Task transfer_file_to_remote_host(string remoteHost, string localPath, string remotePath, string userName, string password)
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

            _mockSftpClient.Verify(c => c.Connect());
            _mockSftpClient.Verify(c =>
                c.UploadFileAsync(
                    It.Is<string>(s => s == localPath),
                    It.Is<string>(s => s == remotePath),
                    It.Is<bool>(b => b == true),
                    It.IsAny<CancellationToken>()));
            _mockSftpClient.Verify(c => c.Disconnect());
            _mockSftpClient.Verify(c => c.Dispose());

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
