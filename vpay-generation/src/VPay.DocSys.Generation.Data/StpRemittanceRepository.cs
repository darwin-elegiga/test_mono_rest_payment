using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation.Data
{
    public class StpRemittanceRepository : DataRepository, IStpRemittanceRepository
    {
        private readonly ILogger<StpRemittanceRepository> _logger;

        public StpRemittanceRepository(IDocSysConnection connection, ILogger<StpRemittanceRepository> logger)
            : base(connection)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public new IDocSysConnection Connection
        {
            get => (IDocSysConnection)base.Connection;
        }

        public async Task<TpaAddress> GetTpaAddressAsync(string tpa, CancellationToken cancellationToken = default)
        {
            return (TpaAddress)await this.Connection.SelectSingleOrDefaultAsync<TpaAddress>(
                   commandType: CommandType.Text,
                   commandText:
                    @"SELECT TOP 1
                        [TPA],
                        [Address],
                        [City],
                        [State],
                        [Zip]
                      FROM CPSCFG (NOLOCK)
                      WHERE
                        TPA = @TPA",
                   commandParameters: new { tpa },
                   cancellationToken: cancellationToken);
        }
    }
}
