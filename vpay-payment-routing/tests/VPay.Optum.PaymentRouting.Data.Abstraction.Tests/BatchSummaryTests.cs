using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Abstraction.Tests
{
    public class BatchSummaryTests
    {
        [Fact]
        public void IsBatchReceived_ShouldReturnFalseOnInitialization()
        {
            var sut = new BatchSummary();
            Assert.False(sut.IsBatchValid());
            Assert.False(sut.IsBatchReceived());
        }

        [Fact]
        public void IsBatchReceived_ShouldReturnFalseOnIncompleteBatch()
        {
            var sut = new BatchSummary();
            sut.InboundBatchId = 1;
            sut.OptumBatchId = "NOT EMPTY";
            sut.HeaderCount = null;
            sut.TotalPaymentCount = 2;
            sut.ActualPaymentCount = 1;

            sut.TotalPaymentAmount = 20.0m;
            sut.ActualPaymentAmount = 10.0m;

            Assert.True(sut.IsBatchValid());
            Assert.False(sut.IsBatchReceived());
        }

        [Fact]
        public void IsBatchReceived_ShouldReturnTrueOnCompleteBatch()
        {
            var sut = new BatchSummary();
            sut.InboundBatchId = 1;
            sut.OptumBatchId = "NOT EMPTY";
            sut.HeaderCount = 1;
            sut.TotalPaymentCount = 2;
            sut.ActualPaymentCount = 2;
            sut.ActualPaymentCount = 2;
            sut.ActualPaymentCount = 2;

            sut.TotalPaymentAmount = 20.0m;
            sut.ActualPaymentAmount = 20.0m;

            Assert.True(sut.IsBatchValid());
            Assert.True(sut.IsBatchReceived());
        }



        [Fact]
        public void IsBatchReceived_With_Pdfs_ShouldReturnTrueOnCompleteBatch()
        {
            var sut = new BatchSummary();
            sut.InboundBatchId = 1;
            sut.OptumBatchId = "NOT EMPTY";
            sut.HeaderCount = 1;
            sut.TotalPaymentCount = 2;
            sut.ActualPaymentCount = 2;
            sut.ActualPaymentCount = 2;
            sut.ActualPaymentCount = 2;
            sut.RequiredPdfs = 2;
            sut.FoundPdfs = 2;

            sut.TotalPaymentAmount = 20.0m;
            sut.ActualPaymentAmount = 20.0m;

            Assert.True(sut.IsBatchValid());
            Assert.True(sut.IsBatchReceived());
        }

        [Fact]
        public void IsBatchReceived_With_Pdfs_ShouldReturnFalse_when_pdfs_missing()
        {
            var sut = new BatchSummary();
            sut.InboundBatchId = 1;
            sut.OptumBatchId = "NOT EMPTY";
            sut.HeaderCount = 1;
            sut.TotalPaymentCount = 2;
            sut.ActualPaymentCount = 2;
            sut.ActualPaymentCount = 2;
            sut.ActualPaymentCount = 2;
            sut.RequiredPdfs = 2;
            sut.FoundPdfs = 1;

            sut.TotalPaymentAmount = 20.0m;
            sut.ActualPaymentAmount = 20.0m;

            Assert.True(sut.IsBatchValid());
            Assert.False(sut.IsBatchReceived());
        }

        

    }
}
