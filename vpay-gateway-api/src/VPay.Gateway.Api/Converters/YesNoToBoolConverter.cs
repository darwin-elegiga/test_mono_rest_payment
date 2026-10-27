using AutoMapper;

namespace VPay.Gateway.Api.Converters;

/// <summary>
/// Converts the specified single-character flag representation of a yes/no value
/// into true or false boolean value.
/// </summary>
public class YesNoToBoolConverter : IValueConverter<string, bool>
{
    private readonly bool _defaultValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="YesNoToBoolConverter"/> class.
    /// </summary>
    /// <param name="defaultValue">The conversion default value.</param>
    public YesNoToBoolConverter(bool defaultValue = default) =>
        _defaultValue = defaultValue;

    /// <summary>
    /// Converts the specified single-character flag representation of a yes/no value
    /// into true or false boolean value. The conversion helps determine if the flag
    /// is set or not.
    /// </summary>
    /// <param name="sourceMember">The single-character flags representing a yes/no
    /// value.</param>
    /// <param name="context">Context information regarding resolution of the destination
    /// value (not used).</param>
    /// <returns>True if the flag is set to Y=Yes; False otherwise.</returns>
    public bool Convert(string sourceMember, ResolutionContext context) =>
        string.IsNullOrWhiteSpace(sourceMember) ?
            _defaultValue :
            sourceMember.Trim().Equals("Y", System.StringComparison.OrdinalIgnoreCase);
}
