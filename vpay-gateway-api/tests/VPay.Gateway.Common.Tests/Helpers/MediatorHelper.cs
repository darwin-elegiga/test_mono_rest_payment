using System.Net;
using MediatR;
using Moq;
using Refit;

namespace VPay.Gateway.Common.Tests.Helpers;

public static class MediatorHelper
{
    public static IApiResponse<T> FakeApiResponse<T>(this T content, HttpStatusCode statusCode, ApiException apiExceptionResponse = null)
    {
        return content.MockApiResponse(statusCode, apiExceptionResponse).Object;
    }

    public static Mock<IApiResponse<T>> MockApiResponse<T>(this T content, HttpStatusCode statusCode, ApiException apiExceptionResponse = null)
    {
        var isSuccessStatusCode = (int)statusCode >= 200 && (int)statusCode < 300;
        var mock = new Mock<IApiResponse<T>>();
        mock.Setup(x => x.StatusCode).Returns(statusCode);
        mock.Setup(x => x.IsSuccessStatusCode).Returns(isSuccessStatusCode);
        mock.Setup(x => x.Content).Returns(content);

        if (!isSuccessStatusCode && apiExceptionResponse != null)
        {
            mock.Setup(x => x.Error).Returns(apiExceptionResponse);
        }

        return mock;
    }
}

public class MockHelper<TQuery, TResponse>
{
    public void BuildMock(Mock<IMediator> mediator, TResponse response)
    {
        mediator.Setup(x => x.Send(It.IsAny<TQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}
