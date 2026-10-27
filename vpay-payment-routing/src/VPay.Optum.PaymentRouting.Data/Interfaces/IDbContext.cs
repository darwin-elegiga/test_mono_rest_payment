using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace VPay.Optum.PaymentRouting.Data.Abstractions
{
    /* Boiler plate code copied from VPay.ReconReports master/v0.4.0/Commit 30a20b19 */
    public interface IDbContext
    {


        /// <summary>
        /// This will return the repository that is associated <typeparamref name="T"/> that you can run the methods on. 
        /// </summary>
        /// <typeparam name="T">Must be of type <see cref="IBaseRepository"/></typeparam>
        /// <returns></returns>
        T GetRepository<T>() where T : IBaseRepository;

        /// <summary>
        /// This will test if a connection can be opened to the database
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>This returns true if a connection to the database can be opened</returns>
        Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);

        Task<IDataConnectionTransaction> BeginTransaction(IsolationLevel isolationLevel);

        Task<IDataConnectionTransaction> BeginTransaction();
    }
}
