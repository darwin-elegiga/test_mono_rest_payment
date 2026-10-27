using System.Text;
using MassTransit;

namespace VPay.Gateway.MessageBus.RabbitMQ;

public static class ExceptionInfoExtensions
{
    public static string GetString(this ExceptionInfo exceptionInfo)
    {
        var message = exceptionInfo.Message;
        var sb = new StringBuilder(exceptionInfo.ExceptionType);
        if (message?.Length > 0)
        {
            sb.Append(": ").Append(message);
        }

        if (exceptionInfo.InnerException != null)
        {
            sb
                .Append(" ---> ")
                .AppendLine(exceptionInfo.InnerException.GetString())
                .Append("   --- End of inner exception stack trace ---");
        }

        var stackTrace = exceptionInfo.StackTrace;
        if (stackTrace?.Length > 0)
        {
            sb.AppendLine().Append(stackTrace);
        }

        return sb.ToString();
    }
}
