using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace VPay.Optum.PaymentRouting.Common.Helpers;

public static class StringHelper
{
    public static string Underscore(this string value) =>
        string.Concat(value.Select((x, i) => i > 0 && char.IsUpper(x) ? $"_{x}" : x.ToString()));

    /// <summary>
    /// Truncate the string to the specified length.
    /// </summary>
    /// <param name="value">The string to be truncated.</param>
    /// <param name="length">The maximum length. Must be greater than zero.</param>
    /// <param name="ellipsis">true to add ellipsis to the truncated text; otherwise, false.</param>
    /// <returns>The truncated string.</returns>
    public static string? Truncate(this string? value, int length, bool ellipsis = false)
    {
        if (string.IsNullOrEmpty(value) || length < 1)
        {
            return value;
        }

        const string Ellipse = "...";
        if (value.Length > length)
        {
            if (ellipsis && length > Ellipse.Length)
            {
                return value[..(length - Ellipse.Length)] + Ellipse;
            }

            return value[..length];
        }

        return value;
    }

    /// <summary>
    /// Masks some or all of the characters in a string using the provided mask character,
    /// except for the specified number of show characters at the end of the string.
    /// </summary>
    /// <param name="value">The string to mask.</param>
    /// <param name="mask">The character to use as mask. Defaults to asterisk.</param>
    /// <param name="show">The number of characters to remain unmasked at the end of the string.
    /// Must be greater than zero but less than the length of the value. Defaults to 4.</param>
    /// <returns>The masked valued, if applicable; empty when null, the original value otherwise.</returns>
    public static string? Mask(this string? value, char mask = '*', int show = 4)
    {
        // address null or empty values
        if (string.IsNullOrWhiteSpace(value) || show < 1)
        {
            return string.Empty;
        }

        // address shorter values
        if (value.Length <= show)
        {
            return value;
        }

        // set asterisk to hide first digits
        string asterisks = new(mask, value.Length - show);

        // pick last digits for showing
        string last = value.Substring(value.Length - show, show);

        return asterisks + last;
    }

    /// <summary>
    /// Cleans an output text value for logging or display purposes by removing non-printable characters
    /// and replacing non-printable values like null or empty with suitable values.
    /// </summary>
    /// <param name="value">The string to fix.</param>
    /// <param name="nullValue">The value to display or log when value is null.</param>
    /// <param name="emptyValue">The value to display or log when value is empty; leave empty to use the same value as the null value.</param>
    /// <param name="upper">When True and value is not null, converts the input text to upper case.</param>
    /// <param name="matchTimeout">The regex match timeout interval. The default (null) is 10 seconds. Use Regex.InfiniteMatchTimeout to match indefinitely.</param>
    /// <returns>When null or empty, the replaced value; the original value otherwise.</returns>
    public static string? Fix(this string? value, string nullValue = "", string emptyValue = "", bool upper = false, TimeSpan? matchTimeout = null)
    {
        // fix null values
        if (value == null)
        {
            return nullValue ?? "";
        }

        // fix empty values
        if (string.IsNullOrWhiteSpace(value))
        {
            return emptyValue ?? "";
        }

        // trim values
        return (value.Sanitize(upper, matchTimeout) ?? nullValue).Trim();
    }

    /// <summary>
    /// Cleans an input text value for database storage by removing control characters and other special characters
    /// not commonly found in proper names, and optionally converts the text to upper case for match disambiguation.
    /// </summary>
    /// <param name="value">The string to clean.</param>
    /// <param name="upper">When True and value is not null, converts the input text to upper case.</param>
    /// <param name="options">The list of character categories to allow.</param>
    /// <returns>When not null, the original string without non-proper characters; null otherwise.</returns>
    public static string? Clean(this string? value, bool upper = false, CharOptions options = CharOptions.AlphaNumericSeparatorPunctuation)
    {
        if (value == null)
        {
            return null;
        }

        if (upper)
        {
            return value.Proper(options).ToUpperInvariant().Trim();
        }

        return value.Proper(options).Trim();
    }

