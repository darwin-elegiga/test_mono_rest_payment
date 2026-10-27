using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Configs;

namespace VPay.DocSys.Generation
{
    public class DropToPrintService : IDropToPrintService
    {

        private IProcessRunner _processRunner;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private DropToPrintSettings _configuration;

        public DropToPrintService(
            IProcessRunner processRunner,
            IFileSystem fileSystem,
            ILogger<DropToPrintService> logger,
            DropToPrintSettings configuration)
        {
            this._processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            this._fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        public async Task<int> CallDropToPrintPython(string statusFilePath)
        {
            ProcessResult result;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                { "StatusFilePath", statusFilePath.Sanitize() }
            }))
            {
                try
                {
                    string workingDirectory = Path.GetDirectoryName(_configuration.PythonScript);
                    string python = @"python3";
                    List<string> newArgs = new List<string>();
                    newArgs.Add(_configuration.PythonScript);
                    newArgs.Add(statusFilePath);

                    result = await _processRunner.RunProcessAsync(python, false, workingDirectory, newArgs.ToArray());
                    _logger.LogInformation(JsonConvert.SerializeObject(result).LogSanitize());
                }
                catch (Exception ex)
                {
                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        { "Exception", ex.Message.Sanitize() }
                    }))
                    {
                        var exWithFileName = new Exception($"Exception for sts file: {statusFilePath}", ex);

                        _logger.LogError(ex, exWithFileName.Message.Sanitize());
                        throw exWithFileName;
                    }
                }
            }

            return result.ExitCode;
        }
    }
}
