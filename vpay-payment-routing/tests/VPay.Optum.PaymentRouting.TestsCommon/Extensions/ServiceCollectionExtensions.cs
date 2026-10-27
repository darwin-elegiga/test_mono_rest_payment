using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace VPay.Optum.PaymentRouting.TestsCommon.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMock<T>(this IServiceCollection services)
          where T : class =>
            services.AddSingleton(Substitute.For<T>());
    }
}
