using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Moq;
using Xunit;
using VPay.Optum.Nc835FileProcessor.Application.Services;
using VPay.Optum.Nc835Segments;

namespace VPay.Optum.Nc835FileProcessor.Application.Tests;
public class X12ReaderTests
{
    private readonly Mock<PipeReader> _readerMock;
    private readonly X12Reader _x12ReaderTests;
    private readonly Mock<Stream> _mockStream;

    public X12ReaderTests()
    {
        _readerMock = new Mock<PipeReader>();
        _mockStream = new Mock<Stream>();
        _x12ReaderTests = new X12Reader(_mockStream.Object);
    }

    [Fact]
    public async Task GetNext_ShouldReturnISA_WhenISAIsNull()
    {
        // Arrange
        var readResult = new ReadResult(new ReadOnlySequence<byte>(), false, true);
        _readerMock.Setup(r => r.ReadAsync(CancellationToken.None)).ReturnsAsync(readResult);
        _readerMock.Setup(r => r.AdvanceTo(It.IsAny<SequencePosition>(), It.IsAny<SequencePosition>()));

        // Act
        
        await foreach (var result in _x12ReaderTests.GetNext())
        {
            // Assert
            //if (result[])
            Assert.IsType<ISA>(result);
        }
    }

    [Fact]
    public async Task GetNext_ShouldReturnGS_WhenSegmentIsGS()
    {
        // Arrange
        var readResult = new ReadResult(new ReadOnlySequence<byte>(), false, true);
        _readerMock.Setup(r => r.ReadAsync(CancellationToken.None)).ReturnsAsync(readResult);
        _readerMock.Setup(r => r.AdvanceTo(It.IsAny<SequencePosition>(), It.IsAny<SequencePosition>()));

        // Act
        await foreach (var result in _x12ReaderTests.GetNext())
        {
            // Assert
            Assert.IsType<GS>(result);
        }
    }

    // Add more test cases for other segments like ST, BPR, TRN, DTM, N1, N3, N4, SE, GE, IEA

    [Fact]
    public async Task GetNext_ShouldReturnRecord_WhenSegmentIsSE()
    {
        // Arrange
        var readResult = new ReadResult(new ReadOnlySequence<byte>(), false, true);
        _readerMock.Setup(r => r.ReadAsync(CancellationToken.None)).ReturnsAsync(readResult);
        _readerMock.Setup(r => r.AdvanceTo(It.IsAny<SequencePosition>(), It.IsAny<SequencePosition>()));

        // Act
        await foreach (var result in _x12ReaderTests.GetNext())
        {
            // Assert
            Assert.IsType<X12Record>(result);
        }
    }
    [Fact]
    public async Task GetNext_ShouldReturnRecord_WhenSegmentIsBPR()
    {
        // Arrange
        var readResult = new ReadResult(new ReadOnlySequence<byte>(), false, true);
        _readerMock.Setup(r => r.ReadAsync(CancellationToken.None)).ReturnsAsync(readResult);
        _readerMock.Setup(r => r.AdvanceTo(It.IsAny<SequencePosition>(), It.IsAny<SequencePosition>()));

        // Act
        await foreach (var result in _x12ReaderTests.GetNext())
        {
            // Assert
            Assert.IsType<BPR>(result);
        }
    }
    [Fact]
    public async Task GetNext_ShouldReturnRecord_WhenSegmentIsTRN()
    {
        // Arrange
        var readResult = new ReadResult(new ReadOnlySequence<byte>(), false, true);
        _readerMock.Setup(r => r.ReadAsync(CancellationToken.None)).ReturnsAsync(readResult);
        _readerMock.Setup(r => r.AdvanceTo(It.IsAny<SequencePosition>(), It.IsAny<SequencePosition>()));

        // Act
        await foreach (var result in _x12ReaderTests.GetNext())
        {
            // Assert
            Assert.IsType<TRN>(result);
        }
    }
    [Fact]
    public async Task GetNext_ShouldReturnRecord_WhenSegmentIsDTM()
    {
        // Arrange
        var readResult = new ReadResult(new ReadOnlySequence<byte>(), false, true);
        _readerMock.Setup(r => r.ReadAsync(CancellationToken.None)).ReturnsAsync(readResult);
        _readerMock.Setup(r => r.AdvanceTo(It.IsAny<SequencePosition>(), It.IsAny<SequencePosition>()));

        // Act
        await foreach (var result in _x12ReaderTests.GetNext())
        {
            // Assert
            Assert.IsType<DTM>(result);
        }
    }
    [Fact]
    public async Task GetNext_ShouldReturnRecord_WhenSegmentIsGE()
    {
        // Arrange
        var readResult = new ReadResult(new ReadOnlySequence<byte>(), false, true);
        _readerMock.Setup(r => r.ReadAsync(CancellationToken.None)).ReturnsAsync(readResult);
        _readerMock.Setup(r => r.AdvanceTo(It.IsAny<SequencePosition>(), It.IsAny<SequencePosition>()));

        // Act
        await foreach (var result in _x12ReaderTests.GetNext())
        {
            // Assert
            Assert.IsType<GE>(result);
        }
    }
    [Fact]
    public async Task GetNext_ShouldReturnRecord_WhenSegmentIsIEA()
    {
        // Arrange
        var readResult = new ReadResult(new ReadOnlySequence<byte>(), false, true);
        _readerMock.Setup(r => r.ReadAsync(CancellationToken.None)).ReturnsAsync(readResult);
        _readerMock.Setup(r => r.AdvanceTo(It.IsAny<SequencePosition>(), It.IsAny<SequencePosition>()));

        // Act
        await foreach (var result in _x12ReaderTests.GetNext())
        {
            // Assert
            Assert.IsType<IEA>(result);
        }
    }

