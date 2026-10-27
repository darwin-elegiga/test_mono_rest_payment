using System;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace VPay.Optum.PaymentRouting.TestsCommon.Exceptions
{
    /// <summary>
    /// A helper class that uses reflection to create a SqlException with a specific number and message
    /// This is required because SqlException is sealed and is not easily mocked
    /// Unfortunately, this is brittle code and will need to be changed if the implementation underlying it changes
    /// This seems acceptable since this is not production code, and just used for developer testing
    /// </summary>
    public static class SqlExceptionCreator
    {
        private static T Construct<T>(params object[] p)
        {
            var ctors = typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)ctors.First(ctor => ctor.GetParameters().Length == p.Length).Invoke(p);
        }

        private static void SetProperty<T>(T targetObject, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(targetObject, value);
            }
            else
            {
                throw new InvalidOperationException("No field with name " + fieldName);
            }
        }

        public static SqlException NewSqlException(int number, string message = "")
        {
            SqlErrorCollection collection = Construct<SqlErrorCollection>();
            SqlError error = Construct<SqlError>(number, (byte)2, (byte)3, "server_name", "error_message", "proc", 100, null);

            typeof(SqlErrorCollection)
                .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(collection, new object[] { error });

            var sqlEx = typeof(SqlException)
                .GetMethod(
                    "CreateException",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    CallingConventions.ExplicitThis,
                    new[] { typeof(SqlErrorCollection), typeof(string) },
                    Array.Empty<ParameterModifier>()
                )
                ?.Invoke(null, new object[] { collection, "7.0.0" }) as SqlException;

            if (!string.IsNullOrWhiteSpace(message))
            {
                SetProperty(sqlEx, "_message", message);
            }

            return sqlEx;
        }
    }
}
