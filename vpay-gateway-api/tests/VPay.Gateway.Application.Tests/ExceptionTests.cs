using System.Runtime.Serialization;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

#nullable enable

namespace VPay.Gateway.Application.Tests;

public class ExceptionTests
{
    [Fact]
    public void WhenNewNoPrsConfigurationValidationException_ReturnArg()
    {
        var expected = new List<ValidationFailure> { new ValidationFailure("test", "test") };

        var actual = new NoPrsConfigurationValidationException(expected);

        actual.Errors.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void WhenSerializeNoPrsConfigurationValidationException_ReturnArg()
    {
        // arrange
        var expectedErrors = new List<ValidationFailure> { new ValidationFailure("test", "test") };
        var expected = new NoPrsConfigurationValidationException(expectedErrors);

        // act
        var actual = Serializer.SerializeAndDeserialize(expected);

        // assert
        actual.Should().NotBeNull();
        actual?.Errors.Should().BeEquivalentTo(expectedErrors);
        actual?.Message.Should().Be(expected.Message);
    }
}

internal static class Serializer
{
    public static TException? SerializeAndDeserialize<TException>(TException exception)
    {
        var formatter = new DataContractSerializer(typeof(TException),new List<Type> { typeof(ValidationFailure), typeof(List<ValidationFailure>)});

        using (var stream = new MemoryStream())
        {
            formatter.WriteObject(stream, exception);

            stream.Seek(0, 0);

            return (TException?)formatter.ReadObject(stream);
        }
    }
}
