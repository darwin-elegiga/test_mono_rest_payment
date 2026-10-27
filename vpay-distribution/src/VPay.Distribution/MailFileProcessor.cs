using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using VPay.Distribution.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.Distribution
{
    public class MailFileProcessor : DistributionProcessor, IMailFileProcessor
    {
        private readonly Phaze04Settings _phaze04Settings;

        public MailFileProcessor(IConfiguration configuration, IFileSystem fileSystem, IDocSysHttpClient docSysClient, IDistributionRepository distributionRepository,
            ApiSettings apiSettings, Phaze04Settings phaze04Settings, ILogger<MailFileProcessor> logger)
            : base(configuration, fileSystem, docSysClient, distributionRepository, apiSettings, logger)
        {
            _phaze04Settings = phaze04Settings;
        }

        public override async Task<DistributionProcessResult> Process(DocDistMsg message, bool createControlFile = true)
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"Processor", this.GetType().Name },
                {"STSFile", message.StatusFilePath},
                {"TPA", message.TPA },
                {"ClientCode", message.TPA },
            }))
            {
                if (!(await _distributionRepository.TryUpdateDistributionStatus(message.PaymentStatusFileId, DistributionMethods.Mail)))
                {
                    _logger.LogInformation($"Mail already distributed for file {message.PaymentStatusFileId}.");
                    return new DistributionProcessResult("MAIL");
                }
                try
                {
                    await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.MailDistBegin);

                    _logger.LogInformation($"Initiating Mail distribution for {message.StatusFilePath}");

                    var getConfig = _docSysClient.GetDocSysConfig(message.TPA);

                    var directory = _fileSystem.Path.GetDirectoryName(message.StatusFilePath);
                    var fileName = _fileSystem.Path.GetFileName(message.StatusFilePath);
                    var mailDirectory = _fileSystem.Path.Combine(directory, "mailStatus");
                    var sourceSts = new StatusFile(File.ReadAllLines(message.StatusFilePath));
                    var mailTransations = message.Transactions.Select(t => t.TxnId).ToList();
                    List<StatusRecord> mailRecords = new List<StatusRecord>();
                    foreach (var stsRecord in sourceSts.Records)
                    {
                        var transactionId = Convert.ToInt64(stsRecord.TXID.RemoveLeadingZeroes());
                        if (mailTransations.Contains(transactionId))
                        {
                            stsRecord.DIST = message.Transactions.First(t => t.TxnId == transactionId).DistType
                                .ToString();
                            mailRecords.Add(stsRecord);
                        }
                    }

                    if (mailRecords.Count == 0)
                    {
                        return new DistributionProcessResult("MAIL");
                    }
                    _fileSystem.Directory.CreateDirectory(mailDirectory);
                    var mailSTSFile = _fileSystem.Path.Combine(mailDirectory, fileName);

                    sourceSts.Records = mailRecords;
                    _fileSystem.File.WriteAllText(mailSTSFile, sourceSts.ToString());
                    var outputFile = _fileSystem.Path.Combine(directory, fileName.Replace(".sts", ".prt"));
                    var docSysConfig = await getConfig;
                    var controlBase = GetConfigValue<string>("ControlPath", docSysConfig);
                    var logFileName = Path.Combine(directory, "Phaze04_" + Path.GetFileNameWithoutExtension(message.StatusFilePath).Split('_')[1] +
                                                              $"_{DateTime.Now:yyyyMMdd}.log");

                    var workBase = docSysConfig.EnvironmentSettings["workbase"];
                    var workFolder = CreateWorkDirectory(workBase);
                    string python = @"python3";
                    List<string> newArgs = new List<string>();
                    newArgs.Add(_phaze04Settings.ScriptName);
                    newArgs.Add(mailSTSFile);
                    newArgs.Add(workFolder);
                    newArgs.Add(outputFile);
                    if (fileName.StartsWith("R"))
                    {
                        newArgs.Add("--unload-reissue");
                    }

                    _logger.LogInformation("Command Line: python3 " + string.Join(" ", newArgs));
                    var result = await RunProcessAsync(python, false, _phaze04Settings.ScriptPath, logFileName, docSysConfig.EnvironmentSettings,
                        newArgs.ToArray());
                    _fileSystem.Directory.Delete(workFolder, true);
                    if (result.ExitCode != 0)
                    {
                        await _distributionRepository.ClearDistributionStatus(message.PaymentStatusFileId, DistributionMethods.Mail);
                        await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.Error);
                        throw new Exception($"An error occurred in {_phaze04Settings.ScriptName}: {result.Output}");
                    }

                    var controlFile = createControlFile
                        ? CreateControlFile(outputFile, controlBase)
                        : null;

                    if (controlFile is null)
                    {
                        _logger.LogError("No control file created for {OutputFile}", outputFile);
                    }
                    else
                    {
                        _logger.LogInformation("Created control file {ControlFile}", controlFile);
                    }

                    await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.MailDistComplete);

                    return new DistributionProcessResult("MAIL")
                    {
                        Count = message.BatchSize,
                        FileName = outputFile,
                        ControlFileName = controlFile,
                    };
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, exception.Message);
                    await _distributionRepository.ClearDistributionStatus(message.PaymentStatusFileId, DistributionMethods.Mail);
                    await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.Error);
                    throw;
                }
            }
        }


        public string CreateWorkDirectory(string workBase)
        {
            Guid tempKey = Guid.NewGuid();
            var tempFolder = Path.Combine(workBase, tempKey.ToString());
            _fileSystem.Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }

        public async Task<ProcessResult> RunProcessAsync(string fileName, bool useStdIn, string workingDirectory, string logFile, Dictionary<string, string> envVariables, params string[] args)
        {
            using (var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {

                    FileName = fileName,
                    Arguments = args == null ? "" : String.Join(" ", args.Take(useStdIn ? args.Length - 1 : args.Length).Where(x => !String.IsNullOrWhiteSpace(x))),
                    UseShellExecute = false,
                    RedirectStandardInput = useStdIn,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }

            })
            {
                foreach (var key in envVariables.Keys)
                {
                    process.StartInfo.EnvironmentVariables[key] = envVariables[key];
                }
                process.StartInfo.EnvironmentVariables["PYTHONPATH"] = "/home/bogner/python3/SEScore";
                process.StartInfo.EnvironmentVariables["log_file"] = logFile;
                return await ProcessResult(useStdIn, workingDirectory, args, process);
            }
        }

        private static async Task<ProcessResult> ProcessResult(bool useStdIn, string workingDirectory, string[] args, Process process)
        {
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            process.Start();
            if (useStdIn)
            {
                StreamWriter myStreamWriter = process.StandardInput;
                myStreamWriter.WriteLine(args[args.Length - 1]);
                myStreamWriter.WriteLine(string.Empty);
                myStreamWriter.Close();
            }

            var tout = process.StandardOutput.ReadToEndAsync();
            var terr = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(tout, terr);

            process.StandardOutput.Close();
            process.StandardError.Close();

            await Task.Run(() => process.WaitForExit());

            var stdout = await tout;
            var stderr = await terr;

            if (!String.IsNullOrWhiteSpace(stderr))
            {
                throw new ApplicationException(stderr);
            }

            return new ProcessResult()
            {
                ExitCode = process.ExitCode,
                Output = stdout
            };
        }
    }
}
