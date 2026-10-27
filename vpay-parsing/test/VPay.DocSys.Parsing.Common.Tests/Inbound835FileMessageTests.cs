using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace VPay.DocSys.Parsing.Common.Tests
{
    public class inbound_835_file_message_should
    {
        [Fact]
        public void init_with_null()
        {
            (new Inbound835FileMessage()).FilePath.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("/my/test/path/202010241532.txt")]
        [InlineData("/my/test/path/202010252455.txt")]
        [InlineData("/my/test/path/202010262349.835.txt")]
        [InlineData("/my/test/path/202010271722.835.txt")]
        [InlineData(null)]
        public void init_with_file_path(string path)
        {
            (new Inbound835FileMessage(path)).FilePath.Should().Be(path);
        }

        [Theory]
        [InlineData("")]
        [InlineData("/my/test/path/202010241532.txt")]
        [InlineData("/my/test/path/202010252455.txt")]
        [InlineData("/my/test/path/202010262349.835.txt")]
        [InlineData("/my/test/path/202010271722.835.txt")]
        [InlineData(null)]
        public void have_assigned_file_path(string path)
        {
            var msg = new Inbound835FileMessage();
            msg.FilePath = path;
            msg.FilePath.Should().Be(path);
        }
    }
}
