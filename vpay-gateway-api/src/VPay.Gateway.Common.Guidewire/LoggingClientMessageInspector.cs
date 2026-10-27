using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace VPay.Gateway.Common.Guidewire;

public partial class LoggingClientMessageInspector : IClientMessageInspector
{
    private readonly ILogger<LoggingClientMessageInspector> _logger;

    public LoggingClientMessageInspector(ILogger<LoggingClientMessageInspector> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets called AFTER receiving a reply from the SOAP Call.
    /// </summary>
    /// <param name="reply">The reply message.</param>
    /// <param name="correlationState">Correlation State.</param>
    public void AfterReceiveReply(ref Message reply, object correlationState)
    {
        Log.ResponseStatus(_logger, reply.State);
        using MessageBuffer buffer = reply.CreateBufferedCopy(int.MaxValue);
        XmlDocument document = GetDocument(buffer.CreateMessage());
        Log.Response(_logger, document.OuterXml);
        reply = buffer.CreateMessage();
    }

    /// <summary>
    /// Gets called BEFORE making the SOAP call.
    /// </summary>
    /// <param name="request">The request message to be sent.</param>
    /// <param name="channel">The client channel.</param>
    /// <returns>The object that is returned as the correlationState argument of the <see cref="IClientMessageInspector.AfterReceiveReply(ref Message, object)"/> method.</returns>
    public object BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        Log.RequestStatus(_logger, request.State);

        using MessageBuffer buffer = request.CreateBufferedCopy(int.MaxValue);
        XmlDocument document = GetDocument(buffer.CreateMessage());

        // The request contains the authentication data (username and password).
        // We'll clone the document and mask out the password on that cloned document
        // so we don't modify the original request, then log the document containing the
        // masked value
        LogRequest(_logger, (XmlDocument)document.Clone());

        request = buffer.CreateMessage();
        return null!;

        static void LogRequest(ILogger logger, XmlDocument clonedDocument)
        {
            var passwordNode = clonedDocument.GetElementsByTagName("password").OfType<XmlNode>().FirstOrDefault();

            // if we can't find the password node, let's default to *not* logging so we don't accidentally log
            // authentication details
            if (passwordNode is null || string.IsNullOrWhiteSpace(passwordNode.InnerText))
            {
                return;
            }

            // mask the password from the cloned document
            passwordNode.InnerText = new string('*', passwordNode.InnerText.Length);
            Log.Request(logger, clonedDocument.OuterXml);
        }
    }

    private static XmlDocument GetDocument(Message request)
    {
        XmlDocument document = new();
        using MemoryStream memoryStream = new();
        // write request to memory stream
        using (XmlWriter writer = XmlWriter.Create(memoryStream))
        {
            request.WriteMessage(writer);
            writer.Flush();
        }

        memoryStream.Position = 0;

        // load memory stream into a document
        document.Load(memoryStream);

        return document;
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Trace,
            Message = "Request state: {Status}")]
        public static partial void RequestStatus(ILogger logger, MessageState status);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Information,
            Message = "Request: {RequestBody}")]
        public static partial void Request(ILogger logger, string requestBody);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Trace,
            Message = "Response state: {Status}")]
        public static partial void ResponseStatus(ILogger logger, MessageState status);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Information,
            Message = "Response: {ResponseBody}")]
        public static partial void Response(ILogger logger, string responseBody);
    }
}
