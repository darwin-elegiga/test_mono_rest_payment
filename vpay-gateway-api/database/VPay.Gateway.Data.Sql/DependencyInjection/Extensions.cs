using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using VPay.Gateway.Data.Connection;
using VPay.Gateway.Data.Mapping;

namespace VPay.Gateway.Data.Sql.DependencyInjection;

/// <summary>
/// Extensions related to dependency injection for this assembly.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Add the SQL services to the service collection.
    /// </summary>
    /// <param name="serviceCollection">The collection to add the services to</param>
    /// <returns>The configuration builder for additional SQL options.</returns>
    public static ISqlConfigurationBuilder AddSql(this IServiceCollection serviceCollection)
    {
        if (serviceCollection == null)
        {
            throw new ArgumentNullException(nameof(serviceCollection));
        }

        serviceCollection.TryAddScoped<IDataConnection<SqlConnection>, SqlDataConnection>();
        serviceCollection.TryAddScoped<IDbUnitOfWork>(x => x.GetRequiredService<IDataConnection<SqlConnection>>());
        serviceCollection.TryAddSingleton<IRetryPolicyRegistry, SqlPolicyRegistry>();

        return new SqlConfigurationBuilder(serviceCollection);
    }

    /// <summary>
    /// Allow configuration of the SQL connection.
    /// </summary>
    /// <param name="builder">The builder to use when configuring the connection.</param>
    /// <param name="configureConnection">An action that takes the connection to configure.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ISqlConfigurationBuilder WithConnectionConfiguration(this ISqlConfigurationBuilder builder, Action<ConnectionConfig> configureConnection)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services
            .Configure(configureConnection)
            .AddTransient(cfg => cfg.GetService<IOptions<ConnectionConfig>>().Value);

        return builder;
    }

    public static ISqlConfigurationBuilder WithTypeMappings(this ISqlConfigurationBuilder builder, Assembly[] vpayAssemblies)
    {
        SqlMapper.AddTypeMap(typeof(DateTime), System.Data.DbType.DateTime2);

        // find any types that have the ColumnAttribute
        var mappedTypes = from asm in vpayAssemblies
                          from type in asm.GetTypes()
                          where type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Any(x => x.GetCustomAttribute<ColumnAttribute>(false) != null)
                          select type;

        // register those types with a Dapper type mapper
        var mapper = typeof(ColumnAttributeTypeMapper<>);
        foreach (var type in mappedTypes)
        {
            var genericType = mapper.MakeGenericType(type);
            SqlMapper.SetTypeMap(type, Activator.CreateInstance(genericType) as SqlMapper.ITypeMap);
        }

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
