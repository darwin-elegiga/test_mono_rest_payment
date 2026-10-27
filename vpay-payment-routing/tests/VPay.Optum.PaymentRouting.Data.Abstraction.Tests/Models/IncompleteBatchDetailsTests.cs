using System;
using FluentAssertions;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using Xunit;


namespace VPay.Optum.PaymentRouting.Data.Abstraction.Tests.Models
{
    public class IncompleteBatchDetailsTests
    {
        [Fact]
        public void IncompleteBatchDetails_get_set_should_return_expected_values()
        {
            //arrange
            var expected = new IncompleteBatchDetails();
     
            expected.InboundBatchId=1;
            expected.OptumBatchId="ABC";
            expected.Payer835Id="ABC";
            expected.TotalPaymentCount=1;
            expected.ReceivedPaymentCount=1;
            expected.TotalPaymentAmount=1;
            expected.ReceivedPaymentAmount=1;
            expected.HeaderCount=1;
            expected.RequiredRemittanceAdviceFiles=1;
            expected.ReceivedRemittanceAdviceFiles=1;
            expected.Status="ABC";
            expected.IncompleteBatchReason="ABC";
            expected.CreatedDateTime=new DateTime(1,1,1);
            expected.LastModifiedDateTime= new DateTime(1, 1, 1);
            //act
            var actual = new IncompleteBatchDetails
            {
                InboundBatchId = 1,
                OptumBatchId = "ABC",
                Payer835Id = "ABC",
                TotalPaymentCount = 1,
                ReceivedPaymentCount = 1,
                TotalPaymentAmount = 1,
                ReceivedPaymentAmount = 1,
                HeaderCount = 1,
                RequiredRemittanceAdviceFiles = 1,
                ReceivedRemittanceAdviceFiles = 1,
                Status = "ABC",
                IncompleteBatchReason = "ABC",
                CreatedDateTime = new DateTime(1, 1, 1),
                LastModifiedDateTime = new DateTime(1, 1, 1)
            };

            //assert
            actual.Should().BeEquivalentTo(expected);

        }
    }
}
