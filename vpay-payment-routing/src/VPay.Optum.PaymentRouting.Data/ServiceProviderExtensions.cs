using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data
{
    /// <summary>
    /// Extension methods for setting up Sql service in an <see cref="IServiceCollection" />.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Registers a <see cref="IDataConnection{T}"/> with the service collection
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static IServiceCollection AddSqlConnection(this IServiceCollection serviceCollection,
            string connectionString)
        {
            serviceCollection.AddTypeMappings();
            serviceCollection.TryAddScoped<IDataConnection<SqlConnection>>(x =>
            {
                return ActivatorUtilities.CreateInstance<SqlDataConnection>(x, connectionString);
            });
            serviceCollection.TryAddSingleton<IRetryPolicyRegistry, SqlPolicyRegistry>();
            serviceCollection.TryAddTransient<IDbContext, DbContext>();
            serviceCollection.AddRepositoryClasses();
            return serviceCollection;
        }

        /// <summary>
        /// Registers a <see cref="IDataConnection{T}"/> with the service collection
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        public static IServiceCollection AddSqlConnection(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTypeMappings();

            serviceCollection.TryAddScoped<IDataConnection<SqlConnection>, SqlDataConnection>();
            serviceCollection.TryAddSingleton<IRetryPolicyRegistry, SqlPolicyRegistry>();
            serviceCollection.TryAddTransient<IDbContext, DbContext>();
            serviceCollection.AddRepositoryClasses();
            return serviceCollection;
        }

        /// <summary>
        /// Registers a <see cref="IDataConnection{T}"/> and <see cref="ConnectionConfig" />
        /// with the service collection
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection AddDb2OdbcConnection(this IServiceCollection serviceCollection, Action<ConnectionConfig> configure)
        {
            serviceCollection.AddSqlConnection();
            serviceCollection.Configure(configure);
            return serviceCollection;
        }

        private static IServiceCollection AddRepositoryClasses(this IServiceCollection serviceCollection)
        {
            var assembly = Assembly.GetAssembly(typeof(ServiceProviderExtensions));

            foreach (var classType in assembly.GetExportedTypes()
                .Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericType && !x.IsNested && typeof(IBaseRepository).IsAssignableFrom(x)))
            {
                var interfaces = classType.GetTypeInfo().ImplementedInterfaces;
                foreach (var infc in interfaces.Where(i => i != typeof(IDisposable) && i.IsPublic && !i.IsNested))
                {
                    serviceCollection.Add(new ServiceDescriptor(infc, classType, ServiceLifetime.Scoped));
                }
            }

            return serviceCollection;
        }

        private static IServiceCollection AddTypeMappings(this IServiceCollection serviceCollection)
        {
            SqlMapper.AddTypeMap(typeof(DateTime), System.Data.DbType.DateTime2);
            SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<BatchStatus>());
            SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<PaymentStatus>());
            SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<ApMessageTrackingStatus>());
            SqlMapper.AddTypeHandler(new SmartEnumTypeHandler<Outbound835FileStatus>());

            return serviceCollection;
        }
    }
}
