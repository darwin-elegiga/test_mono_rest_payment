using System.Diagnostics.CodeAnalysis;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Tests.Helpers
{
    /// <summary>
    /// Mocks IMessagePublisherFactory by always returning the instance given in constructor.
    /// </summary>
    /// <seealso cref="IMessagePublisherFactory" />
    [ExcludeFromCodeCoverage]
    public class MockPublisherFactory : IMessagePublisherFactory
    {
        private readonly IMessagePublisher _theMessagePublisher;

        public MockPublisherFactory(IMessagePublisher theMessagePublisher) => _theMessagePublisher = theMessagePublisher;

        public IMessagePublisher CreatePublisher(QueueConfigurationSettings settings)
        {
            return _theMessagePublisher;
        }

        public IMessagePublisher CreatePublisher(QueueConfigurationSettings settings, Microsoft.Extensions.Logging.ILogger logger)
        {
            return _theMessagePublisher;
        }
    }
}
