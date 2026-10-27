using VPay.Optum.PaymentRouting.Common.Helpers;

namespace VPay.Optum.PaymentRouting.Common.Tests.Helpers;

public class StringHelperTests
{
    private class TestClass
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Count { get; set; }
        public DateTime Date { get; set; }
    }

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
    public void Truncate_ReturnsExpectedResult(string input, int length, bool ellipsis, string expected)
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
    public void Mask_ReturnsExpectedResult(string input, char mask, int show, string expected)
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
    public void Fix_ReturnsExpectedResult(string input, string nullValue, string emptyValue, bool upper, string expected)
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
    public void Clean_ReturnsExpectedResult(string input, bool upper, string expected)
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
    [InlineData("a\nb\nc", false, "abc")]         // \u000A
    [InlineData("a\rb\rc", false, "abc")]         // \u000D
    [InlineData("a\u2028b\u2028c", false, "abc")] // \u2028
    [InlineData("a\u2029b\u2029c", false, "abc")] // \u2029
    [InlineData("a\n\rb", false, "ab")]
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

    [Theory]
    [InlineData(null, null, 0, "2024-01-01", "", "", 0, "2024-01-01")]
    [InlineData("Hello\nWorld", "Test\u0000String", 0, "2024-01-01", "HelloWorld", "TestString", 0, "2024-01-01")]
    [InlineData("A\r\nB\u2028C", "D\u0000E\u0001F", 0, "2025-12-31", "ABC", "DEF", 0, "2025-12-31")]
    [InlineData("X", "Y", 3, "2023-05-05", "X", "Y", 0, "2023-05-05")]
    [InlineData("12345678901234567890", "Short", 0, "2022-02-02", "12345678901234567890", "Short", 0, "2022-02-02")]
    public void SanitizeObjectStrings_InlineData_SanitizesStringsAndCopiesOthers(
       string name, string description, int count, string date,
       string expectedName, string expectedDescription, int expectedCount, string expectedDate)
    {
        var obj = new TestClass
        {
            Name = name ?? "",
            Description = description ?? "",
            Count = count,
            Date = DateTime.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
        };

        var result = (TestClass)StringHelper.SanitizeObjectStrings(obj, 256);

        Assert.Equal(expectedName, result.Name ?? "");
        Assert.Equal(expectedDescription, result.Description ?? "");
        Assert.Equal(expectedCount, result.Count);
        Assert.Equal(DateTime.ParseExact(expectedDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), result.Date);
    }

    // Remove line endings and verify the result.
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("Hello World", "Hello World")]
    [InlineData("Hello\nWorld\r", "HelloWorld")]
    [InlineData("Line1\r\nLine2\nLine3\r", "Line1Line2Line3")]
    [InlineData("\r\n", "")]
    [InlineData("NoLineEndings", "NoLineEndings")]
    public void RemoveLineEndings_RemovesAllLineEndings(string input, string expected)
    {
        var result = input.RemoveLineEndings();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("Hello World", "Hello World")]
    [InlineData("Hello\nWorld\r", "HelloWorld")]
    [InlineData("Line1\r\nLine2\nLine3\r", "Line1Line2Line3")]
    [InlineData("Hello\x01World", "HelloWorld")]
    [InlineData("Printable ASCII: ABC123!@#", "Printable ASCII: ABC123!@#")]
    [InlineData("This is a long string that should be truncated", "This is a long string that should be truncated")]
    [InlineData("A string with emoji 😊", "A string with emoji ")]
    [InlineData("A string with control chars \u0000\u0001", "A string with control chars ")]
    public void SanitizeForLogging_RemovesNonPrintableAndTruncates(string input, string expected)
    {
        var result = input.SanitizeForLogging(256);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789" +
        "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789" +
        "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789" +
        "012345678901234567890123456789012345678901234567890123456789",
        "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789" +
        "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789" +
        "01234567890123456789012345678901234567890123456789012345")]
    public void SanitizeForLogging_RemovesNonPrintableAndTruncatesLongText(string input, string expected)
    {
        var result = input.SanitizeForLogging(256);
        Assert.Equal(expected, result);
    }
}
