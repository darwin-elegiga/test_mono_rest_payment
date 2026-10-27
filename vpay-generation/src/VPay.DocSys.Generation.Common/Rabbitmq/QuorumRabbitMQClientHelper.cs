using System;
using System.Threading;
using RabbitMQ.Client;

namespace VPay.DocSys.Generation.Common.Rabbitmq
{
    public static class QuorumRabbitMQClientHelper
    {
        //
        // Summary:
        //     Blocks current thread until the connection has recovered or the specified timeout
        //     period has elapsed.
        //
        // Parameters:
        //   connection:
        //     The connection to wait for.
        //
        //   timeout:
        //     The timeout period in seconds.
        public static void WaitForAutoRecovery(this IConnection connection, int timeout)
        {
            DateTime dateTime = DateTime.Now.AddSeconds(timeout);
            while (!connection.IsOpen && DateTime.Now < dateTime)
            {
                Thread.Sleep(250);
            }
        }

        //
        // Summary:
        //     Blocks current thread until the channel has recovered or the specified timeout
        //     period has elapsed.
        //
        // Parameters:
        //   channel:
        //     The channel to wait for.
        //
        //   timeout:
        //     The timeout period in seconds.
        public static void WaitForAutoRecovery(this IModel channel, int timeout)
        {
            DateTime dateTime = DateTime.Now.AddSeconds(timeout);
            while (channel.IsClosed && DateTime.Now < dateTime)
            {
                Thread.Sleep(250);
            }
        }

        //
        // Summary:
        //     Blocks current thread until the channel has recovered or the specified timeout
        //     period has elapsed.
        //
        // Parameters:
        //   channel:
        //     The channel to wait for.
        //
        //   timeout:
        //     The timeout period in seconds.
        public static void WaitForAutoRecovery(this IModel channel, TimeSpan timeout)
        {
            DateTime dateTime = DateTime.Now.Add(timeout);
            while (channel.IsClosed && DateTime.Now < dateTime)
            {
                Thread.Sleep(250);
            }
        }
    }
}
