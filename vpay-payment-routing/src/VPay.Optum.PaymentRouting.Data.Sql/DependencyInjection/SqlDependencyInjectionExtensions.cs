using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.DependencyInjection
{
    /// <summary>
    /// Extensions related to dependency injection for this assembly.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class SqlDependencyInjectionExtensions
    {
        /// <summary>
        /// Add the SQL services to the service collection.
        /// </summary>
        /// <param name="serviceCollection">The collection to add the services to</param>
        /// <param name="configureConnection">An action that takes the connection to configure.</param>
        /// <returns>The configuration builder for additional SQL options.</returns>
        public static ISqlConfigurationBuilder AddSql(this IServiceCollection serviceCollection, Action<ConnectionConfig> configureConnection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            var connection = new ConnectionConfig();
            configureConnection(connection);

            serviceCollection.TryAddScoped<IDataConnection<SqlConnection>>(s => ActivatorUtilities.CreateInstance<SqlDataConnection>(s, connection));
            serviceCollection.TryAddScoped<IDbUnitOfWork>(x => (IDbUnitOfWork)x.GetRequiredService<IDataConnection<SqlConnection>>());
            serviceCollection.TryAddSingleton<IRetryPolicyRegistry, SqlPolicyRegistry>();

            return new SqlConfigurationBuilder(serviceCollection);
        }

        public static ISqlConfigurationBuilder WithTypeMappings(this ISqlConfigurationBuilder builder, Assembly[] vpayAssemblies)
        {
            SqlMapper.AddTypeMap(typeof(DateTime), System.Data.DbType.DateTime2);
            SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<BatchStatus>());
            SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<PaymentStatus>());
            SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<Outbound835FileStatus>());
            SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<ApMessageTrackingStatus>());
            return builder;
        }

        /// <summary>
        /// Implementation of the configuration builder.
        /// </summary>
        private sealed class SqlConfigurationBuilder : ISqlConfigurationBuilder
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SqlConfigurationBuilder"/> class.
            /// </summary>
            /// <param name="services">The service collection into which services should be registered.</param>
            /// <exception cref="ArgumentNullException">When the <paramref name="services"/> are null.</exception>
            public SqlConfigurationBuilder(IServiceCollection services)
            {
                Services = services ?? throw new ArgumentNullException(nameof(services));
            }

            /// <summary>
            /// Gets the <see cref="IServiceCollection" /> into which any configuration options should be registered.
            /// </summary>
            public IServiceCollection Services { get; }
        }
    }
}
