using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation.Data
{
    public class BundledFaxRepository : DataRepository, IBundledFaxRepository
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BundledFaxRepository"/> class.
        /// </summary>
        /// <param name="connection">The connection to use.</param>
        /// <param name="logger">The logger to use.</param>
        public BundledFaxRepository(IDocSysConnection connection, ILogger<BundledFaxRepository> logger)
            : base(connection)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        new public IDocSysConnection Connection
        {
            get => (IDocSysConnection)base.Connection;
        }

        /// <inheritdoc/>
        public async Task<List<FailedBundledFax>> GetFailedBundledFaxesAsync(DateTime startDateTime, CancellationToken cancellationToken = default)
        {
            var StartDate = startDateTime.Date.ToString();

            var faxListJson = await this.Connection.SelectAsync<string>(
                commandType: CommandType.StoredProcedure,
                commandText: "[dbo].[usp_FailedBundledFaxes_GetJSON]",
                commandParameters: new { StartDate },
                cancellationToken: cancellationToken);


            if(!faxListJson.Any())
            {
                _logger.LogInformation("no bundled faxes were found in GetFailedBundledFaxesAsync for {StartDateTime}", startDateTime);
            }

            return faxListJson
                .Select(f => JsonConvert.DeserializeObject<FailedBundledFax>(f))
                .ToList();
        }
    }
}
