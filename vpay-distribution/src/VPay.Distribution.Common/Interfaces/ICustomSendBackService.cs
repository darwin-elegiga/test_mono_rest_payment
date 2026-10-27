using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
	/// <summary>
	/// Provides services for distributing Custom Send Back checks.
	/// </summary>
	public interface ICustomSendBackService
	{
		/// <summary>
		/// Distributes custom send back checks for the transactions in the specified <paramref name="message"/>.
		/// </summary>
		/// <param name="message">The message that contains the transactions to distribute.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		Task DistributeCustomSendBacks(DocDistMsg message, CancellationToken cancellationToken);
	}
}
