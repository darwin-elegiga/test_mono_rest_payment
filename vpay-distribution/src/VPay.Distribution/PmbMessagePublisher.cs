using System;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Serialization;
using VPay.Distribution.Common.Interfaces;

namespace VPay.Distribution
{
    public class PmbMessagePublisher : IPmbMessagePublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public PmbMessagePublisher(IPublishEndpoint publishEndpoint) =>
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));

        public async Task SendAsync<TPmbMessage>(TPmbMessage message, CancellationToken cancellationToken) where TPmbMessage : IPmbMessage
        {
            await _publishEndpoint.Publish<IPmbMessage>(message, x =>
            {
                x.Serializer = new StringMessageSerializer(new ContentType("text/plain"),
                    message.ToString(), Encoding.UTF8);
                x.SetRoutingKey("PMBROUTE");
            }, cancellationToken);
        }
    }
}

