using Microsoft.Data.SqlClient;

namespace VPay.Gateway.Data.Sql.Exceptions;

public static class SqlExceptionExtensions
{
    public static bool IsUniqueConstraintOrIndexException(this SqlException exception)
    {
        return exception.Number == 2627 || exception.Number == 2601;
    }
}
