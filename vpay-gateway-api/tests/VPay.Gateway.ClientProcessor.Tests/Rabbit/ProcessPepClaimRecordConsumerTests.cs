using System.Collections.ObjectModel;
using MassTransit;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using VPay.Gateway.ClientProcessor.Rabbit;
using VPay.Gateway.Common.Commands;
using VPay.Gateway.Common.PepClaims;
using VPay.Gateway.Common.PepClaims.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Connection;
using VPay.Gateway.Data.Sql.PepClaims;

namespace VPay.Gateway.ClientProcessor.Tests.Rabbit;

/// <summary>
/// Tests for <see cref="ProcessPepClaimRecordConsumer"/>
/// </summary>
public class ProcessPepClaimRecordConsumerTests
{
    private readonly IPepClaimsApi _pepClaimsApi = Substitute.For<IPepClaimsApi>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly IDataConnection<SqlConnection> _connection = Substitute.For<IDataConnection<SqlConnection>>();

    private readonly ProcessPepClaimRecordConsumer _consumer;

    public ProcessPepClaimRecordConsumerTests()
    {
        // add the sample claims from the resources file.
        // NOTE: These are samples from production (modified for PII)
        var claims = new ReadOnlyDictionary<long, PepClaimInfoDto>(
            new Dictionary<long, PepClaimInfoDto>(2)
            {
                [256064] = JsonConvert.DeserializeObject<PepClaimInfoDto>(Resources.PepClaim_256064),
                [220823] = JsonConvert.DeserializeObject<PepClaimInfoDto>(Resources.PepClaim_220823),
            });

        _pepClaimsApi
            .GetClaimById(Arg.Any<long>(), CancellationToken.None)
            .Returns(x => claims.TryGetValue(x.Arg<long>(), out var claim) ? claim : null);

        _consumer = new ProcessPepClaimRecordConsumer(
            _pepClaimsApi,
            _mediator,
            _publishEndpoint,
            _connection,
            new NullLogger<ProcessPepClaimRecordConsumer>());
    }

    [Fact]
    public async Task WithEnrollmentChanges_ForClaim256064_ShouldProcessEnrollments()
    {
        _mediator
            .Send(Arg.Any<ExternalClientConfigurationQuery>(), CancellationToken.None)
            .Returns(new ExternalClientConfigurationDto());

        _mediator
            .Send(Arg.Any<InsertPayeeCommand>(), CancellationToken.None)
            .ReturnsForAnyArgs(x => new Payee { ClientCode = x.Arg<InsertPayeeCommand>().ClientCode });

        _mediator
            .Send(Arg.Any<PayeeStatusQuery>(), CancellationToken.None)
            .Returns(
                // ClaimStatus = ENRLRCVD, PartyStatus = NULL
                null,

                // ClaimStatus = ENRLINPROG, PartyStatus = NULL
                null,

                // ClaimStatus = ENRLINPROG, PartyStatus = ENRLIDVERREJ
                null,

                // ClaimStatus = ENRLINPROG, PartyStatus = ENRLIDVERREJ
                // subsequent calls should return a value due to a unique on claim/party status
                new PayeeStatus(),
                new PayeeStatus(),

                // ClaimStatus = ENRLFAIL, PartyStatus = NULL
                null);

        _mediator
            .Send(Arg.Any<PayeeQuery>(), CancellationToken.None)
            .Returns((Payee)null);

        var context = Substitute.For<ConsumeContext<ProcessPepClaimRecord>>();
        context.Message.ReturnsForAnyArgs(new ProcessPepClaimRecord { ClaimId = 256064 });

        await _consumer.Consume(context);

        using (new AssertionScope())
        {
            // we should have calls for the different status combinations.
            foreach (var statusCombo in new (string ClaimStatus, string PartyStatus)[]
                     {
                         ("ENRLRCVD", null),
                         ("ENRLINPROG", null),
                         ("ENRLINPROG", "ENRLIDVERREJ"),
                         ("ENRLFAIL", null)
                     })
            {
                await _mediator
                    .Received(1)
                    .Send(
                        Arg.Is<InsertPayeeStatusCommand>(x =>
                            x.Status == statusCombo.ClaimStatus
                            && x.PartyStatus == statusCombo.PartyStatus),
                        CancellationToken.None);
            }

            // the payee associated with the claim should only be inserted once
            await _mediator
                .Received(1)
                .Send(Arg.Any<InsertPayeeCommand>(), CancellationToken.None);

            // we should have an event published to process the enrollment records
            await _publishEndpoint
                .Received(1)
                .Publish(Arg.Any<ProcessEnrollmentStatusRecords>(), CancellationToken.None);
        }
    }

