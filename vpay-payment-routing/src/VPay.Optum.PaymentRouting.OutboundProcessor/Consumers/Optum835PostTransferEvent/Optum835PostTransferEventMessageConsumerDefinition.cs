using System.Diagnostics.CodeAnalysis;
using GreenPipes.Configurators;
using Microsoft.Data.SqlClient;
using VPay.FileTransfer.Messaging;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.Optum835PostTransferEvent
{
    [ExcludeFromCodeCoverage]
    public class Optum835PostTransferEventMessageConsumerDefinition : PostTransferEventMessageConsumerDefinition<Optum835PostTransferEventMessageConsumer>
    {
        public Optum835PostTransferEventMessageConsumerDefinition() : base("OPTUM_OUT835", "OPTUM_OUT835")
        {
        }

        protected override void ConfigureRetries(IRetryConfigurator retryConfigurator)
        {
            base.ConfigureRetries(retryConfigurator);
            retryConfigurator.Handle<SqlException>();
        }
    }
}
