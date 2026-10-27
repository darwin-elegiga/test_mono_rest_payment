using Xunit;

namespace VPay.Gateway.Common.Tests;

public class StringHelperTests
{
    [Theory]
    [InlineData("CamelCase", "Camel_Case")]
    [InlineData("PascalCaseTest", "Pascal_Case_Test")]
    [InlineData("lowercase", "lowercase")]
    [InlineData("A", "A")]
    [InlineData("", "")]
    public void Underscore_ReturnsExpectedResult(string input, string expected)
    {
        var result = input.Underscore();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("HelloWorld", 2, false, "He")]
    [InlineData("HelloWorld", 2, true, "He")]
    [InlineData("HelloWorld", 3, false, "Hel")]
    [InlineData("HelloWorld", 3, true, "Hel")]
    [InlineData("HelloWorld", 4, false, "Hell")]
    [InlineData("HelloWorld", 4, true, "H...")]
    [InlineData("HelloWorld", 5, false, "Hello")]
    [InlineData("HelloWorld", 5, true, "He...")]
    [InlineData("Hi", 5, false, "Hi")]
    [InlineData("Hi", 5, true, "Hi")]
    [InlineData(null, 5, false, null)]
    [InlineData("", 5, false, "")]
    public void Truncate_ReturnsExpectedResult(string? input, int length, bool ellipsis, string? expected)
    {
        var result = input.Truncate(length, ellipsis);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1234567890", '*', 4, "******7890")]
    [InlineData("1234567890", '*', -2, "")]
    [InlineData("1234", '*', 4, "1234")]
    [InlineData("12", '*', 4, "12")]
    [InlineData("", '*', 4, "")]
    [InlineData(null, '*', 4, "")]
    [InlineData("abcdef", '#', 2, "####ef")]
    public void Mask_ReturnsExpectedResult(string? input, char mask, int show, string expected)
    {
        var result = input.Mask(mask, show);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "NULL", "EMPTY", false, "NULL")]
    [InlineData(null, null, null, false, "")]
    [InlineData("", null, null, false, "")]
    [InlineData("", "NULL", null, false, "")]
    [InlineData("", "NULL", "EMPTY", false, "EMPTY")]
    [InlineData("   ", "NULL", "EMPTY", false, "EMPTY")]
    [InlineData("abc", "NULL", "EMPTY", false, "abc")]
    [InlineData("abc", "NULL", "EMPTY", true, "ABC")]
    [InlineData("a\u0000b\u0001c", "NULL", "EMPTY", false, "abc")]
    public void Fix_ReturnsExpectedResult(string? input, string nullValue, string emptyValue, bool upper, string expected)
    {
        var result = input.Fix(nullValue, emptyValue, upper);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, false, null)]
    [InlineData("abc123", false, "abc123")]
    [InlineData("abc123", true, "ABC123")]
    [InlineData("abc'123_+$[]", true, "ABC'123_")]
    [InlineData("abc'123_.;@!", true, "ABC'123_.;@!")]
    [InlineData("a\u0000b\u0001c", false, "abc")]
    public void Clean_ReturnsExpectedResult(string? input, bool upper, string? expected)
    {
        var result = input.Clean(upper);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, false, null)]
    [InlineData("abc123", false, "abc123")]
    [InlineData("abc123\r\n", false, "abc123")]
    [InlineData("abc123", true, "ABC123")]
    [InlineData("abc'123_+$[]", true, "ABC'123_+$[]")]
    [InlineData("abc'123_.;@!", true, "ABC'123_.;@!")]
    [InlineData("a\u0000b\u0001c", false, "abc")]
    [InlineData("a\u0000b\u0001c", true, "ABC")]
    public void Sanitize_RemovesControlCharactersAndHandlesUpper(string input, bool upper, string expected)
    {
        var result = input?.Sanitize(upper);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("John Doe!@#", CharOptions.AlphaNumeric, "JohnDoe")]
    [InlineData("John Doe^@#", CharOptions.AlphaNumeric, "JohnDoe")]
    [InlineData("John Doe!@#", CharOptions.AlphaNumericSeparator, "John Doe")]
    [InlineData("John Doe!@#", CharOptions.AlphaNumericSeparatorPunctuation, "John Doe!@#")]
    [InlineData("John Doe's ", CharOptions.AlphaNumericSeparatorPunctuation, "John Doe's ")]
    [InlineData("John Doe$@#", CharOptions.AlphaNumericSeparatorPunctuationEnclosing, "John Doe@#")]
    [InlineData("John Doe$@#", CharOptions.AlphaNumericSeparatorPunctuationEnclosingSymbol, "John Doe$@#")]
    [InlineData("John Doe!@#", CharOptions.CustomExclusionRegexPattern, "John Doe", "[^a-zA-Z ]+")]
    [InlineData("John Doe^@#", CharOptions.CustomExclusionRegexPattern, "John Doe^@#", "[^a-zA-Z \\^@#]+")]
    [InlineData("John Doe.,;'\"", CharOptions.AlphaNumericSeparatorPunctuation, "John Doe.,;'\"")]
    public void Proper_RemovesNonProperCharacters(string input, CharOptions options, string expected, string pattern = "")
    {
        var result = options == CharOptions.CustomExclusionRegexPattern
            ? input.Proper(options, pattern)
            : input.Proper(options);
        Assert.Equal(expected, result);
    }
}
