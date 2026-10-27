using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace Vpay.Optum.Tests.Nc835Segments
{
    public class ISATests
    {
        [Fact]
        public void Constructor_ValidISARecord_InitializesPropertiesCorrectly()
        {
            // Arrange
            string validISARecord = "ISA*00*          *00*          *ZZ*ABCDEFGHIJKLMNO*ZZ*123456789012345*230501*1234*^*00501*000000905*0*P*:~";

            // Act
            var isa = new ISA(validISARecord);

            // Assert
            Assert.Equal("ISA", isa.ISA00);
            Assert.Equal("00", isa.ISA01);
            Assert.Equal("          ", isa.ISA02);
            Assert.Equal("00", isa.ISA03);
            Assert.Equal("          ", isa.ISA04);
            Assert.Equal("ZZ", isa.ISA05);
            Assert.Equal("ABCDEFGHIJKLMNO", isa.ISA06);
            Assert.Equal("ZZ", isa.ISA07);
            Assert.Equal("123456789012345", isa.ISA08);
            Assert.Equal("230501", isa.ISA09);
            Assert.Equal("1234", isa.ISA10);
            Assert.Equal("^", isa.ISA11);
            Assert.Equal("00501", isa.ISA12);
            Assert.Equal("000000905", isa.ISA13);
            Assert.Equal("0", isa.ISA14);
            Assert.Equal("P", isa.ISA15);
            Assert.Equal('~', isa.EndSegmentChar);
            Assert.Equal(':', isa.ComponentSeparator);
            Assert.Equal('*', isa.SegmentSeparator);
        }

        [Fact]
        public void Constructor_InvalidStart_ThrowsArgumentException()
        {
            // Arrange
            string invalidISARecord = "XXX*00*          *00*          *ZZ*ABCDEFGHIJKLMNO*ZZ*123456789012345*230501*1234*^*00501*000000905*0*P*:~";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ISA(invalidISARecord));
            Assert.Equal("ISA record must start with ISA (Parameter 'isaRecord')", exception.Message);
        }

        [Fact]
        public void Constructor_InvalidLength_ThrowsArgumentException()
        {
            // Arrange
            string invalidISARecord = "ISA*00*          *00*          *ZZ*ABCDEFGHIJKLMNO*ZZ*123456789012345*230501*1234*^*00501*000000905*0*P*:~EXTRA";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ISA(invalidISARecord));
            Assert.Equal("ISA record must be 106 characters long (Parameter 'isaRecord')", exception.Message);
        }

        [Fact]
        public void Constructor_InvalidPartsCount_ThrowsArgumentException()
        {
            // Arrange
            string invalidISARecord = "ISA*00*          *00*          *ZZ*ABCDEFGHIJKLMNO*ZZ*123456789012345*230501*1234*^*00501*000000905*0*P*";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ISA(invalidISARecord));
            Assert.Equal("ISA record must be 106 characters long (Parameter 'isaRecord')", exception.Message);
        }
    }
}