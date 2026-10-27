using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    /// <summary>
    /// Tests for the <see cref="StatusFileTracker"/> class.
    /// </summary>
    public class StatusFileTrackingTests
    {
        private readonly string _connectionString;
        private readonly DocSysConnectionSettings _connectionSettings;
        private readonly SqlServerPolicyHolder _sqlServerPolicies;
        private readonly Edi835ConfigurationCache _edi835ConfigurationCache;
        private readonly ILogger<StatusFileTracker> _logger = new NullLogger<StatusFileTracker>();
        private StatusFileTracker _statusFileTracker;

        public StatusFileTrackingTests()
        {
            _connectionString = "Server=localhost;Database=DocSys;User Id=sa;Password=Your_password123;";
            _connectionSettings = new DocSysConnectionSettings(_connectionString);
            _edi835ConfigurationCache = new Edi835ConfigurationCache(new Mock<IServiceScopeFactory>().Object);
            _sqlServerPolicies = new Mock<SqlServerPolicyHolder>(_logger, LogLevel.Information).Object;
            _statusFileTracker = new StatusFileTracker(_connectionSettings, _edi835ConfigurationCache, _logger);
        }

        public class Constructor : StatusFileTrackingTests
        {
            [Fact]
            public void ThrowsOnNullConfigurationCache()
            {
                Assert.Throws<ArgumentNullException>(() => new StatusFileTracker(_connectionSettings, null, _logger));
            }
        }
    }
}
