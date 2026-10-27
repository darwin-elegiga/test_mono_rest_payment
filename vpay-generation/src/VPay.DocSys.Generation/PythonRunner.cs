using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Generation.Configs;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class PythonRunner : IPythonRunner
    {
        private readonly CustomSendBackOperations _customSendBackOperations;
        private readonly IFileSystem _fileSystem;
        private readonly IProcessRunner _processRunner;
        private readonly ILogger<PythonRunner> _logger;
        private PythonSettings _settings;

        public PythonRunner(
            CustomSendBackOperations customSendBackOperations,
            IFileSystem fileSystem,
            IProcessRunner processRunner,
            ILogger<PythonRunner> logger,
            PythonSettings settings)
        {
            ThrowIf.IsNull(customSendBackOperations, nameof(customSendBackOperations));
            ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ThrowIf.IsNull(processRunner, nameof(processRunner));
            ThrowIf.IsNull(logger, nameof(logger));

            _customSendBackOperations = customSendBackOperations;
            _fileSystem = fileSystem;
            _processRunner = processRunner;
            _logger = logger;
            _settings = settings;
        }

        public string GetPythonExecutable(string pythonEnvVersion)
        {
            return this.getPythonExecutable(pythonEnvVersion);
        }

        private string getPythonExecutable(string pythonEnvVersion)
        {
            string python;
            if (!_settings.PythonExecutableMap.TryGetValue(pythonEnvVersion, out python)) {
                python = _settings.DefaultPythonExecutable;
            }
            return python;
        }

        public async Task<string> GetVersionInfo(string pythonEnvVersion) {
            string python = getPythonExecutable(pythonEnvVersion);
            List<string> args = new List<string>();
            args.Add("-c");
            args.Add("\"import sys; print(sys.version)\"");
            try {
                var result = await _processRunner.RunProcessAsync(python, string.Empty, args.ToArray());
                return result.Output;
            }
            catch (Exception e) {
                return "Couldn't Get Version Info: \n" + e.ToString();
            }
        }

        public async Task<List<string>> GetPythonPackageList(string pythonEnvVersion) {
            string python = getPythonExecutable(pythonEnvVersion);
            List<string> args = new List<string>();
            args.Add("-m");
            args.Add("pip");
            args.Add("freeze");
            try {
                var result = await _processRunner.RunProcessAsync(python, string.Empty, args.ToArray());
                var lines = result.Output.Split('\n');
                return new List<string>(lines);
            }
            catch (Exception) {
                return new List<string>();
            }
        }

        public async Task<string> CreateVPayge(string scriptName, string method, string outputFile, CoverPageData coverPageData, string tpaConfigFile, string pythonVersion)
        {
            string python = getPythonExecutable(pythonVersion);
            List<string> newArgs = new List<string>();
            newArgs.Add("/home/bogner/vpayge_maker/vpayge_maker_runner/__main__.py");
            newArgs.Add(method);
            newArgs.Add(outputFile);
            newArgs.Add("-l");
            newArgs.Add("/SRVFS/bogner/logs/docsys.log");
            newArgs.Add("-f");
            newArgs.Add(scriptName);
            if (!String.IsNullOrWhiteSpace(tpaConfigFile))
            {
                newArgs.Add("-t");
                newArgs.Add(tpaConfigFile);
            }
            _logger.LogInformation("{PythonCommand} {PythonArguments}",
                python.Replace(Environment.NewLine, ""),
                string.Join(" ", newArgs).Replace(Environment.NewLine, ""));
            newArgs.Add($"{JsonConvert.SerializeObject(coverPageData)}");
            var result = await _processRunner.RunProcessAsync(python, true, string.Empty, newArgs.ToArray());

            if (result.ExitCode == 0)
            {
                _logger.LogInformation("Created {OutputFileName} files", outputFile.Replace(Environment.NewLine, ""));

                return outputFile;
            }

            throw new ProcessRunnerException(result.ExitCode, $"Cover maker python runner '{scriptName}' failed to generate file");
        }

        public async Task<string> OverlayEob(string scriptName, string pdfFile, bool isCheck, string overlayId, DateTime overlayDate)
        {
            string python = getPythonExecutable("3");
            List<string> newArgs = new List<string>();
            newArgs.Add(scriptName);
            newArgs.Add(pdfFile);
            newArgs.Add(isCheck ? "T" : "F");
            newArgs.Add(overlayId);
            newArgs.Add(overlayDate.ToString("yyyy-MM-dd"));
            newArgs.Add("-o");
            newArgs.Add(pdfFile);

            _logger.LogInformation("{PythonCommand} {PythonArguments}",
                python.Replace(Environment.NewLine, ""),
                string.Join(" ", newArgs).Replace(Environment.NewLine, ""));

            var result = await _processRunner.RunProcessAsync(python, string.Empty, newArgs.ToArray());

            if (result.ExitCode != 0)
            {
                throw new ProcessRunnerException(result.ExitCode, $"Eob overlayer python runner '{scriptName}' failed to generate file");
            }

            _logger.LogInformation("Created {PdfFile} file", pdfFile);

            return pdfFile;
        }

        public async Task<string> CreateEob(string scriptName, string outputFile, string eobXmlFile, string auxFiles, string payType, string pythonVersion, string overlayId, DateTime overlayDate, bool isCheck)
        {
            string python = getPythonExecutable(pythonVersion);
            List<string> newArgs = new List<string>();
            newArgs.Add("/home/bogner/vpayge_maker/eob_maker_runner/__main__.py");
            newArgs.Add(scriptName);
            newArgs.Add(outputFile);
            newArgs.Add(eobXmlFile);
            newArgs.Add(isCheck ? "T" : "F");
            newArgs.Add(overlayId);
            newArgs.Add($"{overlayDate:yyyy-MM-dd}");
            newArgs.Add("-l");
            newArgs.Add("/SRVFS/bogner/logs/docsys.log");
            newArgs.Add("-p");
            newArgs.Add(payType);
            if (!string.IsNullOrEmpty(auxFiles))
            {
                newArgs.Add("-a");
                newArgs.Add(auxFiles);
            }

            _logger.LogInformation("{PythonCommand} {PythonArguments}",
                python.Replace(Environment.NewLine, ""),
                string.Join(" ", newArgs).Replace(Environment.NewLine, ""));

            var result = await _processRunner.RunProcessAsync(python, string.Empty, newArgs.ToArray());

            if (result.ExitCode != 0)
            {
                throw new ProcessRunnerException(result.ExitCode, $"Eob maker python runner '{scriptName}' failed to generate file");
            }

            _logger.LogInformation("Created {OutputFile} file", outputFile);

            return outputFile;
        }

        public async Task<string[]> CreateCustomSendBacks(string checkInfoXmlFilePath)
        {
            ThrowIf.IsNull(checkInfoXmlFilePath, nameof(checkInfoXmlFilePath));

            var python = @"python3";

            var newArgs = new[]
            {
                _customSendBackOperations.CheckMaker,
                checkInfoXmlFilePath,
            };

            _logger.LogInformation($"{python} {string.Join(" ", newArgs)}");

            var result = await _processRunner.RunProcessAsync(python, false, string.Empty, newArgs);

            _logger.LogInformation($"Created {result.Output} files");

            return result.Output
                .Split(new[] {Environment.NewLine, " "}, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
