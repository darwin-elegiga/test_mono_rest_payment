namespace VPay.DocSys.Parsing.Http.Tests
{
    public class MockDelegatingHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _respond;

        public MockDelegatingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> setup)
        {
            _respond = setup;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _respond(request);
        }
    }
}
