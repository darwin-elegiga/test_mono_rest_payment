using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.FileTransfer;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Helpers;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;


namespace VPay.Optum.PaymentRouting.OutboundProcessor.Application.Tests.Helpers
{
    public class OutboundFinSegPayerAliasHelperFacts
    {


        [Theory]
        [InlineData("MUM25", "MUMV5")]
        [InlineData("DUM25", "DUMV5")]
        [InlineData("UMHS5", "UMHS5")]
        [InlineData("nonsence", "nonsence")]
        [InlineData("", "")]
        [InlineData("dum25", "dum25")]
        [InlineData("mum25", "mum25")]
        public void OutboundFinSegPayerAliasConverter_should_return_correct_results(string payerAlias, string expected)
        {
            var result = payerAlias.OutboundFinSegPayerAliasConverter();
            result.Should().Be(expected);

        }


    }
}
