using System.Diagnostics.CodeAnalysis;

namespace VPay.Gateway.Data.Mapping;

/// <summary>
/// Specifies the database column that a property is mapped to.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
[SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "We want the ability to extend this class")]
public class ColumnAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the column the property is mapped to.</param>
    public ColumnAttribute(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"{nameof(name)} cannot be null or an empty string", nameof(name));
        }

        Name = name;
    }

    /// <summary>
    /// Gets the name of the column the property is mapped to.
    /// </summary>
    public string Name { get; }
}
