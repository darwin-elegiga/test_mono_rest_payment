using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace VPay.Gateway.Common.Tests;

/// <summary>
/// Tests for <see cref="SmartEnumValueConverter"/>
/// </summary>
public class SmartEnumValueConverterTests
{
    // Dummy SmartEnum base for testing
    public abstract class SmartEnum<TEnum, TValue>
        where TEnum : SmartEnum<TEnum, TValue>
    {
        public TValue Value { get; }
        protected SmartEnum(TValue value) => Value = value;
        public static TEnum FromValue(TValue value)
        {
            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetValue(null) is TEnum instance && instance.Value.Equals(value))
                {
                    return instance;
                }
            }
            throw new ArgumentException("No matching value");
        }
    }

    // Example SmartEnum for testing
    public class TestEnum : SmartEnum<TestEnum, int>
    {
        public static readonly TestEnum One = new(1);
        public static readonly TestEnum Two = new(2);
        public TestEnum(int value) : base(value) { }
    }

    [Fact]
    public void CanRead_ShouldReturnTrue()
    {
        var converter = new SmartEnumValueConverter();
        Assert.True(converter.CanRead);
    }

    [Fact]
    public void CanWrite_ShouldReturnTrue()
    {
        var converter = new SmartEnumValueConverter();
        Assert.True(converter.CanWrite);
    }

    [Fact]
    public void WriteJson_ShouldWriteNull_WhenValueIsNull()
    {
        var converter = new SmartEnumValueConverter();
        var writer = new JTokenWriter();
        converter.WriteJson(writer, null, new JsonSerializer());
        Assert.True(writer.Token.Type == JTokenType.Null);
    }

    [Fact]
    public void WriteJson_ShouldWriteValue_WhenValueIsSmartEnum()
    {
        var converter = new SmartEnumValueConverter();
        var writer = new JTokenWriter();
        converter.WriteJson(writer, TestEnum.One, new JsonSerializer());
        Assert.Equal(1, (int)writer.Token);
    }

    //[Fact]
    //public void ReadJson_ShouldReturnSmartEnumInstance_FromInteger()
    //{
    //    var converter = new SmartEnumValueConverter();
    //    var reader = new JsonTextReader(new StringReader("1"));

    //    var result = converter.ReadJson(reader, typeof(TestEnum), TestEnum.One, new JsonSerializer());

    //    Assert.NotNull(result);
    //    Assert.IsType<TestEnum>(result);
    //    Assert.Equal(1, ((TestEnum)result).Value);
    //}

    //[Fact]
    //public void ReadJson_ShouldThrowJsonSerializationException_OnError()
    //{
    //    var converter = new SmartEnumValueConverter();
    //    var reader = new JsonTextReader(new StringReader("\"bad\""));

    //    Assert.Throws<JsonSerializationException>(() =>
    //        converter.ReadJson(reader, typeof(TestEnum), null, new JsonSerializer()));
    //}

    //[Fact]
    //public void CanConvert_ShouldReturnTrue_ForSmartEnumType()
    //{
    //    var converter = new SmartEnumValueConverter();
    //    Assert.True(converter.CanConvert(typeof(TestEnum)));
    //}

    //[Fact]
    //public void CanConvert_ShouldReturnFalse_ForNonSmartEnumType()
    //{
    //    var converter = new SmartEnumValueConverter();
    //    Assert.False(converter.CanConvert(typeof(string)));
    //}

    //[Fact]
    //public void GetBaseType_ShouldReturnNull_ForNonMatchingType()
    //{
    //    var method = typeof(SmartEnumValueConverter)
    //        .GetMethod("GetBaseType", BindingFlags.NonPublic | BindingFlags.Static);
    //    var result = method.Invoke(null, new object[] { typeof(SmartEnum<,>), typeof(string) });
    //    Assert.Null(result);
    //}

    //[Fact]
    //public void GetBaseType_ShouldReturnType_ForMatchingType()
    //{
    //    var method = typeof(SmartEnumValueConverter)
    //        .GetMethod("GetBaseType", BindingFlags.NonPublic | BindingFlags.Static);
    //    var result = method.Invoke(null, new object[] { typeof(SmartEnum<,>), typeof(TestEnum) });
    //    var type = result as Type;
    //    var type2 = typeof(TestEnum);
    //    Assert.NotNull(result);
    //    Assert.Equal(typeof(TestEnum), result);
    //}
}