    /// <summary>
    /// Removes control characters (Cc), unassigned characters (Cn), formatting characters (Cf), private use characters (Co)
    /// and UTF-8 invalid characters (Cs). CR and LF are both control characters (Cc) but explicit removal of CR+LF
    /// is required by Sonar for logging purposes. Intended for cleaning of printable text words and paragraphs.
    /// </summary>
    /// <param name="value">The original string to modify.</param>
    /// <param name="upper">When True and value is not null, converts the input text to upper case.</param>
    /// <param name="matchTimeout">The regex match timeout interval. The default (null) is 10 seconds. Use Regex.InfiniteMatchTimeout to match indefinitely.</param>
    /// <returns>When not null, the original string without the control, unassigned, formatting, and invalid characters; empty otherwise</returns>
    /// <remarks>Source: https://en.wikipedia.org/wiki/Unicode_character_property#General_Category</remarks>
    public static string? Sanitize(this string? value, bool upper = false, TimeSpan? matchTimeout = null)
    {
        if (value == null)
        {
            return value;
        }
        var result = Regex.Replace(value, @"[\p{Cc}\p{Cn}\p{Cf}\p{Cs}\p{Co}]", "", RegexOptions.Compiled, matchTimeout ?? TimeSpan.FromSeconds(10));
        result = result.ReplaceLineEndings(string.Empty);  // Required to satisfy Sonar strict logging rules.
        return upper ? result.ToUpperInvariant() : result;
    }
    /// <summary>
    /// Creates a copy of the specified object with all string property values sanitized and truncated to a maximum
    /// length.
    /// </summary>
    /// <remarks>Only properties that are both readable and writable are processed. Non-string properties are
    /// copied as-is. The method uses the object's runtime type to create a new instance and copies property values
    /// accordingly.</remarks>
    /// <param name="obj">The object whose string properties will be sanitized. Must not be <see langword="null"/>.</param>
    /// <param name="maxLength">The maximum number of characters allowed for each string property value after sanitization. Defaults to 256.
    /// Must be positive.</param>
    /// <returns>A new object of the same type as <paramref name="obj"/>, with all string properties sanitized and truncated.
    /// Returns <see langword="null"/> if <paramref name="obj"/> is <see langword="null"/>.</returns>
    public static object SanitizeObjectStrings(object obj, int maxLength = 256)
    {
        if (obj == null) { return null!; }

        var type = obj.GetType();
        var sanitized = Activator.CreateInstance(type);

        foreach (var prop in type.GetProperties())
        {
            if (!prop.CanRead || !prop.CanWrite) { continue; }

            var value = prop.GetValue(obj);

            if (value is string str)
            {
                prop.SetValue(sanitized, str.Sanitize().Truncate(maxLength));
            }
            else if (value != null && !type.IsPrimitive && !(value is DateTime) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType))
            {
                // Handle nested objects recursively, but skip collections
                prop.SetValue(sanitized, SanitizeObjectStrings(value, maxLength));
            }
            else if (value is System.Collections.IEnumerable collection && !(value is string))
            {
                // Handle collections recursively, but avoid parameter count mismatch
                var elementType = prop.PropertyType.IsGenericType
                    ? prop.PropertyType.GetGenericArguments()[0]
                    : typeof(object);

                var sanitizedList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                foreach (var item in collection)
                {
                    sanitizedList.Add(SanitizeObjectStrings(item, maxLength));
                }
                prop.SetValue(sanitized, sanitizedList);
            }
            else
            {
                prop.SetValue(sanitized, value);
            }
        }

