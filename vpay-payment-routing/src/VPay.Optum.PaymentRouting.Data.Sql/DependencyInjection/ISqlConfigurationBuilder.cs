using Microsoft.Extensions.DependencyInjection;

namespace VPay.Optum.PaymentRouting.Data.Sql.DependencyInjection
{
    /// <summary>
    /// A builder used to configure SQL.
    /// </summary>
    public interface ISqlConfigurationBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> into which any configuration options should be registered.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
