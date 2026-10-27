using Xunit;

namespace VPay.DocSys.Parsing.Common.Tests
{
    public class SmbPathExtensionsTests
    {
        [Theory]
        [InlineData("/my/file/path", "/my/file/path")]
        [InlineData("con", "con1")]
        [InlineData("con.ini", "con1.ini")]
        [InlineData("/SRVFS/bogner/PRINT/RC/CON", "/SRVFS/bogner/PRINT/RC/CON1")]
        [InlineData("/SRVFS/bogner/logs/con", "/SRVFS/bogner/logs/con1")]
        [InlineData("/SRVFS/bogner/faxes/queued/RC/CON", "/SRVFS/bogner/faxes/queued/RC/CON1")]
        [InlineData("/SRVFS/bogner/transfer/RC/CON", "/SRVFS/bogner/transfer/RC/CON1")]
        [InlineData("/SRVFS/bogner/uploadFiles/con", "/SRVFS/bogner/uploadFiles/con1")]
        [InlineData("/home/bogner/con", "/home/bogner/con1")]
        [InlineData("/home/bogner/con/file", "/home/bogner/con1/file")]
        [InlineData("/home/bogner/con/con", "/home/bogner/con1/con1")]
        [InlineData("/home/bogner/con/con/con", "/home/bogner/con1/con1/con1")]
        [InlineData("/home/bogner/con/con.ini", "/home/bogner/con1/con1.ini")]
        [InlineData("/home/bogner/con_thing/con.ini", "/home/bogner/con_thing/con1.ini")]
        [InlineData("/home/bogner/con-thing/con.ini", "/home/bogner/con-thing/con1.ini")]
        [InlineData("/home/bogner/con-thing/", "/home/bogner/con-thing/")]
        public void EnsureIsSmbSafePath_ShouldReplaceWithExpected(string input, string expectedOutput)
        {
            // arrange

            // act
            var output = input.EnsureIsSmbSafePath();

            // assert
            Assert.Equal(expectedOutput, output);
        }

        [Theory]
        [InlineData("path", "path")] // no append
        [InlineData("con", "con1")] // append
        [InlineData("cOn", "cOn1")] // append
        [InlineData("CON", "CON1")] // append
        [InlineData("PRN", "PRN1")] // append
        [InlineData("AUX", "AUX1")] // append
        [InlineData("NUL", "NUL1")] // append
        [InlineData("COM", "COM")] // no append
        [InlineData("COM1", "COM11")] // append
        [InlineData("COM2", "COM21")] // append
        [InlineData("COM3", "COM31")] // append
        [InlineData("COM4", "COM41")] // append
        [InlineData("COM5", "COM51")] // append
        [InlineData("COM6", "COM61")] // append
        [InlineData("COM7", "COM71")] // append
        [InlineData("COM8", "COM81")] // append
        [InlineData("COM9", "COM91")] // append
        [InlineData("COM0", "COM01")] // append
        [InlineData("COM11", "COM11")] // no append
        [InlineData("LPT", "LPT")] // no append
        [InlineData("LPT1", "LPT11")] // no append
        [InlineData("LPT2", "LPT21")] // no append
        [InlineData("LPT3", "LPT31")] // no append
        [InlineData("LPT4", "LPT41")] // no append
        [InlineData("LPT5", "LPT51")] // no append
        [InlineData("LPT6", "LPT61")] // no append
        [InlineData("LPT7", "LPT71")] // no append
        [InlineData("LPT8", "LPT81")] // no append
        [InlineData("LPT9", "LPT91")] // no append
        [InlineData("LPT0", "LPT01")] // no append
        [InlineData("LPT11", "LPT11")] // no append
        public void EnsureIsSmbSafeName_ShouldReplaceWithExpected(string input, string expectedOutput)
        {
            // arrange

            // act
            var output = input.EnsureIsSmbSafeName();

            // assert
            Assert.Equal(expectedOutput, output);
        }
    }
}
