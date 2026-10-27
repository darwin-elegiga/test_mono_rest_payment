using System.Reflection;
using Dapper;

namespace VPay.Gateway.Data.Mapping;

/// <summary>
/// The <see cref="FallBackTypeMapper" /> class can be used as a type mapper
/// for Dapper to allow for multiple different types of mappings for a column
/// that can fall back until it finds a mapper that can handle the property.
/// </summary>
public class FallBackTypeMapper : SqlMapper.ITypeMap
{
    private readonly IEnumerable<SqlMapper.ITypeMap> _mappers;

    public FallBackTypeMapper(IEnumerable<SqlMapper.ITypeMap> mappers)
    {
        _mappers = mappers;
    }

    public ConstructorInfo? FindConstructor(string[] names, Type[] types)
    {
        foreach (var mapper in _mappers)
        {
            try
            {
                var result = mapper.FindConstructor(names, types);
                if (result != null)
                {
                    return result;
                }
            }
            catch (NotImplementedException)
            {
                // the CustomPropertyTypeMap only supports a no-args
                // constructor and throws a not implemented exception.
                // to work around that, catch and ignore.
            }
        }

        return null;
    }

    public ConstructorInfo? FindExplicitConstructor()
    {
        return _mappers.Select(m => m.FindExplicitConstructor()).FirstOrDefault(result => result != null);
    }

    public SqlMapper.IMemberMap? GetConstructorParameter(ConstructorInfo constructor, string columnName)
    {
        foreach (var mapper in _mappers)
        {
            try
            {
                var result = mapper.GetConstructorParameter(constructor, columnName);
                if (result != null)
                {
                    return result;
                }
            }
            catch (NotImplementedException)
            {
                // the CustomPropertyTypeMap only supports a no-args
                // constructor and throws a not implemented exception.
                // to work around that, catch and ignore.
            }
        }

        return null;
    }

    public SqlMapper.IMemberMap? GetMember(string columnName)
    {
        foreach (var mapper in _mappers)
        {
            try
            {
                var result = mapper.GetMember(columnName);
                if (result != null)
                {
                    return result;
                }
            }
            catch (NotImplementedException)
            {
                // the CustomPropertyTypeMap only supports a no-args
                // constructor and throws a not implemented exception.
                // to work around that, catch and ignore.
            }
        }

        return null;
    }
}
