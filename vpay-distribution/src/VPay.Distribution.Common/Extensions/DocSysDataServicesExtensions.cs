using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using VPay.DocSys.Data;

namespace VPay.Distribution.Common.Extensions
{
    public static class DocSysDataServicesExtensions
    {
        ///// <summary>
        ///// Adds the DocSys data services to the dependency injection container.
        ///// Attempts to register repositories from the VPay.DocSys.Data package.
        ///// </summary>
        ///// <param name="services">The service collection to add services to</param>
        ///// <param name="connectionString">The connection string for the DocSys database</param>
        ///// <returns>The service collection for chaining</returns>
        //public static IServiceCollection AddDocSysDataServices(this IServiceCollection services, string connectionString)
        //{
        //    // Register the connection string settings as singleton
        //    services.AddSingleton(new DocSysConnectionSettings { ConnectionString = connectionString });

        //    // Try to register repositories from the package
        //    RegisterRepositories(services, connectionString);

        //    return services;
        //}

        private static void RegisterRepositories(IServiceCollection services, string connectionString)
        {
            try
            {
                var assembly = typeof(DocSysConnectionSettings).Assembly;
                
                // Get all concrete types in the VPay.DocSys.Data namespace
                var concreteTypes = GetConcreteTypesInNamespace(assembly, "VPay.DocSys.Data");
                
                // Try to register IDistributionRepository
                RegisterRepositoryIfExists(services, assembly, concreteTypes, typeof(IDistributionRepository), connectionString);
                
                // Try to register IRedCardRepository
                RegisterRepositoryIfExists(services, assembly, concreteTypes, typeof(IRedCardRepository), connectionString);
                
                // Try to register IEdi835Repository
                RegisterRepositoryIfExists(services, assembly, concreteTypes, typeof(IEdi835Repository), connectionString);
            }
            catch
            {
                // Silently fail - repositories may be optional or registered elsewhere
            }
        }

        private static IEnumerable<Type> GetConcreteTypesInNamespace(Assembly assembly, string namespaceName)
        {
            var types = new List<Type>();
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Namespace == namespaceName && 
                        !type.IsAbstract && 
                        !type.IsInterface &&
                        !type.IsGenericTypeDefinition)
                    {
                        types.Add(type);
                    }
                }
            }
            catch { }
            
            return types;
        }

        private static void RegisterRepositoryIfExists(
            IServiceCollection services,
            Assembly assembly,
            IEnumerable<Type> concreteTypes,
            Type interfaceType,
            string connectionString)
        {
            try
            {
                // Find a concrete type that implements this interface
                foreach (var concreteType in concreteTypes)
                {
                    if (interfaceType.IsAssignableFrom(concreteType))
                    {
                        RegisterRepository(services, interfaceType, concreteType, connectionString);
                        return;
                    }
                }
            }
            catch { }
        }

        private static void RegisterRepository(
            IServiceCollection services,
            Type interfaceType,
            Type implementationType,
            string connectionString)
        {
            try
            {
                var registerMethod = services.GetType()
                    .GetMethod("AddScoped", new[] { typeof(Type), typeof(Type) })
                    ?.MakeGenericMethod(interfaceType, implementationType);

                if (registerMethod != null)
                {
                    // Try simple registration first
                    registerMethod.Invoke(services, null);
                }
                else
                {
                    // Fallback: register with factory
                    var addScopedMethod = typeof(ServiceCollectionServiceExtensions)
                        .GetMethod("AddScoped", 
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            new[] { typeof(IServiceCollection), typeof(Func<IServiceProvider, object>) },
                            null);

                    if (addScopedMethod != null)
                    {
                        addScopedMethod = addScopedMethod.MakeGenericMethod(interfaceType);
                        
                        // Create factory delegate
                        var factory = CreateRepositoryFactory(implementationType, connectionString);
                        addScopedMethod.Invoke(null, new object[] { services, factory });
                    }
                }
            }
            catch
            {
                // Ignore registration failures
            }
        }

        private static Delegate CreateRepositoryFactory(Type implementationType, string connectionString)
        {
            // Create a factory function: IServiceProvider -> object
            var providerParam = System.Linq.Expressions.Expression.Parameter(typeof(IServiceProvider));
            
            // Try to instantiate with connection string
            var constructors = implementationType.GetConstructors();
            var stringCtor = Array.Find(constructors, c =>
            {
                var @params = c.GetParameters();
                return @params.Length == 1 && @params[0].ParameterType == typeof(string);
            });

            if (stringCtor != null)
            {
                var newExpr = System.Linq.Expressions.Expression.New(stringCtor, 
                    System.Linq.Expressions.Expression.Constant(connectionString));
                var castExpr = System.Linq.Expressions.Expression.Convert(newExpr, typeof(object));
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<IServiceProvider, object>>(castExpr, providerParam);
                return lambda.Compile();
            }

            // Fallback to parameterless constructor
            var parameterlessCtor = implementationType.GetConstructor(Type.EmptyTypes);
            if (parameterlessCtor != null)
            {
                var newExpr = System.Linq.Expressions.Expression.New(parameterlessCtor);
                var castExpr = System.Linq.Expressions.Expression.Convert(newExpr, typeof(object));
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<IServiceProvider, object>>(castExpr, providerParam);
                return lambda.Compile();
            }

            throw new InvalidOperationException($"No suitable constructor found for {implementationType.Name}");
        }
    }
}

