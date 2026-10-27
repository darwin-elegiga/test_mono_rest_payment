using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql;

public class SqlDataConnection : DataConnection<SqlConnection>
{
    public SqlDataConnection(
        ConnectionConfig connectionConfig,
        IRetryPolicyRegistry policyRegistry) : this(ConvertToConnectionString(connectionConfig), policyRegistry)
    {
        DefaultCommandTimeout = connectionConfig?.DefaultCommandTimeout;
    }

    public SqlDataConnection(string? connectionString, IRetryPolicyRegistry policyRegistry) : base(connectionString)
    {
        RetryPolicy = policyRegistry;
    }

    public override IRetryPolicyRegistry RetryPolicy { get; }

    protected override SqlConnection GetConnectionObject()
    {
        return new SqlConnection(ConnectionString);
    }

    private static string? ConvertToConnectionString(ConnectionConfig connectionConfig)
    {
        if (connectionConfig == null)
        {
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
