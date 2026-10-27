using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace VPay.DocSys.Parsing.Common.Extensions
{
    public static class StringHelper
    {
        /// <summary>
        /// Takes the given string (uses <see cref="string.Empty"/> if null before returning exactly <see cref="length"/> long string, will pad and truncate if required to reach <see cref="length"/> 
        /// </summary>
        /// <param name="value">string to use</param>
        /// <param name="length">exact length to return of new string, using a negative value is the same as 0</param>
        /// <returns>new string</returns>
        public static string PadAndTrim(this string value, int length) => (value ?? string.Empty).PadRight(length < 0 ? 0 : length).Substring(0, length < 0 ? 0 : length);

        /// <summary>
        /// Takes the given string (uses <see cref="string.Empty"/> if null before returning exactly <see cref="length"/> long string, will truncate if required to reach <see cref="length"/> 
        /// </summary>
        /// <param name="value">string to use</param>
        /// <param name="length">exact length to return of new string, using a negative value is the same as 0</param>
        /// <returns>new string</returns>
        public static string TruncateAndTrim(this string value, int length)
        {
            if (string.IsNullOrWhiteSpace(value) || length < 1)
            {
                return string.Empty;
            }

            if (length > value.Length)
            {
                return value.Trim();
            }

            return value[..length].Trim();
        }

        /// <summary>
        /// Truncate the string to the specified length.
        /// </summary>
        /// <param name="value">The string to be truncated.</param>
        /// <param name="length">The maximum length. Must be greater than zero.</param>
        /// <param name="ellipsis">true to add ellipsis to the truncated text; otherwise, false.</param>
        /// <returns>The truncated string.</returns>
        public static string Truncate(this string value, int length, bool ellipsis = false)
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
        public static string Mask(this string value, char mask = '*', int show = 4)
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
        public static string Fix(this string value, string nullValue = "", string emptyValue = "", bool upper = false, TimeSpan? matchTimeout = null)
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
        public static string Clean(this string value, bool upper = false, CharOptions options = CharOptions.AlphaNumericSeparatorPunctuation)
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
        /// Removes control characters (Cc), unassigned characters (Cn), formatting characters (Cf),
        /// and UTF-8 invalid characters (Cs). Intended for cleaning of printable text words and paragraphs.
        /// </summary>
        /// <param name="value">The original string to modify.</param>
        /// <param name="upper">When True and value is not null, converts the input text to upper case.</param>
        /// <param name="matchTimeout">The regex match timeout interval. The default (null) is 10 seconds. Use Regex.InfiniteMatchTimeout to match indefinitely.</param>
        /// <returns>When not null, the original string without the control, unassigned, formatting, and invalid characters; empty otherwise</returns>
        /// <remarks>Source: https://en.wikipedia.org/wiki/Unicode_character_property#General_Category</remarks>
        public static string Sanitize(this string value, bool upper = false, TimeSpan? matchTimeout = null)
        {
            if (value == null)
            {
                return value;
            }
            var result = Regex.Replace(value, @"[\p{Cc}\p{Cn}\p{Cf}\p{Cs}]", "", RegexOptions.Compiled, matchTimeout ?? TimeSpan.FromSeconds(10));
            return upper ? result.ToUpperInvariant() : result;
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
}
