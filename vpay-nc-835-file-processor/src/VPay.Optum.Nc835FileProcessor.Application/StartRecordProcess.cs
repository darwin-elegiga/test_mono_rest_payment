using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Serialization;
using MediatR;
using VPay.Optum.Nc835FileProcessor.Application.Models;

namespace VPay.Optum.Nc835FileProcessor.Application
{
    public static class StartRecordProcess
    {
        public record Command(Nc835RecordNotification Record) : IRequest;

        public class Handler : IRequestHandler<Command>
        {
            private readonly Nc835FileSettings _settings;
            private readonly IPublishEndpoint _publishEndpoint;

            public Handler(Nc835FileSettings settings, IPublishEndpoint publishEndpoint)
            {
                _settings = settings;
                _publishEndpoint = publishEndpoint;
            }

            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                var key = Convert.FromBase64String(_settings.RecordEncryptionKey);

                var keyProvider = new ConstantSecureKeyProvider(key);
                var streamProvider = new AesCryptoStreamProviderV2(keyProvider);

                await _publishEndpoint.Publish(request.Record, x => x.Serializer = new EncryptedMessageSerializerV2(streamProvider), cancellationToken);

                return;
            }
        }
    }
}
