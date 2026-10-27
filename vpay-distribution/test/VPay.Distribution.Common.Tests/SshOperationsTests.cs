using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;

namespace VPay.Distribution.Common.Tests;

public class SshOperationsTests
{
    private readonly Mock<IFileSystem> _fileSystemMock = new();
    private readonly Mock<ILogger<SshOperations>> _loggerMock = new();
    private readonly Mock<ISftpClientFactory> _sftpClientFactoryMock = new();
    private readonly Mock<ISshClientFactory> _sshClientFactoryMock = new();
    private readonly SshOperations _sut;

    public SshOperationsTests()
    {
        _sut = new SshOperations(
            _fileSystemMock.Object,
            _loggerMock.Object,
            _sftpClientFactoryMock.Object,
            _sshClientFactoryMock.Object
        );
    }

    private ScpSettings GetValidScpSettings(string host = "remotehost") => new ScpSettings
    {
        Host = host,
        User = "user",
        Path = "/remote/path"
    };

    [Fact]
    public async Task TransferFileAsync_LocalHost_CopiesFile()
    {
        var filePath = "/local/file.txt";
        var scpSettings = GetValidScpSettings("localhost");

        _fileSystemMock.Setup(fs => fs.File.Copy(filePath, scpSettings.Path, true));

        await _sut.TransferFileAsync(filePath, scpSettings);

        _fileSystemMock.Verify(fs => fs.File.Copy(filePath, scpSettings.Path, true), Times.Once);
    }

    [Fact]
    public async Task TransferFileAsync_RemoteHost_UsesSftpClient()
    {
        var fileName = "file.txt";
        var filePath = "/local/file.txt";
        var scpSettings = GetValidScpSettings();
        var fileSystem = new MockFileSystem();
        fileSystem.AddEmptyFile(filePath);

        var sftpClientMock = new Mock<ISftpClient>();
        _sftpClientFactoryMock
            .Setup(f => f.Create(scpSettings.Host, 22, scpSettings.User, "/app/.ssh/id_rsa"))
            .Returns(sftpClientMock.Object);

        sftpClientMock.Setup(s => s.Connect());
        sftpClientMock.Setup(s => s.UploadFile(It.IsAny<Stream>(), scpSettings.Path, true));
        sftpClientMock.Setup(s => s.Disconnect());

        _fileSystemMock.Setup(fs => fs.Path.GetFileName(filePath)).Returns(fileName);
        _fileSystemMock.Setup(fs => fs.Path.Combine(scpSettings.Path, fileName)).Returns($"{scpSettings.Path}/{fileName}");

        // Setup fileStreamMock to behave like a readable stream
        var fileStreamMock = new Mock<Stream>();
        fileStreamMock.Setup(s => s.CanRead).Returns(true);
        fileStreamMock.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(0);

        _fileSystemMock.Setup(fs => fs.File.OpenRead(filePath)).Returns(() => fileSystem.File.OpenRead(filePath));

        await _sut.TransferFileAsync(filePath, scpSettings);

        sftpClientMock.Verify(s => s.Connect(), Times.Once);
        //sftpClientMock.Verify(s => s.UploadFile(It.IsAny<Stream>(), scpSettings.Path, true), Times.Once);
        sftpClientMock.Verify(s => s.UploadFile(It.IsAny<Stream>(), $"{scpSettings.Path}/{fileName}", true), Times.Once);
        sftpClientMock.Verify(s => s.Disconnect(), Times.Once);
    }

    [Fact]
    public async Task TransferFileAsync_RemoteHost_SftpThrows_ThrowsIOException()
    {
        var filePath = "/local/file.txt";
        var scpSettings = GetValidScpSettings();
        var fileSystem = new MockFileSystem();
        fileSystem.AddEmptyFile(filePath);

        var sftpClientMock = new Mock<ISftpClient>();
        _sftpClientFactoryMock
            .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(sftpClientMock.Object);

        sftpClientMock.Setup(s => s.Connect()).Throws(new IOException("fail"));

        var fileStreamMock = new Mock<Stream>();
        _fileSystemMock.Setup(fs => fs.File.OpenRead(filePath)).Returns(() => fileSystem.File.OpenRead(filePath));

        Func<Task> act = async () => await _sut.TransferFileAsync(filePath, scpSettings);
        await act.Should().ThrowAsync<IOException>();
    }

