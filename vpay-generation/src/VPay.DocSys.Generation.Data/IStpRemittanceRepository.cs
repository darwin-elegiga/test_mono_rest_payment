using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation.Data;

public interface IStpRemittanceRepository : IDataRepository
{
    Task<TpaAddress> GetTpaAddressAsync(string tpa, CancellationToken cancellationToken = default);
}
