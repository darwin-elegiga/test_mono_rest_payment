using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using System.Collections.Generic;
using System.IO.Abstractions;
using VPay.DocSys.Parsing.Common.Comparers;
using System.Diagnostics;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Parsing
{
    public class OtbService : IOtbService
    {
        private readonly StatusFileSettings _statusFileSettings;
        private readonly IOTBParserService _otbParserService;
        private readonly IOtbDataService _otbDataService;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<OtbService> _logger;

        public OtbService(
            StatusFileSettings statusFileSettings,
            IOTBParserService otbParserService,
            IOtbDataService otbDataService,
            IFileSystem fileSystem,
            ILogger<OtbService> logger)
        {
            _statusFileSettings = statusFileSettings ?? throw new ArgumentNullException(nameof(statusFileSettings));
            _otbParserService = otbParserService ?? throw new ArgumentNullException(nameof(otbParserService));
            _otbDataService = otbDataService ?? throw new ArgumentNullException(nameof(otbDataService));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!_otbDataService.UseApi && !_otbDataService.UseFile)
            {
                throw new ArgumentException($"{nameof(OtbDataServiceSettings.UseFile)} and {nameof(OtbDataServiceSettings.UseApi)} cannot both be false");
            }
        }

        public async Task<OtbRecords> GetOtbRecordsAsync(string clientCode, string[] beCodes)
        {
            using (_logger.BeginScope(new
            {
                _otbDataService.UseFile,
                _otbDataService.UseApi,
            }.ToDictionary()))
            {
                ThrowIf.IsNullOrWhiteSpace(clientCode, nameof(clientCode));
                ThrowIf.IsNullOrEmpty(beCodes, nameof(beCodes));

                var apiRecords = await GetApiRecordsAsync(clientCode.ToUpper(), beCodes).ConfigureAwait(false);

                if (_otbDataService.UseFile)
                {
                    var fileRecords = GetFileRecords(clientCode.ToLower().EnsureIsSmbSafeName());

                    CompareRecords(apiRecords, fileRecords);

                    ValidateBillingEntities(beCodes, fileRecords.Keys, "FILE");

                    return fileRecords;
                }

                ValidateBillingEntities(beCodes, apiRecords.Keys, "API");

                return apiRecords;
            }
        }

        private async Task<OtbRecords> GetApiRecordsAsync(string clientCode, string[] beCodes)
        {
            var records = new OtbRecords();

            if (_otbDataService.UseApi)
            {
                var timer = Stopwatch.StartNew();

                var otbApiData = await _otbDataService.GetOtbDataAsync(clientCode, beCodes).ConfigureAwait(false);

                timer.Stop();

                var duration = timer.Elapsed.TotalMilliseconds.ToString();
                _logger.LogInformation("OTB API endpoint call completed in {Elapsed} ms.", duration);

                records = new OtbRecords(otbApiData.BillingEntities);
            }

            return records;
        }

        private OtbRecords GetFileRecords(string clientCode)
        {
            var otbFilePath = _fileSystem.Path.Combine(
                _statusFileSettings.PythonHome,
                clientCode,
                clientCode + ".otb");

            // Parse OTB Data from OTB File.
            return new OtbRecords(_otbParserService.ParseOTBFile(otbFilePath));
        }

        private void CompareRecords(OtbRecords apiRecords, OtbRecords fileRecords)
        {
            if (!_otbDataService.UseApi)
            {
                _logger.LogInformation("OTB API comparison is disabled");
                return;
            }

            var timer = Stopwatch.StartNew();

            // Compare OTB Data from OTB API with OTB Data from OTB File and log any discrepancies.
            foreach (var be in apiRecords.Keys)
            {
                var discrepancies = OtbDataComparer.CompareOtbData(be, apiRecords, fileRecords);

                using (_logger.BeginScope(new { BillingEntityCode = be }.ToDictionary()))
                {
                    if (discrepancies.Any())
                    {
                        _logger.LogInformation("OTB API found differences with OTB File for BE {BillingEntityCode}.", be);

                        foreach (var discrepancy in discrepancies)
                        {
                            _logger.LogInformation("OTB API discrepancy: " + discrepancy);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("OTB API matches data from OTB File for BE {BillingEntityCode}.", be);
                    }
                }
            }

            timer.Stop();
            var duration = timer.Elapsed.TotalMilliseconds.ToString();
            _logger.LogInformation("OTB API comparison completed in {Elapsed} ms.", duration);
        }

        private void ValidateBillingEntities(string[] beCodes, ICollection<string> recordKeys, string source)
        {
            var missingRecords = beCodes.Except(recordKeys);

            if (missingRecords.Any())
            {
                foreach (var be in missingRecords)
                {
                    _logger.LogError("OTB {Source} missing BE {BillingEntityCode}.", source, be);
                }

                throw new KeyNotFoundException($"OTB {source} missing Billing Entities: {string.Join(',', missingRecords)}");
            }
        }
    }
}
