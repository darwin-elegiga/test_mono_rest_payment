using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using VPay.Optum.Nc835FileProcessor.Application.Models;

namespace VPay.Optum.Nc835FileProcessor.Application.HealthChecks
{
    public class BaseFolderPathHealthCheck : IHealthCheck
    {
        private readonly Nc835FileSettings _settings;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<BaseFolderPathHealthCheck> _logger;

        public BaseFolderPathHealthCheck(
            Nc835FileSettings enrollmentFileSettings,
            IFileSystem fileSystem,
            ILogger<BaseFolderPathHealthCheck> logger)
        {
            _settings = enrollmentFileSettings ?? throw new ArgumentNullException(nameof(enrollmentFileSettings));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using var ls = _logger.BeginScope(new Dictionary<string, object>
            {
                { nameof(_settings.BaseFolderPath), _settings.BaseFolderPath }
            });

            HealthCheckResult hcr;

            if (_fileSystem.Directory.Exists(_settings.BaseFolderPath))
            {
                var message = $"{nameof(_settings.BaseFolderPath)} found";

                hcr = new HealthCheckResult(HealthStatus.Healthy, message);
            }
            else
            {
                var message = $"{nameof(_settings.BaseFolderPath)} not found";
                throw new DirectoryNotFoundException(message);
            }

            return Task.FromResult(hcr);
        }
    }
}
