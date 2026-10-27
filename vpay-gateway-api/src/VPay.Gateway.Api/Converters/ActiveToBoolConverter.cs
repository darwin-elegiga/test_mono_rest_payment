using AutoMapper;

namespace VPay.Gateway.Api.Converters;

/// <summary>
/// Converts the specified character flag representation of an active/inactive value
/// into true or false boolean value.
/// </summary>
public class ActiveToBoolConverter : IValueConverter<string, bool>
{
    private readonly bool _defaultValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveToBoolConverter"/> class.
    /// </summary>
    /// <param name="defaultValue">The conversion default value.</param>
    public ActiveToBoolConverter(bool defaultValue = default) =>
        _defaultValue = defaultValue;

    /// <summary>
    /// Converts the specified character flag representation of an active/inactive value
    /// into true or false boolean value. The conversion helps determine if the flag
    /// is set or not.
    /// </summary>
    /// <param name="sourceMember">The character flag representing an active/inactive
    /// value.</param>
    /// <param name="context">Context information regarding resolution of the destination
    /// value (not used).</param>
    /// <returns>True if the flag is set to ACTIVE=Active; False otherwise.</returns>
    public bool Convert(string sourceMember, ResolutionContext context) =>
        string.IsNullOrWhiteSpace(sourceMember) ?
            _defaultValue :
            sourceMember.Trim().Equals("ACTIVE", System.StringComparison.OrdinalIgnoreCase);
}
