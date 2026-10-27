using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Renci.SshNet.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.Extensions.Ssh;

namespace VPay.DocSys.Parsing.Api.Controllers
{
    [Route("/api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class SshTestsController : ControllerBase
    {
        #region [-- Test Step/Action --]

        public enum ActionCode
        {
            SshConnect,
            ScpConnect,
            SftpConnect,
            LocalFileExists,
            RemoteFileExists,
            RemoteFileNotExists,
            ScpUpload,
            ScpDownload,
            SftpUpload,
            SftpDownload,
            SftpUploadAsync,
            SftpDownloadAsync,
            ChangePermissions,
            ChangeRemoteDirectory,
            ChangeLocalDirectory,
            CreateRemoteFile,
            CreateLocalFile,
            CreateRemoteDirectory,
            CreateLocalDirectory,
            DeleteRemote,
            DeleteRemoteFile,
            DeleteRemoteDirectory,
            DeleteLocal,
            DeleteLocalFile,
            DeleteLocalDirectory,
            ReadBytes,
            ReadLines,
            ReadLinesWithEncoding,
            ReadText,
            ReadTextWithEncoding,
            Touch,
            WriteBytes,
            WriteLines,
            WriteLinesWithEncoding,
            WriteText,
            WriteTextWithEncoding,
            RunCommand,
            RunCommandAsync
        }

        public class Connection
        {
            public string Name { get; set; }
            public string Host { get; set; }
            public int? Port { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public string PrivateKeyFile { get; set; }
        }

        public class Step
        {
            public string Title { get; set; }
            public ActionCode? Action { get; set; }
            public string Connection { get; set; }
            public string Host { get; set; }
            public int? Port { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public string PrivateKeyFile { get; set; }
            public bool UseStream { get; set; }
            public string LocalPath { get; set; }
            public string RemotePath { get; set; }
            public short? Permissions { get; set; }
            public bool? AllowOverwrite { get; set; }
            public string RenamePath { get; set; }
            public string CommandText { get; set; }
            public int? ExpectedExitStatus { get; set; }
            public string Encoding { get; set; }
            public bool? Recursive { get; set; }
            public bool HaltOnError { get; set; }
            public string FailureMessage { get; set; }
            public string ExpectedException { get; set; }
            public Step Clone()
            {
                return (Step)this.MemberwiseClone();
            }
        }

        public class TestSet
        {
            public Connection[] Connections { get; set; }
            public Step[] Steps { get; set; }
        }

        #endregion

        private const string DefaultPrivateKeyFile = @"/home/bogner/.ssh/id_rsa";

        private readonly IFileSystem _fileSystem;
        private readonly IConfiguration _configuration;
        private readonly ISshClientFactory _sshClientFactory;
        private readonly Stream _defaultPrivateKeyStream;

        public SshTestsController(
            IFileSystem fileSystem,
            IConfiguration configuration,
            ISshClientFactory sshClientFactory
            )
        {
            _fileSystem = fileSystem;
            _configuration = configuration;
            _sshClientFactory = sshClientFactory;

            _defaultPrivateKeyStream = _fileSystem.File.OpenRead(DefaultPrivateKeyFile);
        }


        [HttpPost("RunTestSet")]
        public async Task<IActionResult> RunTestSet([FromBody] TestSet testSet, CancellationToken cancellationToken)
        {
            var steps = GetSteps(testSet);
            return Ok(await this.RunSteps(steps, cancellationToken));
        }

        [HttpPost("RunTestSetFromFile")]
        public async Task<IActionResult> RunTestSetFromFile(string testSetFile, CancellationToken cancellationToken)
        {
            var testSet = Newtonsoft.Json.JsonConvert.DeserializeObject<TestSet>(_fileSystem.File.ReadAllText(testSetFile));
            var steps = GetSteps(testSet);
            return Ok(await this.RunSteps(steps, cancellationToken));
        }

        private async Task<Dictionary<string, string>> RunSteps(Step[] steps, CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, string>();

            foreach(var (step, index) in steps.Select((s, i) => (s, i)))
            {
                if(step.Action.HasValue)
                {
                    var result = string.Empty;
                    var hasException = false;
                    var exceptionType = null as string;
                    var isExpectedException = false;

                    try
                    {
                        result = await this.TestStep(step, cancellationToken);
                    }
                    catch(Exception exception)
                    {
                        hasException = true;

                        exceptionType = exception.GetType().Name.ToString();

                        isExpectedException = string.Equals(step.ExpectedException, exceptionType, StringComparison.OrdinalIgnoreCase);

                        var blockText = isExpectedException
                            ? "Expected exception"
                            : "[FAILURE]";

                        var exceptionMessage = exceptionType == "Exception"
                            ? exception.Message
                            : $"{exceptionType}: {exception.Message}";

                        if(string.IsNullOrEmpty(step.FailureMessage))
                        {
                            result = $"{blockText} - {exceptionMessage}";
                        }
                        else
                        {
                            result = $"{blockText} - {step.FailureMessage} - {exceptionMessage}";
                        }
                    }

                    var key = step.Title ?? step.Action.ToString();

                    if(results.ContainsKey(key))
                    {
                        key = $"{key}-{index}";
                    }

                    results.Add(key, result);

                    if(hasException && !isExpectedException && step.HaltOnError)
                    {
                        break;
                    }
                }
            }

            return results;
        }

        private async Task<string> TestStep(Step step, CancellationToken cancellationToken)
        {
            switch(step.Action)
            {
                case ActionCode.SshConnect:
                    return await this.SshConnect(step);

                case ActionCode.ScpConnect:
                    return await this.ScpConnect(step);

                case ActionCode.SftpConnect:
                    return await this.SftpConnect(step);

                case ActionCode.LocalFileExists:
                    return await this.LocalFileExists(step);

                case ActionCode.RemoteFileExists:
                    return await this.RemoteFileExists(step);

                case ActionCode.RemoteFileNotExists:
                    return await this.RemoteFileNotExists(step);

                case ActionCode.ScpUpload:
                    return await this.ScpUpload(step);

                case ActionCode.ScpDownload:
                    return await this.ScpDownload(step);

                case ActionCode.SftpUpload:
                    return await this.SftpUpload(step);

                case ActionCode.SftpDownload:
                    return await this.SftpDownload(step);

                case ActionCode.SftpUploadAsync:
                    return await this.SftpUploadAsync(step, cancellationToken);

                case ActionCode.SftpDownloadAsync:
                    return await this.SftpDownloadAsync(step, cancellationToken);

                case ActionCode.ChangePermissions:
                    return await this.ChangePermissions(step);

                case ActionCode.ChangeRemoteDirectory:
                    return await this.ChangeRemoteDirectory(step);

                case ActionCode.ChangeLocalDirectory:
                    return await this.ChangeLocalDirectory(step);

                case ActionCode.CreateRemoteFile:
                    return await this.CreateRemoteFile(step);

                case ActionCode.CreateLocalFile:
                    return await this.CreateLocalFile(step);

                case ActionCode.CreateRemoteDirectory:
                    return await this.CreateRemoteDirectory(step);

                case ActionCode.CreateLocalDirectory:
                    return await this.CreateLocalDirectory(step);

                case ActionCode.DeleteRemote:
                    return await this.DeleteRemote(step);

                case ActionCode.DeleteRemoteFile:
                    return await this.DeleteRemoteFile(step);

                case ActionCode.DeleteRemoteDirectory:
                    return await this.DeleteRemoteDirectory(step);

                case ActionCode.DeleteLocal:
                    return await this.DeleteLocal(step);

                case ActionCode.DeleteLocalFile:
                    return await this.DeleteLocalFile(step);

                case ActionCode.DeleteLocalDirectory:
                    return await this.DeleteLocalDirectory(step);

                case ActionCode.ReadBytes:
                    return await this.ReadBytes(step);

                case ActionCode.ReadLines:
                    return await this.ReadLines(step);

                case ActionCode.ReadLinesWithEncoding:
                    return await this.ReadLinesWithEncoding(step);

                case ActionCode.ReadText:
                    return await this.ReadText(step);

                case ActionCode.ReadTextWithEncoding:
                    return await this.ReadTextWithEncoding(step);

                case ActionCode.Touch:
                    return await this.Touch(step);

                case ActionCode.WriteBytes:
                    return await this.WriteBytes(step);

                case ActionCode.WriteLines:
                    return await this.WriteLines(step);

                case ActionCode.WriteLinesWithEncoding:
                    return await this.WriteLinesWithEncoding(step);

                case ActionCode.WriteText:
                    return await this.WriteText(step);

                case ActionCode.WriteTextWithEncoding:
                    return await this.WriteTextWithEncoding(step);

                case ActionCode.RunCommand:
                    return await this.RunCommand(step);

                case ActionCode.RunCommandAsync:
                    return await this.RunCommandAsync(step, cancellationToken);
            }

            throw new Exception($"Action {step.Action} not supported.");
        }


        private Task<string> SshConnect(Step step)
        {
            using(var client = this.CreateSshClient(step))
            {
                try
                {
                    client.Connect();

                    if(!client.IsConnected)
                    {
                        throw new SshConnectionException("Client not connected.");
                    }

                    return FromResult(() =>
                    {
                        return $"Connected to ssh host '{step.Host}'.";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> ScpConnect(Step step)
        {
            using(var client = this.CreateScpClient(step))
            {
                try
                {
                    client.Connect();

                    if(!client.IsConnected)
                    {
                        throw new SshConnectionException("Client not connected.");
                    }

                    return FromResult(() =>
                    {
                        return $"Connected to scp host '{step.Host}'.";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> SftpConnect(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                try
                {
                    client.Connect();

                    if(!client.IsConnected)
                    {
                        throw new SshConnectionException("Client not connected.");
                    }

                    return FromResult(() =>
                    {
                        return $"Connected to sftp host '{step.Host}'.";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }


        private Task<string> LocalFileExists(Step step)
        {
            return FromResult(() =>
            {

                if(!_fileSystem.File.Exists(step.LocalPath))
                {
                    throw new FileNotFoundException(step.LocalPath);
                }

                return $"File exists '{step.LocalPath}'";
            });
        }

        private Task<string> RemoteFileExists(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        if(!client.Exists(step.RemotePath))
                        {
                            throw new IOException($"File or directory does not exist '{step.RemotePath}'");
                        }

                        return $"File or directory exists '{step.RemotePath}'";
                    });

                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> RemoteFileNotExists(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        if(client.Exists(step.RemotePath))
                        {
                            throw new IOException($"File exists '{step.RemotePath}'");
                        }

                        return $"File does not exist '{step.RemotePath}'";
                    });

                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> ScpUpload(Step step)
        {
            using(var client = this.CreateScpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        if(step.UseStream)
                        {
                            using(var stream = _fileSystem.File.OpenRead(step.LocalPath))
                            {
                                client.Upload(stream, step.RemotePath);
                            }
                        }
                        else
                        {
                            client.Upload(step.LocalPath, step.RemotePath);
                        }

                        return $"Uploaded file '{step.RemotePath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> ScpDownload(Step step)
        {
            using(var client = this.CreateScpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        if(step.UseStream)
                        {
                            var fileMode = (!step.AllowOverwrite.HasValue || !step.AllowOverwrite.Value) ? FileMode.Create : FileMode.CreateNew;
                            using(var stream = _fileSystem.File.Open(step.LocalPath, fileMode, FileAccess.Write))
                            {
                                client.Download(step.RemotePath, stream);
                            }
                        }
                        else
                        {
                            if(step.AllowOverwrite.HasValue)
                            {
                                client.Download(step.RemotePath, step.LocalPath, step.AllowOverwrite.Value);
                            }
                            else
                            {
                                client.Download(step.RemotePath, step.LocalPath);
                            }
                        }

                        return $"Downloaded file '{step.LocalPath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> SftpUpload(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        if(step.UseStream)
                        {
                            using(var stream = _fileSystem.File.OpenRead(step.LocalPath))
                            {
                                if(step.AllowOverwrite.HasValue)
                                {
                                    client.UploadFile(stream, step.RemotePath, step.AllowOverwrite.Value);
                                }
                                else
                                {
                                    client.UploadFile(stream, step.RemotePath);
                                }
                            }

                        }
                        else
                        {
                            if(step.AllowOverwrite.HasValue)
                            {
                                client.UploadFile(step.LocalPath, step.RemotePath, step.AllowOverwrite.Value);
                            }
                            else
                            {
                                client.UploadFile(step.LocalPath, step.RemotePath);
                            }
                        }

                        return $"Uploaded file '{step.RemotePath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> SftpDownload(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        if(step.UseStream)
                        {
                            var fileMode = (!step.AllowOverwrite.HasValue || !step.AllowOverwrite.Value) ? FileMode.Create : FileMode.CreateNew;
                            using(var stream = _fileSystem.File.Open(step.LocalPath, fileMode, FileAccess.Write))
                            {
                                client.DownloadFile(step.RemotePath, stream);
                            }
                        }
                        else
                        {
                            if(step.AllowOverwrite.HasValue)
                            {
                                client.DownloadFile(step.RemotePath, step.LocalPath, step.AllowOverwrite.Value);
                            }
                            else
                            {
                                client.DownloadFile(step.RemotePath, step.LocalPath);
                            }
                        }

                        return $"Downloaded file '{step.LocalPath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private async Task<string> SftpUploadAsync(Step step, CancellationToken cancellationToken)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    if(step.UseStream)
                    {
                        using(var stream = _fileSystem.File.OpenRead(step.LocalPath))
                        {
                            if(step.AllowOverwrite.HasValue)
                            {
                                await client.UploadFileAsync(stream, step.RemotePath, step.AllowOverwrite.Value, cancellationToken);
                            }
                            else
                            {
                                await client.UploadFileAsync(stream, step.RemotePath, cancellationToken);
                            }
                        }

                    }
                    else
                    {
                        if(step.AllowOverwrite.HasValue)
                        {
                            await client.UploadFileAsync(step.LocalPath, step.RemotePath, step.AllowOverwrite.Value, cancellationToken);
                        }
                        else
                        {
                            await client.UploadFileAsync(step.LocalPath, step.RemotePath, cancellationToken);
                        }
                    }

                    return $"Uploaded file '{step.RemotePath}'";
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private async Task<string> SftpDownloadAsync(Step step, CancellationToken cancellationToken)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    if(step.UseStream)
                    {
                        var fileMode = (!step.AllowOverwrite.HasValue || !step.AllowOverwrite.Value) ? FileMode.Create : FileMode.CreateNew;
                        using(var stream = _fileSystem.File.Open(step.LocalPath, fileMode, FileAccess.Write))
                        {
                            await client.DownloadFileAsync(step.RemotePath, stream, cancellationToken);
                        }
                    }
                    else
                    {
                        if(step.AllowOverwrite.HasValue)
                        {
                            await client.DownloadFileAsync(step.RemotePath, step.LocalPath, step.AllowOverwrite.Value, cancellationToken);
                        }
                        else
                        {
                            await client.DownloadFileAsync(step.RemotePath, step.LocalPath, cancellationToken);
                        }
                    }

                    return $"Downloaded file '{step.LocalPath}'";
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> ChangePermissions(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        client.ChangePermissions(step.RemotePath, step.Permissions.Value);
                        return $"Changed permissions on '{step.RemotePath}' to {step.Permissions}.";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> ChangeLocalDirectory(Step step)
        {
            return FromResult(() =>
            {
                Environment.CurrentDirectory = step.LocalPath;
                return $"Changed local directory to '{step.LocalPath}'";
            });

        }

        private Task<string> ChangeRemoteDirectory(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        client.ChangeDirectory(step.RemotePath);
                        return $"Changed remote directory to '{step.RemotePath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> CreateLocalFile(Step step)
        {
            return FromResult(() =>
            {
                _fileSystem.File.Create(step.LocalPath);

                return $"Created local file '{step.LocalPath}'";
            });
        }

        private Task<string> CreateRemoteFile(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        client.Touch(step.RemotePath);

                        return $"Created remote file '{step.RemotePath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> CreateLocalDirectory(Step step)
        {
            return FromResult(() =>
            {
                _fileSystem.Directory.CreateDirectory(step.LocalPath);

                return $"Created local directory '{step.LocalPath}'";
            });
        }

        private Task<string> CreateRemoteDirectory(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        if(client.Exists(step.RemotePath))
                        {
                            return $"Remote directory '{step.RemotePath}' already exists.";
                        }

                        client.CreateDirectory(step.RemotePath);

                        return $"Created remote directory '{step.RemotePath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> DeleteRemote(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        if(step.Recursive.HasValue)
                        {
                            client.Delete(step.RemotePath, step.Recursive.Value);
                        }
                        else
                        {
                            client.Delete(step.RemotePath);
                        }

                        return $"Deleted remote path '{step.RemotePath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> DeleteRemoteFile(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        client.DeleteFile(step.RemotePath);
                        return $"Deleted remote file '{step.RemotePath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> DeleteRemoteDirectory(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        if(step.Recursive.HasValue)
                        {
                            client.DeleteDirectory(step.RemotePath, step.Recursive.Value);
                        }
                        else
                        {
                            client.DeleteDirectory(step.RemotePath);
                        }

                        return $"Deleted remote directory '{step.RemotePath}'";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> DeleteLocal(Step step)
        {
            return FromResult(() =>
            {
                if(_fileSystem.File.Exists(step.LocalPath))
                {
                    _fileSystem.File.Delete(step.LocalPath);
                }
                else
                {

                    _fileSystem.Directory.Delete(step.LocalPath, step.Recursive ?? false);
                }

                return $"Deleted local path '{step.LocalPath}'";
            });
        }

        private Task<string> DeleteLocalFile(Step step)
        {
            return FromResult(() =>
            {
                _fileSystem.File.Delete(step.LocalPath);

                return $"Deleted local file '{step.LocalPath}'";
            });
        }

        private Task<string> DeleteLocalDirectory(Step step)
        {
            return FromResult(() =>
            {
                _fileSystem.Directory.Delete(step.LocalPath, step.Recursive ?? false);

                return $"Deleted local directory '{step.LocalPath}'";
            });
        }

        private Task<string> ReadBytes(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var bytes = client.ReadAllBytes(step.RemotePath);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({bytes.Length} bytes)";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> ReadLines(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var lines = client.ReadAllLines(step.RemotePath);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({lines.Length} lines)";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> ReadLinesWithEncoding(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var encoding = step.Encoding.ToEncoding();
                        var lines = client.ReadAllLines(step.RemotePath, encoding);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({lines.Length} lines - {step.Encoding})";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> ReadText(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var text = client.ReadAllText(step.RemotePath);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({text.Length} chars)";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> ReadTextWithEncoding(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var encoding = step.Encoding.ToEncoding();
                        var text = client.ReadAllText(step.RemotePath, encoding);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({text.Length} chars - {step.Encoding})";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> Touch(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        client.Touch(step.RemotePath);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath}";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> WriteBytes(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var bytes = _fileSystem.File.ReadAllBytes(step.LocalPath);
                        client.WriteAllBytes(step.RemotePath, bytes);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({bytes.Length} bytes)";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> WriteLines(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var lines = _fileSystem.File.ReadAllLines(step.LocalPath);
                        client.WriteAllLines(step.RemotePath, lines);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({lines.Length} lines)";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> WriteLinesWithEncoding(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var encoding = step.Encoding.ToEncoding();
                        var lines = _fileSystem.File.ReadAllLines(step.LocalPath, encoding);
                        client.WriteAllLines(step.RemotePath, lines, encoding);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({lines.Length} lines - {step.Encoding})";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> WriteText(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var text = _fileSystem.File.ReadAllText(step.LocalPath);
                        client.WriteAllText(step.RemotePath, text);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({text.Length} chars)";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> WriteTextWithEncoding(Step step)
        {
            using(var client = this.CreateSftpClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var encoding = step.Encoding.ToEncoding();
                        var text = _fileSystem.File.ReadAllText(step.LocalPath, encoding);
                        client.WriteAllText(step.RemotePath, text, encoding);
                        return $"{step.UserName}@{step.Host}:{step.RemotePath} ({text.Length} chars - {step.Encoding})";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private Task<string> RunCommand(Step step)
        {
            using(var client = this.CreateSshClient(step))
            {
                client.Connect();

                try
                {
                    return FromResult(() =>
                    {
                        var exitStatus = client.RunCommand(step.CommandText);

                        if(step.ExpectedExitStatus.HasValue && step.ExpectedExitStatus.Value != exitStatus)
                        {
                            throw new Exception($"Expected exit status ({step.ExpectedExitStatus}) but got exit status ({exitStatus})");
                        }

                        return $"ExitStatus: {exitStatus}: {step.CommandText}";
                    });
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        private async Task<string> RunCommandAsync(Step step, CancellationToken cancellationToken)
        {
            using(var client = this.CreateSshClient(step))
            {
                client.Connect();

                try
                {
                    var exitStatus = await client.RunCommandAsync(step.CommandText, cancellationToken);

                    if(step.ExpectedExitStatus.HasValue && step.ExpectedExitStatus.Value != exitStatus)
                    {
                        throw new Exception($"Expected exit status ({step.ExpectedExitStatus}) but got exit status ({exitStatus})");
                    }

                    return $"ExitStatus: {exitStatus}: {step.CommandText}";
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }


        private ISshClient CreateSshClient(Step step)
        {
            if(!string.IsNullOrWhiteSpace(step.Password))
            {
                if(step.Port.HasValue)
                {
                    return _sshClientFactory.CreateSshClient(step.Host, step.Port.Value, step.UserName, step.Password);
                }
                else
                {
                    return _sshClientFactory.CreateSshClient(step.Host, step.UserName, step.Password);
                }
            }
            else if(!string.IsNullOrWhiteSpace(step.PrivateKeyFile))
            {
                if(step.Port.HasValue)
                {
                    return _sshClientFactory.CreateSshClient(step.Host, step.Port.Value, step.UserName, new PrivateKey(step.PrivateKeyFile));
                }
                else
                {
                    return _sshClientFactory.CreateSshClient(step.Host, step.UserName, new PrivateKey(step.PrivateKeyFile));
                }
            }
            else if(step.Port.HasValue)
            {
                return _sshClientFactory.CreateSshClient(step.Host, step.Port.Value, step.UserName, new PrivateKey(_defaultPrivateKeyStream));
            }

            return _sshClientFactory.CreateSshClient(step.Host, step.UserName, new PrivateKey(_defaultPrivateKeyStream));
        }

        private IScpClient CreateScpClient(Step step)
        {
            if(!string.IsNullOrWhiteSpace(step.Password))
            {
                if(step.Port.HasValue)
                {
                    return _sshClientFactory.CreateScpClient(step.Host, step.Port.Value, step.UserName, step.Password);
                }
                else
                {
                    return _sshClientFactory.CreateScpClient(step.Host, step.UserName, step.Password);
                }
            }
            else if(!string.IsNullOrWhiteSpace(step.PrivateKeyFile))
            {
                if(step.Port.HasValue)
                {
                    return _sshClientFactory.CreateScpClient(step.Host, step.Port.Value, step.UserName, new PrivateKey(step.PrivateKeyFile));
                }
                else
                {
                    return _sshClientFactory.CreateScpClient(step.Host, step.UserName, new PrivateKey(step.PrivateKeyFile));
                }
            }
            else if(step.Port.HasValue)
            {
                return _sshClientFactory.CreateScpClient(step.Host, step.Port.Value, step.UserName, new PrivateKey(DefaultPrivateKeyFile));
            }

            return _sshClientFactory.CreateScpClient(step.Host, step.UserName, new PrivateKey(DefaultPrivateKeyFile));
        }

        private ISftpClient CreateSftpClient(Step step)
        {
            if(!string.IsNullOrWhiteSpace(step.Password))
            {
                if(step.Port.HasValue)
                {
                    return _sshClientFactory.CreateSftpClient(step.Host, step.Port.Value, step.UserName, step.Password);
                }
                else
                {
                    return _sshClientFactory.CreateSftpClient(step.Host, step.UserName, step.Password);
                }
            }
            else if(!string.IsNullOrWhiteSpace(step.PrivateKeyFile))
            {
                if(step.Port.HasValue)
                {
                    return _sshClientFactory.CreateSftpClient(step.Host, step.Port.Value, step.UserName, new PrivateKey(step.PrivateKeyFile));
                }
                else
                {
                    return _sshClientFactory.CreateSftpClient(step.Host, step.UserName, new PrivateKey(step.PrivateKeyFile));
                }
            }
            else if(step.Port.HasValue)
            {
                return _sshClientFactory.CreateSftpClient(step.Host, step.Port.Value, step.UserName, new PrivateKey(DefaultPrivateKeyFile));
            }

            return _sshClientFactory.CreateSftpClient(step.Host, step.UserName, new PrivateKey(DefaultPrivateKeyFile));
        }

        private Step[] GetSteps(TestSet testSet)
        {
            var steps = new List<Step>();

            foreach(var step in testSet.Steps)
            {
                steps.Add(BuildStep(step, testSet.Connections));
            }

            return steps.ToArray();
        }

        private Step BuildStep(Step sourceStep, Connection[] connections)
        {
            var step = sourceStep.Clone();

            if(!string.IsNullOrEmpty(step.Connection))
            {
                var connection = connections.FirstOrDefault(c => string.Equals(c.Name, step.Connection, StringComparison.OrdinalIgnoreCase));
                if(connection == null)
                {
                    throw new Exception($"Connection '{step.Connection}' not found.");
                }

                step.Host = step.Host ?? connection.Host;
                step.Port = step.Port ?? connection.Port;
                step.UserName = step.UserName ?? connection.UserName;
                step.Password = step.Password ?? connection.Password;
                step.PrivateKeyFile = step.PrivateKeyFile ?? connection.PrivateKeyFile;
            }

            return step;
        }

        private Task<T> FromResult<T>(Func<T> func)
        {
            return Task.FromResult(func());
        }
    }

    static class Extensions
    {
        public static Encoding ToEncoding(this string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                throw new Exception("Value required for encoding.");
            }

            return Encoding.GetEncoding(value);
        }
    }
}
