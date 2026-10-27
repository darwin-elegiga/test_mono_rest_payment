using Dapper;

namespace VPay.Gateway.Data.Mapping;

/// <summary>
/// The <see cref="ColumnAttributeTypeMapper{T}" /> class can be used as a type mapper
/// for Dapper to allow for mapping to properties automatically from a column name when the
/// property being mapped to is decorated with a <see cref="ColumnAttribute"/>.  If the property
/// is not decorated with the <see cref="ColumnAttribute"/>, it will fall back to using the default
/// mapper for the type.
/// </summary>
/// <typeparam name="T">The type being mapped to.</typeparam>
public class ColumnAttributeTypeMapper<T> : FallBackTypeMapper
{
    public ColumnAttributeTypeMapper()
        : base(new SqlMapper.ITypeMap[]
        {
                CreateColumnMap(),
                new DefaultTypeMap(typeof(T))
        })
    {
    }

    /// <summary>
    /// Create a map that can be used to map a column to a property decorated with
    /// a <see cref="ColumnAttribute"/>.
    /// </summary>
    /// <returns>The mapper for the type.</returns>
    private static SqlMapper.ITypeMap CreateColumnMap()
    {
        return new CustomPropertyTypeMap(
            typeof(T),
            (type, columnName) => Array.Find(
                type.GetProperties(),
                prop => prop.GetCustomAttributes(false)
                    .OfType<ColumnAttribute>()
                    .Any(attribute => attribute.Name == columnName))
            );
    }
}
