using System.Data;
using Ardalis.SmartEnum;
using Dapper;

namespace VPay.Optum.PaymentRouting.Data
{
    public class SmartEnumTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum>
        where TEnum : SmartEnum<TEnum>
    {
        private readonly TEnum _defaultValue;

        /// <summary>
        ///  Sets up the <see cref="SmartEnumTypeHandler{TEnum}"/>
        /// </summary>
        public SmartEnumTypeHandler()
        {
            _defaultValue = null;
        }

        /// <summary>
        ///  Sets up the <see cref="SmartEnumTypeHandler{TEnum}"/>
        /// </summary>
        /// <param name="defaultValue">This is the defaultValue that is used if it cannot parse the value</param>
        public SmartEnumTypeHandler(TEnum defaultValue)
        {
            _defaultValue = defaultValue;
        }

        public override TEnum Parse(object value)
        {
            if (_defaultValue == null)
            {
                return SmartEnum<TEnum>.FromName(value?.ToString(), true);
            }

            if (SmartEnum<TEnum>.TryFromName(value?.ToString(), true, out var result))
            {
                return result;
            }

            return _defaultValue;
        }

        public override void SetValue(IDbDataParameter parameter, TEnum value)
        {
            parameter.Value = value.Name;
            parameter.DbType = DbType.String;
        }
    }
}
