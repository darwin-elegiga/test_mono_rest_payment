using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation.Data
{
    public interface IBundledFaxRepository : IDataRepository
    {
        /// <summary>
        /// Return <see cref="FailedBundledFax"/>es for the given <paramref name="startDate"/>;s <see cref="DateTime.Date"/>
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<FailedBundledFax>> GetFailedBundledFaxesAsync(DateTime startDate, CancellationToken cancellationToken = default);
    }
}