    [Fact]
    public async Task WithEnrollmentChanges_ForClaim220823_ShouldProcessEnrollments()
    {
        _mediator
            .Send(Arg.Any<ExternalClientConfigurationQuery>(), CancellationToken.None)
            .Returns(new ExternalClientConfigurationDto());

        _mediator
            .Send(Arg.Any<InsertPayeeCommand>(), CancellationToken.None)
            .ReturnsForAnyArgs(x => new Payee { ClientCode = x.Arg<InsertPayeeCommand>().ClientCode });

        _mediator
            .Send(Arg.Any<PayeeStatusQuery>(), CancellationToken.None)
            .Returns(
                // ClaimStatus = ENRLRCVD, PartyStatus = NULL
                null,

                // ClaimStatus = ENRLINPROG, PartyStatus = NULL
                null,

                // ClaimStatus = ENRLINPROG, PartyStatus = ENRLEMFAIL
                null,

                // ClaimStatus = ENRLINPROG, PartyStatus = ENRLEMFAIL
                // subsequent calls should return a value due to a unique on claim/party status
                // NOTE: This claim had a whole bunch of ENRLEMFAIL messages
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),
                new PayeeStatus(),

                // ClaimStatus = ENRLINPROG, PartyStatus = ENRLEXP
                null,

                // ClaimStatus = ENRLEXP, PartyStatus = NULL
                null,

                // ClaimStatus = ENRLEXP, PartyStatus = ENRLEMFAIL
                null,

                // ClaimStatus = ENRLEXP, PartyStatus = ENRLEMFAIL
                // subsequent calls should return a value due to a unique on claim/party status
                new PayeeStatus());

        _mediator
            .Send(Arg.Any<PayeeQuery>(), CancellationToken.None)
            .Returns((Payee)null);

        var context = Substitute.For<ConsumeContext<ProcessPepClaimRecord>>();
        context.Message.ReturnsForAnyArgs(new ProcessPepClaimRecord { ClaimId = 220823 });

        await _consumer.Consume(context);

        using (new AssertionScope())
        {
            // we should have calls for the different status combinations.
            foreach (var statusCombo in new (string ClaimStatus, string PartyStatus)[]
                     {
                         ("ENRLRCVD", null),
                         ("ENRLINPROG", null),
                         ("ENRLINPROG", "ENRLEMFAIL"),
                         ("ENRLINPROG", "ENRLEXP"),
                         ("ENRLEXP", null),
                         ("ENRLEXP", "ENRLEMFAIL")
                     })
            {
                await _mediator
                    .Received(1)
                    .Send(
                        Arg.Is<InsertPayeeStatusCommand>(x =>
                            x.Status == statusCombo.ClaimStatus
                            && x.PartyStatus == statusCombo.PartyStatus),
                        CancellationToken.None);
            }

            // the payee associated with the claim should only be inserted once
            await _mediator
                .Received(1)
                .Send(Arg.Any<InsertPayeeCommand>(), CancellationToken.None);

            // we should have an event published to process the enrollment records
            await _publishEndpoint
                .Received(1)
                .Publish(Arg.Any<ProcessEnrollmentStatusRecords>(), CancellationToken.None);
        }
    }
}