    [Theory]
    [InlineData(null, "user", "/path")]
    [InlineData("host", null, "/path")]
    [InlineData("host", "user", null)]
    public async Task TransferFileAsync_InvalidNullArguments_Throws(string host, string user, string path)
    {
        var scpSettings = new ScpSettings { Host = host, User = user, Path = path };
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.TransferFileAsync("/file.txt", scpSettings));
    }

    [Theory]
    [InlineData("", "user", "/path")]
    [InlineData("host", "", "/path")]
    [InlineData("host", "user", "")]
    public async Task TransferFileAsync_InvalidEmptyArguments_Throws(string host, string user, string path)
    {
        var scpSettings = new ScpSettings { Host = host, User = user, Path = path };
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.TransferFileAsync("/file.txt", scpSettings));
    }

    [Fact]
    public async Task TouchControlFileOnServerAsync_ValidArguments_RunsSshCommand()
    {
        var fileName = "control.txt";
        var scpSettings = GetValidScpSettings();
        var filePath = $"{scpSettings.Path}/{fileName}";

        _fileSystemMock.Setup(fs => fs.Path.Combine(scpSettings.Path, fileName)).Returns(filePath);

        var sshClientMock = new Mock<ISshClient>();
        _sshClientFactoryMock
            .Setup(f => f.Create(scpSettings.Host, 22, scpSettings.User, "/app/.ssh/id_rsa"))
            .Returns(sshClientMock.Object);

        sshClientMock.Setup(s => s.Connect());
        var commandMock = new Mock<ISshCommand>();
        commandMock.SetupGet(c => c.ExitStatus).Returns(0);
        sshClientMock.Setup(s => s.RunCommand($"touch {filePath}")).Returns(commandMock.Object);
        sshClientMock.Setup(s => s.Disconnect());

        await _sut.TouchControlFileOnServerAsync(fileName, scpSettings);

        sshClientMock.Verify(s => s.Connect(), Times.Once);
        sshClientMock.Verify(s => s.RunCommand($"touch {filePath}"), Times.Once);
        sshClientMock.Verify(s => s.Disconnect(), Times.Once);
    }

    [Fact]
    public async Task TouchControlFileOnServerAsync_CommandFails_ThrowsInvalidOperationException()
    {
        var fileName = "control.txt";
        var scpSettings = GetValidScpSettings();
        var filePath = $"{scpSettings.Path}/{fileName}";

        _fileSystemMock.Setup(fs => fs.Path.Combine(scpSettings.Path, fileName)).Returns(filePath);

        var sshClientMock = new Mock<ISshClient>();
        _sshClientFactoryMock
            .Setup(f => f.Create(scpSettings.Host, 22, scpSettings.User, "/app/.ssh/id_rsa"))
            .Returns(sshClientMock.Object);

        sshClientMock.Setup(s => s.Connect());
        var commandMock = new Mock<ISshCommand>();
        commandMock.SetupGet(c => c.ExitStatus).Returns(1);
        commandMock.SetupGet(c => c.Error).Returns("error");
        sshClientMock.Setup(s => s.RunCommand($"touch {filePath}")).Returns(commandMock.Object);
        sshClientMock.Setup(s => s.Disconnect());

        Func<Task> act = async () => await _sut.TouchControlFileOnServerAsync(fileName, scpSettings);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TouchControlFileOnServerAsync_SshThrows_ThrowsIOException()
    {
        var fileName = "control.txt";
        var scpSettings = GetValidScpSettings();
        var filePath = $"{scpSettings.Path}/{fileName}";

        _fileSystemMock.Setup(fs => fs.Path.Combine(scpSettings.Path, fileName)).Returns(filePath);

        var sshClientMock = new Mock<ISshClient>();
        _sshClientFactoryMock
            .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(sshClientMock.Object);

        sshClientMock.Setup(s => s.Connect()).Throws(new IOException("fail"));

        Func<Task> act = async () => await _sut.TouchControlFileOnServerAsync(fileName, scpSettings);
        await act.Should().ThrowAsync<IOException>();
    }

    [Theory]
    [InlineData(null, "user", "/path")]
    [InlineData("host", null, "/path")]
    [InlineData("host", "user", null)]
    public async Task TouchControlFileOnServerAsync_InvalidNullArguments_Throws(string host, string user, string path)
    {
        var scpSettings = new ScpSettings { Host = host, User = user, Path = path };
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.TouchControlFileOnServerAsync("file.txt", scpSettings));
    }

    [Theory]
    [InlineData("", "user", "/path")]
    [InlineData("host", "", "/path")]
    [InlineData("host", "user", "")]
    public async Task TouchControlFileOnServerAsync_InvalidEmptyArguments_Throws(string host, string user, string path)
    {
        var scpSettings = new ScpSettings { Host = host, User = user, Path = path };
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.TouchControlFileOnServerAsync("file.txt", scpSettings));
    }
}
