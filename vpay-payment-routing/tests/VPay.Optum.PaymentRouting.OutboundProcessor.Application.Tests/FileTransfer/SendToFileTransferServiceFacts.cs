using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.FileTransfer;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.InboundHeaders.Queries
{
    public class SendToFileTransferServiceFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public SendToFileTransferServiceFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<SendToFileTransferService.Handler>()
                    .AddMock<IFileSystem>()
                    .AddMock<ISigningService>()
                    .AddMock<IHashingService<SHA256CryptoServiceProvider>>()
                    .AddMock<IOptions<SigningServiceOptions>>()
                    .AddMock<FileTransferServiceSettings>()
                    .AddMock<IFileReadyForTransferNotificationPublisher>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var ssOptions = _container.Fake<SigningServiceOptions>();
            var options = _container.Get<IOptions<SigningServiceOptions>>();
            options.Value.Returns(ssOptions);

            var fileInfo = SetupFileInfoMock();
            (var fileSystem, var file, var path, var dir) = GetFileSystemMocks();
            var hasingService = _container.Get<IHashingService<SHA256CryptoServiceProvider>>();
            hasingService.ComputeHash(Arg.Any<Stream>()).Returns("ABCDEF");

            var signingService = _container.Get<ISigningService>();
            signingService.Sign(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("11223344");

            var pub = _container.Get<IFileReadyForTransferNotificationPublisher>();
            pub.Publish(Arg.Any<FileReadyForTransferNotification>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var command = new SendToFileTransferService.Command(fileInfo, 1);

            var handler = _container.Get<SendToFileTransferService.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();
            VerifyFileSystemMocks(file, path, dir);

            fileInfo.Received().CopyTo(Arg.Any<string>());
            fileInfo.Received().CopyTo(Arg.Any<string>(), Arg.Any<bool>());
            fileInfo.Received().OpenRead();
            fileInfo.Received().Delete();

            hasingService.Received().ComputeHash(Arg.Any<Stream>());

            await signingService.Received().Sign(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await pub.Received().Publish(Arg.Any<FileReadyForTransferNotification>(), Arg.Any<CancellationToken>());
        }

        private (IFileSystem, IFile, IPath, IDirectory) GetFileSystemMocks(string combinedPath = "CombinedPath", bool fileExist = false, bool dirExist = false)
        {
            var file = _container.Mock<IFile>();
            file.Exists(Arg.Any<string>()).Returns(fileExist);
            var path = _container.Mock<IPath>();
            path.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(combinedPath);
            path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(combinedPath);
            path.GetRelativePath(Arg.Any<string>(), Arg.Any<string>()).Returns(combinedPath);

            var dir = _container.Mock<IDirectory>();
            dir.Exists(Arg.Any<string>()).Returns(dirExist);
            dir.CreateDirectory(Arg.Any<string>()).Returns(_container.Mock<IDirectoryInfo>());

            var fileSystem = _container.Get<IFileSystem>();
            fileSystem.File.Returns(file);
            fileSystem.Path.Returns(path);
            fileSystem.Directory.Returns(dir);

            return (fileSystem, file, path, dir);
        }

        private IFileInfo SetupFileInfoMock()
        {
            var fileInfo = _container.Mock<IFileInfo>();
            fileInfo.CopyTo(Arg.Any<string>()).Returns(fileInfo);
            fileInfo.CopyTo(Arg.Any<string>(), Arg.Any<bool>()).Returns(fileInfo);
            fileInfo.OpenRead().Returns(new MemoryStream());
            fileInfo.Delete();

            return fileInfo;
        }

        private static void VerifyFileSystemMocks(IFile file, IPath path, IDirectory dir)
        {
            file.Received().Exists(Arg.Any<string>());

            path.Received().Combine(Arg.Any<string>(), Arg.Any<string>());
            path.Received().Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            path.Received().GetRelativePath(Arg.Any<string>(), Arg.Any<string>());

            dir.Received().Exists(Arg.Any<string>());
            dir.Received().CreateDirectory(Arg.Any<string>());
        }
    }
}
