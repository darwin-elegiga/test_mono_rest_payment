using System.Diagnostics.CodeAnalysis;
using GreenPipes.Configurators;
using Microsoft.Data.SqlClient;
using VPay.FileTransfer.Messaging;

namespace VPay.DocSys.Parsing;

[ExcludeFromCodeCoverage]
public class PostPaymentEngineConfirmationEventMessageConsumerDefinition : PostTransferEventMessageConsumerDefinition<PostPaymentEngineConfirmationEventMessageConsumer>
{
    public PostPaymentEngineConfirmationEventMessageConsumerDefinition() : base("OPTUM_PE", "OPTUM_PE")
    {
    }

    protected override void ConfigureRetries(IRetryConfigurator retryConfigurator)
    {
        base.ConfigureRetries(retryConfigurator);
        retryConfigurator.Handle<SqlException>();
    }
}
