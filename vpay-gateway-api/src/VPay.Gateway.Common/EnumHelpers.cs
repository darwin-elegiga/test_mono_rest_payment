using System.Runtime.Serialization;

namespace VPay.Gateway.Common;

public static class EnumHelpers
{
    /// <summary>
    /// Converts enum type item to its string representation.
    /// Expects use of <see cref="EnumMemberAttribute"/> definitions.
    /// </summary>
    /// <param name="value">The enum </param>
    /// <returns>The enum member value.</returns>
    public static string ToMember(this Enum value)
    {
        var attr = value.GetType().GetMember(value.ToString()).FirstOrDefault()?.
                GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault();

        return attr?.Value ?? value.ToString();
    }

    /// <summary>
    /// Converts enum string representation to enum type item.
    /// Supports use of <see cref="EnumMemberAttribute"/> definitions.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The enum string representation.</param>
    /// <param name="defaultValue">The enum default value.</param>
    /// <returns>The enum type item.</returns>
    public static TEnum ToEnum<TEnum>(this string value, TEnum defaultValue) where TEnum : struct
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        TEnum result;
        var enumType = typeof(TEnum);
        foreach (var enumName in Enum.GetNames(enumType))
        {
            var fieldInfo = enumType.GetField(enumName);
            if (fieldInfo == null)
            {
                return defaultValue;
            }

            var enumMemberAttribute = ((EnumMemberAttribute[])fieldInfo.GetCustomAttributes(typeof(EnumMemberAttribute), true)).FirstOrDefault();
            if (enumMemberAttribute?.Value == value)
            {
                return Enum.TryParse(enumName, true, out result) ? result : defaultValue;
            }
        }

        return Enum.TryParse(value, true, out result) ? result : defaultValue;
    }
}
