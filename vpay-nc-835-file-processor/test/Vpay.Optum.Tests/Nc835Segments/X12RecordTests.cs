using Xunit;
using VPay.Optum.Nc835Segments;

namespace VPay.Optum.Tests.Nc835Segments
{
    public class X12RecordTests
    {
        private static readonly string[] STArray = { "ST", "01", "02" };
        private static readonly string[] BPRArray = { "BPR", "C", "1000", "C", "test", "test1", "test2", "test3", "test4", "test5", "test6", "test7", "test8", "test9", "test10", "test11", "test12", "test13", "test14" };
        private static readonly string[] TRNArray = { "TRN", "1", "12345", "test", "test1" };
        private static readonly string[] DTMArray = { "DTM", "20230505", "test" };
        private static readonly string[] SEArray = { "SE", "10", "0001" };

        [Fact]
        public void X12Record_ShouldInitializePropertiesToDefaultValues()
        {
            // Arrange & Act
            var record = new X12Record();

            // Assert
            Assert.Null(record.ST);
            Assert.Null(record.BPR);
            Assert.Null(record.TRN);
            Assert.Null(record.DTM);
            Assert.NotNull(record.Loop1000A);
            Assert.NotNull(record.Loop1000B);
            Assert.Null(record.SE);
        }

        [Fact]
        public void X12Record_ShouldAllowSettingAndGettingProperties()
        {
            // Arrange
            var record = new X12Record();
            var st = new ST(STArray);
            var bpr = new BPR(BPRArray);
            var trn = new TRN(TRNArray);
            var dtm = new DTM(DTMArray);
            var loop1000A = new Loop1000A();
            var loop1000B = new Loop1000B();
            var se = new SE(SEArray);

            // Act
            record.ST = st;
            record.BPR = bpr;
            record.TRN = trn;
            record.DTM = dtm;
            record.Loop1000A = loop1000A;
            record.Loop1000B = loop1000B;
            record.SE = se;

            // Assert
            Assert.Equal(st, record.ST);
            Assert.Equal(bpr, record.BPR);
            Assert.Equal(trn, record.TRN);
            Assert.Equal(dtm, record.DTM);
            Assert.Equal(loop1000A, record.Loop1000A);
            Assert.Equal(loop1000B, record.Loop1000B);
            Assert.Equal(se, record.SE);
        }
    }
}