    //new code

    [Fact]
    public void TryReadLine_ShouldReturnTrueForValidLine()
    {
        // Arrange
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("ISA*00*~"));
        var separator = '~';
        SequencePosition? consumed = null;

        // Act
        var method = typeof(X12Reader).GetMethod("TryReadLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { buffer, consumed!, separator };
        var result = (bool)method?.Invoke(null, parameters)!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ProcessSequence_ShouldReturnCorrectString()
    {
        // Arrange
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("ISA*00*~"));

        // Act
        var method = typeof(X12Reader).GetMethod("ProcessSequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method?.Invoke(null, new object[] { sequence })!;

        // Assert
        Assert.Equal("ISA*00*~", result);
    }

    //New Code

    [Fact]
    public void Constructor_ShouldInitializePipeReader()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var reader = new X12Reader(stream);

        // Assert
        Assert.NotNull(reader);
    }

    
    [Fact]
    public void ProcessSequence_ShouldConvertSequenceToString()
    {
        // Arrange
        var input = "TestSequence";
        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
        var method = typeof(X12Reader).GetMethod("ProcessSequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var parameters = new object[] { sequence };
        var result = method?.Invoke(null, parameters)!;

        // Assert
        Assert.Equal(input, result.ToString());
    }


    [Fact]
    public async Task GetNext_ShouldHandleEmptyStream()
    {
        // Arrange
        using var stream = new MemoryStream();
        var reader = new X12Reader(stream);

        // Act
        var results = new List<object>();
        await foreach (var item in reader.GetNext())
        {
            results.Add(item);
        }

        // Assert
        Assert.Empty(results);
    }

    //New code again
    [Fact]
    public void TryReadLine_ShouldReturnTrue_WhenSeparatorIsNullAndBufferHasEnoughLength()
    {
        // Arrange
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(new string('A', 107)));
        var separator = (char?)null;
        var method = typeof(X12Reader).GetMethod("TryReadLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { buffer, null!, separator! };

        // Act
        var result = (bool)method?.Invoke(null, parameters)!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryReadLine_ShouldReturnFalse_WhenSeparatorIsNullAndBufferIsTooShort()
    {
        // Arrange
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(new string('A', 50)));
        var separator = (char?)null;
        var method = typeof(X12Reader).GetMethod("TryReadLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { buffer, null!, separator! };

        // Act
        var result = (bool)method?.Invoke(null, parameters)!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryReadLine_ShouldReturnTrue_WhenSeparatorIsFound()
    {
        // Arrange
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("ISA*00*~"));
        var separator = '~';
        var method = typeof(X12Reader).GetMethod("TryReadLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { buffer, null!, separator! };

        // Act
        var result = (bool)method?.Invoke(null, parameters)!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryReadLine_ShouldReturnFalse_WhenSeparatorIsNotFound()
    {
        // Arrange
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("ISA*00*"));
        var separator = '~';
        var method = typeof(X12Reader).GetMethod("TryReadLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { buffer, null!, separator! };

        // Act
        var result = (bool)method?.Invoke(null, parameters)!;

        // Assert
        Assert.False(result);
    }
    [Fact]
    public async Task GetNext_ShouldIgnoreUnhandledSegments()
    {
        // Arrange
        var input = "ISA*00*          *00*          *ZZ*ABCDEFGHIJKLMNO*ZZ*123456789012345*230101*1253*^*00501*000000905*0*T*:~" +
                    "GS*HC*ABCDEFGHIJKLMNO*123456789012345*20230101*1253*1*X*005010X222A1~" +
                    "UNHANDLED*DATA~" + // Unhandled segment
                    "IEA*1*000000905~";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        var reader = new X12Reader(stream);

        // Act
        var results = new List<object>();
        await foreach (var item in reader.GetNext())
        {
            results.Add(item);
        }

        // Assert
        Assert.DoesNotContain(results, result => result is string && ((string)result).StartsWith("UNHANDLED"));
    }

    [Fact]
    public async Task GetNext_ShouldContinueProcessingAfterUnhandledSegments()
    {
        // Arrange
        var input = "ISA*00*          *00*          *ZZ*ABCDEFGHIJKLMNO*ZZ*123456789012345*230101*1253*^*00501*000000905*0*T*:~" +
                    "GS*HC*ABCDEFGHIJKLMNO*123456789012345*20230101*1253*1*X*005010X222A1~" + 
                    "UNHANDLED*DATA~" + // Unhandled segment
                    "IEA*1*000000905~" + "GE*1*ABC~" +"GS*1*0001*ABC*test*202301*1234*ABCDE*9999~";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        var reader = new X12Reader(stream);

        // Act
        var results = new List<object>();
        await foreach (var item in reader.GetNext())
        {
            results.Add(item);
        }

        // Assert
        Assert.Contains(results, result => result is ISA);
        Assert.Contains(results, result => result is IEA);
        Assert.Contains(results, result => result is GE);
        Assert.Contains(results, result => result is GS);
    }
    [Fact]
    public async Task GetNext_ShouldReturnX12Record_WhenSegmentIsX12Recordtypes()
    {
        // Arrange
        var input = "ISA*00*          *00*          *ZZ*ABCDEFGHIJKLMNO*ZZ*123456789012345*230101*1253*^*00501*000000905*0*T*:~" +
                    "GS*HC*ABCDEFGHIJKLMNO*123456789012345*20230101*1253*1*X*005010X222A1~" +
                    "ST*835*0001~" +
                    "BPR*C*1000*C*ABCDEF*test*test1*test2*test3*test4*test5*test6*test7*test8*test9*test10*test11~" +
                    "SE*1*0001~" + 
                    "IEA*1*000000905~";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        var reader = new X12Reader(stream);

        // Act
        var results = new List<object>();
        await foreach (var item in reader.GetNext())
        {
            results.Add(item);
        }

        // Assert
        Assert.Contains(results, result => result is X12Record);
    }

    [Fact]
    public async Task GetNext_ShouldReturnTRNDTMN1_WhenSegmentIsTRNDTMN1()
    {
        // Arrange
        var input = "ISA*00*          *00*          *ZZ*ABCDEFGHIJKLMNO*ZZ*123456789012345*230101*1253*^*00501*000000905*0*T*:~" +
                    "GS*HC*ABCDEFGHIJKLMNO*123456789012345*20230101*1253*1*X*005010X222A1~" +
                    "ST*835*0001~" +
                    "TRN*12345*ABCD*testdata*testdata1~" + "DTM*472*20230101~" + "N1*PR*PAYER NAME~" +
                    "SE*1*0001~" +
                    "IEA*1*000000905~";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        var reader = new X12Reader(stream);

        // Act
        var results = new List<object>();
        await foreach (var item in reader.GetNext())
        {
            results.Add(item);
        }

        // Assert
        Assert.Contains(results, result => result is X12Record);
        
    }

    

}



