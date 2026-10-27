using System;

namespace VPay.Optum.PaymentRouting.Data.Abstractions
{
    /* Boiler plate code copied from VPay.ReconReports master/v0.4.0/Commit 30a20b19 */
    /// <summary>
    ///     <para>
    ///         A transaction against the database.
    ///     </para>
    ///     <para>
    ///         Instances of this class are typically obtained from DataConnection.BeginTransaction and it is not designed
    ///         to be directly constructed in your application code.
    ///     </para>
    /// </summary>
    public interface IDataConnectionTransaction : IDisposable
    {
        /// <summary>
        ///     Gets the transaction identifier.
        /// </summary>
        Guid TransactionId { get; }

        /// <summary>
        ///     Commits all changes made to the database in the current transaction.
        /// </summary>
        void Commit();

        /// <summary>
        ///     Discards all changes made to the database in the current transaction.
        /// </summary>
        void Rollback();
    }
}
