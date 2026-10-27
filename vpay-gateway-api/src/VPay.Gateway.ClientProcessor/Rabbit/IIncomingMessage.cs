
/* NOTE: We'll use the namespace and message definition from PEP Kubera
 * so the messages will be handled correctly (automatically) by MassTransit.
 */
namespace Vpay.Pep.Kubera.Core.Messages;

public interface IIncomingMessage
{
    IEnumerable<string> PepClaimIds { get; set; }

    string startClaimId { get; set; }
    string endClaimId { get; set; }
}