        return sanitized!;
    }

    /// <summary>
    /// Removes all non-alphanumeric, non-punctuation, non-separator characters and allows only upper-case
    /// letters (Lu), lower-case letters (Ll), decimal numbers (Nd), letter numbers (Nl), spaces (Zs),
    /// non-enclosing marks (Mn, Mc) and punctuation marks, connectors and dashes (Po, Pc, Pd).
    /// Other characters can be allowed or disallowed through the CharOptions parameter.
    /// Intended for cleaning of common proper names.
    /// </summary>
    /// <param name="value">The original string to modify.</param>
    /// <param name="options">The list of character categories to allow (not remove).</param>
    /// <param name="pattern">
    /// The exclusion regex pattern matching the characters to remove or not remove when using CustomExclusionRegexPattern option, i.e.: "[^a-zA-Z0-9_-,.]+"
    /// will allow only basic alphanumeric characters plus underscore, dash, comma and period characters.
    /// </param>
    /// <param name="matchTimeout">The regex match timeout interval. The default (null) is 10 seconds. Use Regex.InfiniteMatchTimeout to match indefinitely.</param>
    /// <returns>The original string without non-proper characters.</returns>
    /// <remarks>Source: https://en.wikipedia.org/wiki/Unicode_character_property#General_Category</remarks>
    public static string Proper(this string value, CharOptions options = CharOptions.AlphaNumericSeparatorPunctuation, string pattern = "", TimeSpan? matchTimeout = null)
    {
        var timeout = matchTimeout ?? TimeSpan.FromSeconds(10);

        return options switch
        {
            CharOptions.AlphaNumeric => Regex.Replace(value, @"[^\p{Lu}\p{Ll}\p{Nd}\p{Nl}]+", "", RegexOptions.Compiled, timeout),
            CharOptions.AlphaNumericSeparator => Regex.Replace(value, @"[^\p{Lu}\p{Ll}\p{Nd}\p{Nl}\p{Zs}\p{Pc}\p{Pd}]+", "", RegexOptions.Compiled, timeout),
            CharOptions.AlphaNumericSeparatorPunctuation => Regex.Replace(value, @"[^\p{Lu}\p{Ll}\p{Nd}\p{Nl}\p{Zs}\p{Pc}\p{Pd}\p{Po}\p{Mn}\p{Mc}]+", "", RegexOptions.Compiled, timeout),
            CharOptions.AlphaNumericSeparatorPunctuationEnclosing => Regex.Replace(value, @"[^\p{Lu}\p{Ll}\p{Nd}\p{Nl}\p{Zs}\p{Pc}\p{Pd}\p{Po}\p{Ps}\p{Pe}\p{Pi}\p{Pf}\p{Mn}\p{Mc}\p{Me}]+", "", RegexOptions.Compiled, timeout),
            CharOptions.AlphaNumericSeparatorPunctuationEnclosingSymbol => Regex.Replace(value, @"[^\p{Lu}\p{Ll}\p{Nd}\p{Nl}\p{Zs}\p{Pc}\p{Pd}\p{Po}\p{Ps}\p{Pe}\p{Pi}\p{Pf}\p{Mn}\p{Mc}\p{Me}\p{Sm}\p{Sc}]+", "", RegexOptions.Compiled, timeout),
            CharOptions.CustomExclusionRegexPattern => Regex.Replace(value, pattern, "", RegexOptions.None, matchTimeout ?? TimeSpan.FromSeconds(10)),
            _ => value,
        };
    }
    /// <summary>
    /// Prepares a string value for safe use in log entries by removing control and line-separator
    /// characters and restricting the content to printable ASCII, then truncating to the specified
    /// maximum length.
    /// </summary>
    /// <param name="value">The original string to sanitize for logging.</param>
    /// <param name="maxLength">The maximum allowed length of the resulting string.</param>
    /// <returns>A log-safe string, or <see langword="null"/> if <paramref name="value"/> is <see langword="null"/>.</returns>
    public static string? SanitizeForLogging(this string? value, int maxLength = 256)
    {
        if (value == null)
        {
            return null;
        }

        // First remove control, unassigned, formatting and invalid characters (including line breaks).
        var sanitized = value.Sanitize();

        // Then restrict to printable ASCII characters to avoid confusing log consumers.
        sanitized = Regex.Replace(sanitized!, @"[^\x20-\x7E]", string.Empty, RegexOptions.Compiled);

        return sanitized.Truncate(maxLength);
    }
    /// <summary>
    /// Removes all line ending characters (CR, LF, and CRLF) from the specified string.
    /// Intended for preparing user input for logging to avoid log forging via injected new lines.
    /// </summary>
    /// <param name="value">The original string to modify.</param>
    /// <returns>The input string without any '\r' or '\n' characters; null or empty unchanged.</returns>
    public static string? RemoveLineEndings(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Replace("\r\n", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace("\r", string.Empty);
    }

}

public enum CharOptions
{
    [Description("Alphanumeric")]
    AlphaNumeric,
    [Description("Alphanumeric, Separators, Connectors & Dashes")]
    AlphaNumericSeparator,
    [Description("Alphanumeric, Separators, Punctuation Marks")]
    AlphaNumericSeparatorPunctuation,
    [Description("Alphanumeric, Separators, Punctuation & Enclosing Marks")]
    AlphaNumericSeparatorPunctuationEnclosing,
    [Description("Alphanumeric, Separators, Punctuation & Enclosing Marks, Math & Currency Symbols")]
    AlphaNumericSeparatorPunctuationEnclosingSymbol,
    [Description("Custom Exclusion Regex Pattern")]
    CustomExclusionRegexPattern
}
