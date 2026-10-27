using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data
{
    [ExcludeFromCodeCoverage]
    public class SqlDataConnection : DataConnection<SqlConnection>
    {
        /* Boiler plate code copied from VPay.ReconReports master/v0.4.0/Commit 30a20b19 */

        public SqlDataConnection(
            ConnectionConfig connectionConfig,
            IRetryPolicyRegistry policyRegistry,
            ILogger<SqlDataConnection> logger) : this(ConvertToConnectionString(connectionConfig, logger), policyRegistry, logger)
        {
            DefaultCommandTimeout = connectionConfig?.DefaultCommandTimeout;
        }

        public SqlDataConnection(string connectionString, IRetryPolicyRegistry policyRegistry, ILogger<SqlDataConnection> logger) : base(connectionString, logger)
        {
            RetryPolicy = policyRegistry;
        }

        public override IRetryPolicyRegistry RetryPolicy { get; }

        protected override SqlConnection GetConnectionObject()
        {
            return new SqlConnection(ConnectionString);
        }

        private static string ConvertToConnectionString(ConnectionConfig connectionConfig, ILogger logger)
        {
            if (connectionConfig == null)
            {
                logger.LogWarning($"Creating SqlConnection string: {nameof(connectionConfig)} is null");
                return null;
            }

            var builder = new SqlConnectionStringBuilder(connectionConfig.BaseConnectionString)
            {
                DataSource = connectionConfig.Hostname,
                InitialCatalog = connectionConfig.Database,
                UserID = connectionConfig.Username,
                Password = connectionConfig.Password,
                IntegratedSecurity = connectionConfig.IntegratedSecurity
            };

            return builder.ToString();
        }
    }
}
