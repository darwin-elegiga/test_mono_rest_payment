using System;
using System.Threading.Tasks;
using Polly;

namespace VPay.DocSys.Generation
{
    public static class PolicyHelper
    {
        public static void Execute(this ISyncPolicy policy, Action action, string operationKey)
        {
            policy.Execute((c) => action(), new Context(operationKey));
        }
        public static void Execute(this ISyncPolicy policy, Action<Context> action, string operationKey)
        {
            policy.Execute(action, new Context(operationKey));
        }

        public static TResult Execute<TResult>(this ISyncPolicy policy, Func<TResult> action, string operationKey)
        {
            return policy.Execute((c) => action(), new Context(operationKey));
        }
        public static TResult Execute<TResult>(this ISyncPolicy policy, Func<Context, TResult> action, string operationKey)
        {
            return policy.Execute(action, new Context(operationKey));
        }

        public static Task ExecuteAsync(this IAsyncPolicy policy, Func<Task> action, string operationKey)
        {
            return policy.ExecuteAsync((c) => action(), new Context(operationKey));
        }
        public static Task ExecuteAsync(this IAsyncPolicy policy, Func<Context, Task> action, string operationKey)
        {
            return policy.ExecuteAsync(action, new Context(operationKey));
        }

        public static Task<TResult> ExecuteAsync<TResult>(this IAsyncPolicy policy, Func<Task<TResult>> action, string operationKey)
        {
            return policy.ExecuteAsync((c) => action(), new Context(operationKey));
        }
        public static Task<TResult> ExecuteAsync<TResult>(this IAsyncPolicy policy, Func<Context, Task<TResult>> action, string operationKey)
        {
            return policy.ExecuteAsync(action, new Context(operationKey));
        }
    }
}
