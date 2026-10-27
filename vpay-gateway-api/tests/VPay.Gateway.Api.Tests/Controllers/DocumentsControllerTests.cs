using System.Diagnostics.CodeAnalysis;
using System.Net;
using AutoMapper;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Caching;
using Polly.Caching.Distributed;
using Polly.Registry;
using Refit;
using Vpay.Gateway.Common.Vpay.Models;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Api.Caching;
using VPay.Gateway.Api.Controllers;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Application;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Common.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Paging;
using VPay.Gateway.Data.Sql.Transactions;
using Xunit;

namespace VPay.Gateway.Api.Tests.Controllers;

public class DocumentsControllerTests : IDisposable
{
    private readonly Mock<IMediator> _mockMediator = new(MockBehavior.Strict);
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentsController> _logger = new NullLogger<DocumentsController>();
    private readonly IDistributedCache _cache;
    private readonly DocumentsController _handler;
    private readonly Mock<IUserInfo> _userInfo;
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory();

    public DocumentsControllerTests()
    {
        var profiles = from type in typeof(Startup).Assembly.GetTypes()
                       where typeof(Profile).IsAssignableFrom(type)
                       select type;

        var configuration = new MapperConfiguration(cfg =>
        {
            foreach (var profileType in profiles)
            {
                cfg.AddProfile(profileType);
            }
        },_loggerFactory);
        _mapper = configuration.CreateMapper();

        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        var registry = new PolicyRegistry
            {
                {
                    CachingPolicies.TransactionIdCachePolicy,
                    Policy.CacheAsync(_cache.AsAsyncCacheProvider<string>().WithSerializer(new LongCacheSerializer()), TimeSpan.FromHours(1))
                }
            };
        _userInfo = new Mock<IUserInfo>();
        _handler = new DocumentsController(_mockMediator.Object, _mapper, registry, _logger, _userInfo.Object);
    }

    ~DocumentsControllerTests() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => _mockMediator.VerifyAll();

    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public class Constructor : DocumentsControllerTests
    {
#pragma warning disable CA1806

        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException() =>
            new Action(() => new DocumentsController(null, _mapper, Mock.Of<IReadOnlyPolicyRegistry<string>>(), _logger, _userInfo.Object))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("mediator");

        [Fact]
        public void WhenMapperIsNull_ThrowsArgumentNullException() =>
            new Action(() => new DocumentsController(_mockMediator.Object, null, Mock.Of<IReadOnlyPolicyRegistry<string>>(), _logger, _userInfo.Object))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("mapper");

        [Fact]
        public void WhenPolicyRegistryIsNull_ThrowsArgumentNullException() =>
            new Action(() => new DocumentsController(_mockMediator.Object, _mapper, null, _logger, _userInfo.Object))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("policyRegistry");

        [Fact]
        public void WhenLoggerIsNull_ThrowsArgumentNullException() =>
            new Action(() => new DocumentsController(_mockMediator.Object, _mapper, Mock.Of<IReadOnlyPolicyRegistry<string>>(), null, _userInfo.Object))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");

#pragma warning restore CA1806
    }

    public class GetDocumentsList : DocumentsControllerTests
    {
        private const long TransactionId = 100;

        [Fact]
        public async Task WhenGetDocumentsReturnsNull_ThenShouldReturnNotFound()
        {
            _mockMediator.Setup(x => x.Send(It.Is<GetDocumentsByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync((IReadOnlyCollection<ArchiveDetail>)null)
                .Verifiable();

            var act = await _handler.GetDocumentsList(TransactionId, default);
            act.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task WhenGetDocumentsReturnsList_ThenShouldReturnListMappedToDtos()
        {
            var docs = new List<ArchiveDetail> {
                    new ArchiveDetail
                    {
                        ArchiveDetailId = 1
                    },
                    new ArchiveDetail
                    {
                        ArchiveDetailId = 103
                    }
                    };

            var expectedResults = _mapper.Map<List<DocumentDto>>(docs);

            _mockMediator.Setup(x => x.Send(It.Is<GetDocumentsByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(docs)
                .Verifiable();

            var act = await _handler.GetDocumentsList(TransactionId, default);
            act.Value.Should().BeEquivalentTo(expectedResults);
        }

        [Fact]
        public async Task WhenGetDocumentsThrowsUnauthorizedException_ThenShouldReturnNotFound()
        {
            _mockMediator.Setup(x => x.Send(It.Is<GetDocumentsByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ThrowsAsync(new NotAuthorizedException())
                .Verifiable();

            var act = await _handler.GetDocumentsList(TransactionId, default);
            act.Result.Should().BeOfType<NotFoundResult>();
        }
    }

    public class GetDocument : DocumentsControllerTests
    {
        private const long TransactionId = 100;
        private const long DocumentId = 989;

        [Fact]
        public async Task WhenDownloadDocumentReturnsNull_ThenShouldReturnNotFound()
        {
            _mockMediator.Setup(x => x.Send(It.Is<DownloadDocumentByIdQuery>(q => q.TransactionId == TransactionId && q.DocumentId == DocumentId), default))
                .ReturnsAsync((Document)null)
                .Verifiable();

            var act = await _handler.GetDocument(TransactionId, DocumentId, default);
            act.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task WhenDownloadDocumentThrowsUnauthorizedException_ThenShouldReturnNotFound()
        {
            _mockMediator.Setup(x => x.Send(It.Is<DownloadDocumentByIdQuery>(q => q.TransactionId == TransactionId && q.DocumentId == DocumentId), default))
                .ThrowsAsync(new NotAuthorizedException())
                .Verifiable();

            var act = await _handler.GetDocument(TransactionId, DocumentId, default);
            act.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task WhenDownloadDocumentThrowsDocumentNotInArchiveException_ThenShouldReturnNotFound()
        {
            _mockMediator.Setup(x => x.Send(It.Is<DownloadDocumentByIdQuery>(q => q.TransactionId == TransactionId && q.DocumentId == DocumentId), default))
                .ThrowsAsync(new DocumentNotInArchiveException())
                .Verifiable();

            var act = await _handler.GetDocument(TransactionId, DocumentId, default);
            act.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task WhenGetDocumentsReturnsADocument_ThenShouldReturnFileWithDocumentDetails()
        {
            await using var memoryStream = new MemoryStream();
            var file = new Document(memoryStream, "text/plain", "value.txt");

            _mockMediator.Setup(x => x.Send(It.Is<DownloadDocumentByIdQuery>(q => q.TransactionId == TransactionId && q.DocumentId == DocumentId), default))
                .ReturnsAsync(file)
                .Verifiable();

            var act = await _handler.GetDocument(TransactionId, DocumentId, default);

            using (new AssertionScope())
            {
                var v = act.Should().BeOfType<FileStreamResult>()
                    .Which;

                v.FileDownloadName.Should().Be(file.FileName);
                v.ContentType.Should().Be(file.ContentType);
                v.FileStream.Should().BeSameAs(file.Stream);
            }
        }
    }

    public class GetPraDocument : DocumentsControllerTests
    {
        private const long TransactionId = 100;
        private const string PaymentId = "ABC1234";
        private const string PayerId = "VPAY5";
        private const string SubPayerId = "123-abc";
        private const string OriginCode = "SEMED";
        private const string ClientCode = "ABC";
        private const string BillingEntityCode = "ABC0000001";
        private const string PrfClientGroup = "VHSA5";
        private const string ConfigurationName = "PREFIX_USERKEY";
        private const string NonPrsErrorMsg = "When the payerId is VPAY5 or is not enabled for PRS, the paymentId should be a numeric TransactionId.";

        [Fact]
        public async Task WhenPayerIsNotConfiguredForPrs_AndTransactionNotNumeric_ShouldReturnBadRequest()
        {
            var act = await _handler.GetPraDocument(PayerId, SubPayerId, "123ABC", default);
            act.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<SerializableError>()
                .Which.Should().ContainKey("paymentId")
                .WhoseValue.Should().BeAssignableTo<IEnumerable<string>>()
                .Which.Should().Contain(NonPrsErrorMsg);
        }

        [Fact]
        public async Task WhenPayerIsNotEnabledForPrs_AndTransactionNotNumeric_ShouldReturnBadRequest()
        {
            SetupPrsConfiguration(false, "USHA1", "12345678");

            var act = await _handler.GetPraDocument("USHA1", "12345678", "123ABC", default);
            act.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<SerializableError>()
                .Which.Should().ContainKey("paymentId")
                .WhoseValue.Should().BeAssignableTo<IEnumerable<string>>()
                .Which.Should().Contain(NonPrsErrorMsg);
        }

        [Fact]
        public async Task WhenPayerIsEnabledForPrs_AndOptumSubPayerCannotBeFound_ShouldReturnBadRequest()
        {
            SetupPrsConfiguration(true, "UMR01", SubPayerId);

            _mockMediator
                .Setup(x => x.Send(It.Is<PaymentNumberToTransactionId.Query>(q =>
                    q.SubPayerId == SubPayerId && q.PaymentId == PaymentId), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ItemNotFoundException("Sub-payer cannot be found."))
                .Verifiable();

            var act = await _handler.GetPraDocument("UMR01", SubPayerId, PaymentId, default);
            act.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be("Sub-payer cannot be found.");
        }

        [Fact]
        public async Task WhenPayerIsEnabledForPrs_AndPaymentInformationCannotBeFound_ShouldReturnBadRequest()
        {
            SetupPrsConfiguration(true, "UMR01", SubPayerId);

            _mockMediator
                .Setup(x => x.Send(It.Is<PaymentNumberToTransactionId.Query>(q =>
                    q.SubPayerId == SubPayerId && q.PaymentId == PaymentId), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ItemNotFoundException("Could not find payment for the sub-payer."));

            var act = await _handler.GetPraDocument("UMR01", SubPayerId, PaymentId, default);
            act.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be("Could not find payment for the sub-payer.");
        }

        [Fact]
        public async Task WhenPayerIsEnabledForPrs_AndAssociatedTransactionFound_ShouldDownloadPraFile()
        {
            SetupPrsConfiguration(true, "UMR01", SubPayerId);

            _mockMediator
                .Setup(x => x.Send(It.Is<PaymentNumberToTransactionId.Query>(q =>
                    q.SubPayerId == SubPayerId && q.PaymentId == PaymentId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentNumberToTransactionId.Response(ClientCode, "BE", TransactionId))
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync((Document)null)
                .Verifiable();

            var act = await _handler.GetPraDocument("UMR01", SubPayerId, PaymentId, default);
            using (new AssertionScope())
            {
                act.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().Be("TRANSACTION_NOT_FOUND");

                string cacheKey = $"TXID:{SubPayerId}:{PaymentId}";
                (await _cache.GetStringAsync(cacheKey)).Should().Be(TransactionId.ToString());
            }
        }

        [Fact]
        public async Task WhenPayerIsEnabledForPrs_AndAssociatedTransactionFoundInCache_ShouldDownloadPraFile()
        {
            string cacheKey = $"TXID:{SubPayerId}:{PaymentId}";
            await _cache.SetStringAsync(cacheKey, TransactionId.ToString());

            SetupPrsConfiguration(true, "UMR01", SubPayerId);

            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync((Document)null)
                .Verifiable();

            var act = await _handler.GetPraDocument("UMR01", SubPayerId, PaymentId, default);
            act.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().Be("TRANSACTION_NOT_FOUND");
        }

        [Fact]
        public async Task WhenDownloadDocumentReturnsNull_ThenShouldReturnNotFound()
        {
            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync((Document)null)
                .Verifiable();

            var act = await _handler.GetPraDocument("VPAY5", SubPayerId, TransactionId.ToString(), default);
            act.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().Be("TRANSACTION_NOT_FOUND");
        }

        [Fact]
        public async Task WhenDownloadDocumentThrowsUnauthorizedException_ThenShouldReturnNotFound()
        {
            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ThrowsAsync(new NotAuthorizedException())
                .Verifiable();

            var act = await _handler.GetPraDocument("VPAY5", SubPayerId, TransactionId.ToString(), default);
            act.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().Be("TRANSACTION_NOT_FOUND");
        }

        [Fact]
        public async Task WhenDownloadDocumentThrowsItemNotFoundExceptionException_ThenShouldReturnNotFound()
        {
            var exception = new ItemNotFoundException("PRA_REQUESTED");

            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ThrowsAsync(exception)
                .Verifiable();

            var act = await _handler.GetPraDocument("VPAY5", SubPayerId, TransactionId.ToString(), default);
            act.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().Be(exception.Message);
        }

        [Theory]
        [InlineData(false, "ADN", "ADN0001001", "VPAY5", "", "VPAY5")]
        [InlineData(false, "NOV", "NOV0001001", "VPAY5", "", "VHSA5")]
        [InlineData(false, "USH", "USH0001001", "USHA1", "000020943", "VPUSH")]
        [InlineData(true, "UMR", "UMRPF01001", "UMR01", "886000028", "OUT835")]
        [InlineData(true, "HSO", "HSOPF01001", "40026", "381490038", "UMHS5")]
        [InlineData(true, "MHP", "MHP0001001", "MCLRN", "272204037", "MCLN5")]
        public async Task WhenGetDocumentsReturnsADocument_ThenShouldReturnFileWithDocumentDetails(bool isPrsEnabled, string clientCode, string beCode, string payerId, string subPayerId, string clientGroup)
        {
            await using var memoryStream = new MemoryStream();
            var file = new Document(memoryStream, "application/pdf", "value.pdf");

            if (payerId != "VPAY5")
            {
                OptumPrsConfigurationDto prsConfigDto = new()
                {
                    OriginCode = OriginCode,
                    ClientCode = clientCode,
                    BillingEntityCode = beCode,
                    PrfClientGroup = clientGroup,
                    IsPrsEnabled = isPrsEnabled,
                };

                _mockMediator.Setup(x => x.Send(It.Is<GetOptumPrsConfiguration.Query>(q => q.PayerId == payerId && q.SubPayerId == subPayerId), default))
                    .ReturnsAsync(prsConfigDto)
                    .Verifiable();

                if (isPrsEnabled)
                {
                    SetupClientBoardingConfiguration(ConfigurationName, OriginCode, clientCode, "ACTIVE", "N");
                }
            }

            if (isPrsEnabled)
            {
                _mockMediator
                    .Setup(x => x.Send(It.Is<PaymentNumberToTransactionId.Query>(q =>
                        q.SubPayerId == subPayerId && q.PaymentId == PaymentId), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PaymentNumberToTransactionId.Response(clientCode, beCode, TransactionId))
                    .Verifiable();
            }

            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(file)
                .Verifiable();

            ActionResult act;
            if (isPrsEnabled)
            {
                act = await _handler.GetPraDocument(payerId, subPayerId, PaymentId, default);
            }
            else
            {
                act = await _handler.GetPraDocument(payerId, subPayerId, TransactionId.ToString(), default);
            }

            using (new AssertionScope())
            {
                var v = act.Should().BeOfType<FileStreamResult>()
                    .Which;

                v.FileDownloadName.Should().Be(file.FileName);
                v.ContentType.Should().Be(file.ContentType);
                v.FileStream.Should().BeSameAs(file.Stream);
            }
        }

        [Fact]
        public async Task WhenPayerIsNotConfiguredForPrs_AndTransactionIsNumeric_ShouldReturnDocument()
        {
            string payerId = "USHA1";
            string subPayerId = "000020943";

            await using var memoryStream = new MemoryStream();
            var file = new Document(memoryStream, "application/pdf", "value.pdf");

            _mockMediator.Setup(x => x.Send(It.Is<GetOptumPrsConfiguration.Query>(q => q.PayerId == payerId && q.SubPayerId == subPayerId), default))
                .ReturnsAsync((OptumPrsConfigurationDto)null)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(file)
                .Verifiable();

            var act = await _handler.GetPraDocument(payerId, subPayerId, TransactionId.ToString(), default);

            using (new AssertionScope())
            {
                var v = act.Should().BeOfType<FileStreamResult>()
                    .Which;

                v.FileDownloadName.Should().Be(file.FileName);
                v.ContentType.Should().Be(file.ContentType);
                v.FileStream.Should().BeSameAs(file.Stream);
            }
        }

        [Fact]
        public async Task WhenPayerIsNotEnabledForPrs_AndTransactionIsNumeric_ShouldReturnDocument()
        {
            string clientCode = "USH";
            string beCode = "USH0001001";
            string payerId = "USHA1";
            string subPayerId = "000020943";
            string clientGroup = "VPUSH";

            await using var memoryStream = new MemoryStream();
            var file = new Document(memoryStream, "application/pdf", "value.pdf");

            OptumPrsConfigurationDto prsConfigDto = new()
            {
                ClientCode = clientCode,
                BillingEntityCode = beCode,
                PrfClientGroup = clientGroup,
                IsPrsEnabled = false,
            };

            _mockMediator.Setup(x => x.Send(It.Is<GetOptumPrsConfiguration.Query>(q => q.PayerId == payerId && q.SubPayerId == subPayerId), default))
                .ReturnsAsync(prsConfigDto)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(file)
                .Verifiable();

            var act = await _handler.GetPraDocument(payerId, subPayerId, TransactionId.ToString(), default);

            using (new AssertionScope())
            {
                var v = act.Should().BeOfType<FileStreamResult>()
                    .Which;

                v.FileDownloadName.Should().Be(file.FileName);
                v.ContentType.Should().Be(file.ContentType);
                v.FileStream.Should().BeSameAs(file.Stream);
            }
        }

        [Fact]
        public async Task WhenPayerIsEnabledForPrs_AndPaymentNumberIsValid_ShouldReturnDocument()
        {
            string clientCode = "USH";
            string beCode = "USH0001001";
            string payerId = "USHA1";
            string subPayerId = "000020943";
            string clientGroup = "VPUSH";

            await using var memoryStream = new MemoryStream();
            var file = new Document(memoryStream, "application/pdf", "value.pdf");

            OptumPrsConfigurationDto prsConfigDto = new()
            {
                OriginCode = OriginCode,
                ClientCode = clientCode,
                BillingEntityCode = beCode,
                PrfClientGroup = clientGroup,
                IsPrsEnabled = true,
            };

            _mockMediator.Setup(x => x.Send(It.Is<GetOptumPrsConfiguration.Query>(q
                => q.PayerId == payerId && q.SubPayerId == subPayerId), default))
                .ReturnsAsync(prsConfigDto)
                .Verifiable();

            SetupClientBoardingConfiguration(ConfigurationName, OriginCode, clientCode, "ACTIVE", "N");

            _mockMediator
                .Setup(x => x.Send(It.Is<PaymentNumberToTransactionId.Query>(q =>
                    q.SubPayerId == subPayerId && q.PaymentId == PaymentId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentNumberToTransactionId.Response(clientCode, beCode, TransactionId))
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(file)
                .Verifiable();

            var act = await _handler.GetPraDocument(payerId, subPayerId, PaymentId, default);

            using (new AssertionScope())
            {
                var v = act.Should().BeOfType<FileStreamResult>()
                    .Which;

                v.FileDownloadName.Should().Be(file.FileName);
                v.ContentType.Should().Be(file.ContentType);
                v.FileStream.Should().BeSameAs(file.Stream);
            }
        }

        [Fact]
        public async Task WhenPayerIsEnabledForPrs_AndPaymentNumberHasPrefix_ShouldReturnDocument()
        {
            string clientCode = "MHP";
            string beCode = "MHP0001001";
            string payerId = "MCLRN";
            string subPayerId = "111383252";
            string clientGroup = "MCLN5";
            string paymentId = $"{subPayerId}_{PaymentId}";

            await using var memoryStream = new MemoryStream();
            var file = new Document(memoryStream, "application/pdf", "value.pdf");

            OptumPrsConfigurationDto prsConfigDto = new()
            {
                OriginCode = OriginCode,
                ClientCode = clientCode,
                BillingEntityCode = beCode,
                PrfClientGroup = clientGroup,
                IsPrsEnabled = true,
            };

            _mockMediator.Setup(x => x.Send(It.Is<GetOptumPrsConfiguration.Query>(q
                => q.PayerId == payerId && q.SubPayerId == subPayerId), default))
                .ReturnsAsync(prsConfigDto)
                .Verifiable();

            SetupClientBoardingConfiguration(ConfigurationName, OriginCode, clientCode, "ACTIVE", "Y");

            _mockMediator
                .Setup(x => x.Send(It.Is<PaymentNumberToTransactionId.Query>(q =>
                    q.SubPayerId == subPayerId && q.PaymentId == paymentId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentNumberToTransactionId.Response(clientCode, beCode, TransactionId))
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(file)
                .Verifiable();

            var act = await _handler.GetPraDocument(payerId, subPayerId, PaymentId, default);

            using (new AssertionScope())
            {
                var v = act.Should().BeOfType<FileStreamResult>()
                    .Which;

                v.FileDownloadName.Should().Be(file.FileName);
                v.ContentType.Should().Be(file.ContentType);
                v.FileStream.Should().BeSameAs(file.Stream);
            }
        }

        [Fact]
        public async Task WhenPayerIsEnabledForPrs_AndPaymentNumberHasNoPrefix_ShouldReturnDocument()
        {
            string clientCode = "USH";
            string beCode = "USH0001001";
            string payerId = "USHA1";
            string subPayerId = "000020943";
            string clientGroup = "VPUSH";
            string paymentId = PaymentId;

            await using var memoryStream = new MemoryStream();
            var file = new Document(memoryStream, "application/pdf", "value.pdf");

            OptumPrsConfigurationDto prsConfigDto = new()
            {
                OriginCode = OriginCode,
                ClientCode = clientCode,
                BillingEntityCode = beCode,
                PrfClientGroup = clientGroup,
                IsPrsEnabled = true,
            };

            _mockMediator.Setup(x => x.Send(It.Is<GetOptumPrsConfiguration.Query>(q
                => q.PayerId == payerId && q.SubPayerId == subPayerId), default))
                .ReturnsAsync(prsConfigDto)
                .Verifiable();

            SetupClientBoardingConfiguration(ConfigurationName, OriginCode, clientCode, "ACTIVE", "N");

            _mockMediator
                .Setup(x => x.Send(It.Is<PaymentNumberToTransactionId.Query>(q =>
                    q.SubPayerId == subPayerId && q.PaymentId == paymentId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentNumberToTransactionId.Response(clientCode, beCode, TransactionId))
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<DownloadPraFileByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(file)
                .Verifiable();

            var act = await _handler.GetPraDocument(payerId, subPayerId, PaymentId, default);

            using (new AssertionScope())
            {
                var v = act.Should().BeOfType<FileStreamResult>()
                    .Which;

                v.FileDownloadName.Should().Be(file.FileName);
                v.ContentType.Should().Be(file.ContentType);
                v.FileStream.Should().BeSameAs(file.Stream);
            }
        }

        private void SetupPrsConfiguration(bool isPrsEnabled, string payerId, string subPayerId)
        {
            OptumPrsConfigurationDto prsConfigDto = new()
            {
                OriginCode = OriginCode,
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode,
                PrfClientGroup = PrfClientGroup,
                IsPrsEnabled = isPrsEnabled,
            };

            _mockMediator.Setup(x => x.Send(It.Is<GetOptumPrsConfiguration.Query>(q
                => q.PayerId == payerId && q.SubPayerId == subPayerId), default))
                .ReturnsAsync(prsConfigDto)
                .Verifiable();

            if (isPrsEnabled)
            {
                SetupClientBoardingConfiguration(ConfigurationName, OriginCode, ClientCode, "ACTIVE", "N");
            }
        }

        private void SetupClientBoardingConfiguration(string configurationName, string originCode, string clientCode, string status, string enabled)
        {
            ClientConfigurationDto clientConfigDto = new()
            {
                OriginCode = originCode,
                ClientCode = clientCode,
                Status = status,
                Data1 = enabled
            };

            _mockMediator.Setup(x => x.Send(It.Is<GetClientBoardingConfiguration.Query>(q
                => q.ConfigurationName == configurationName
                && q.OriginCode == originCode
                && q.ClientCode == clientCode), default))
                .ReturnsAsync(clientConfigDto)
                .Verifiable();
        }
    }

    public class GetTransactionIds : DocumentsControllerTests
    {
        private const string PaymentId = "ABC1234";
        private DateTime StartDate = DateTime.Today.AddDays(-30);
        private DateTime EndDate = DateTime.Now;
        private const string ProviderTin = "88888888";

        [Fact]
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            var request = SetUpRequest(PaymentId, StartDate, EndDate, ProviderTin);
            var apiException = ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.BadRequest), new RefitSettings()).Result;

            _mockMediator
            .Setup(x => x.Send(It.Is<GetTransactionsByDateRangeQuery>(q =>
            q.StartDate == StartDate && q.EndDate == EndDate && q.PaymentId == PaymentId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(apiException)
            .Verifiable();

            var result = await _handler.GetTransactionIds(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
            }
        }

        [Fact]
        public async Task WhenPaymentId_StartDate_EndDateFound_ShouldReturnSuccess()
        {
            var request = SetUpRequest(PaymentId, StartDate, EndDate, ProviderTin);

            List<TransactionDetails> items = new();
            PagedResult<TransactionDetails> expectedResult = new(items, request.Page.Value, request.PageSize.Value, items.Count);
            _mockMediator
                .Setup(x => x.Send(It.Is<GetTransactionsByDateRangeQuery>(q =>
                    q.StartDate == StartDate && q.EndDate == EndDate && q.PaymentId == PaymentId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult)
                .Verifiable();

            var result = await _handler.GetTransactionIds(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().BeOfType<OkObjectResult>();
            }
        }

        [Fact]
        public async Task WhenPaymentId_StartDate_EndDate_ProviderTinFound_ShouldReturnSuccess()
        {
            var request = SetUpRequest(PaymentId, StartDate, EndDate, ProviderTin);

            List<TransactionDetails> items = new();
            PagedResult<TransactionDetails> expectedResult = new(items, request.Page.Value, request.PageSize.Value, items.Count);
            _mockMediator
                .Setup(x => x.Send(It.Is<GetTransactionsByDateRangeQuery>(q =>
                    q.StartDate == StartDate && q.EndDate == EndDate && q.PaymentId == PaymentId && q.TaxId == ProviderTin), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult)
                .Verifiable();

            var result = await _handler.GetTransactionIds(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().BeOfType<OkObjectResult>();
            }
        }

        private static TransactionSearchDto SetUpRequest(string paymentId, DateTime startDate, DateTime endDate, string ProviderTin)
        {
            return new TransactionSearchDto
            {
                PaymentId = paymentId,
                StartDate = startDate,
                EndDate = endDate,
                ProviderTin = ProviderTin,
                Page = 1,
                PageSize = 1
            };
        }

        [Theory]
        [InlineData(1, 1, 1, 1)]
        [InlineData(10, 10, 1, 10)]
        [InlineData(15, 5, 2, 10)]
        [InlineData(15, 0, 3, 10)]
        public async Task WhenRequestMatchesItems_ReturnsItemList(int totalRows, int itemCount, int page, int pageSize)
        {
            var request = SetUpRequest(PaymentId, StartDate, EndDate, ProviderTin);

            var response = MockTransactionDetails(totalRows, itemCount, page, pageSize);

            _mockMediator
                .Setup(x => x.Send(It.IsAny<GetTransactionsByDateRangeQuery>(), default))
                .ReturnsAsync(response)
                .Verifiable();

            var result = await _handler.GetTransactionIds(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<PagedResult<TransactionDetails>>();

                var itemList = response.Items;
                var dtoList = okResult.Value as PagedResult<TransactionDetails>;
                dtoList.Should().NotBeNull();
                dtoList.CurrentPage.Should().Be(page);
                dtoList.PageSize.Should().Be(pageSize);
                dtoList.TotalRows.Should().Be(totalRows);
                dtoList.Items.Should().NotBeNull();
                dtoList.Items.Count().Should().Be(itemCount);
                for (int i = 0; i < dtoList.Items.Count(); i++)
                {
                    var dto = dtoList.Items.ToList();
                    var items = response.Items.ToList();
                    dto[i].PaymentId.Should().Be(items[i].PaymentId);
                    dto[i].PaymentDate.Should().Be(items[i].PaymentDate);
                    dto[i].PaymentType.Should().Be(items[i].PaymentType);
                    dto[i].ProviderTin.Should().Be(items[i].ProviderTin);
                    dto[i].VpayTransactionId.Should().Be(items[i].VpayTransactionId);
                }
            }
        }

        private static PagedResult<TransactionDetails> MockTransactionDetails(int totalRows, int itemCount, int page, int pageSize)
        {
            var pageModel = new PageModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalRows = totalRows
            };

            var items = new List<TransactionDetails>();

            for (int i = 0; i < itemCount; i++)
            {
                items.Add(new TransactionDetails
                {
                    VpayTransactionId = i,
                    PaymentType = "CLVCP",
                    PaymentDate = DateTime.Now,
                    PaymentId = "1234",
                    ProviderTin = "12345678"
                });
            }

            return new PagedResult<TransactionDetails>(items, pageModel);
        }
    }

    public class GetDocumentsListByProviderTinAndPaymentId : DocumentsControllerTests
    {
        private const long TransactionId = 100;

        [Fact]
        public async Task WhenGetDocumentsByProviderTinAndPaymentIdReturnsNull_ThenShouldReturnNotFound()
        {
            _mockMediator.Setup(x => x.Send(It.Is<GetDocumentsByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync((IReadOnlyCollection<ArchiveDetail>)null)
                .Verifiable();

            var act = await _handler.GetDocumentsList(TransactionId, default);
            act.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task WhenGetDocumentsByProviderTinAndPaymentIdReturnsList_ThenShouldReturnListMappedToDtos()
        {
            var docs = new List<ArchiveDetail> {
                    new ArchiveDetail
                    {
                        ArchiveDetailId = 1
                    },
                    new ArchiveDetail
                    {
                        ArchiveDetailId = 103
                    }
                    };

            var expectedResults = _mapper.Map<List<DocumentDto>>(docs);

            _mockMediator.Setup(x => x.Send(It.Is<GetDocumentsByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(docs)
                .Verifiable();

            var act = await _handler.GetDocumentsList(TransactionId, default);
            act.Value.Should().BeEquivalentTo(expectedResults);
        }

        [Fact]
        public async Task WhenGetDocumentsByProviderTinAndPaymentIdThrowsUnauthorizedException_ThenShouldReturnNotFound()
        {
            _mockMediator.Setup(x => x.Send(It.Is<GetDocumentsByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ThrowsAsync(new NotAuthorizedException())
                .Verifiable();

            var act = await _handler.GetDocumentsList(TransactionId, default);
            act.Result.Should().BeOfType<NotFoundResult>();
        }
    }

    public class GetTransactionIdsbyTrn02 : DocumentsControllerTests
    {
        private const string Trn02 = "TRN02";
        private DateTime StartDate = DateTime.Today.AddDays(-30);
        private DateTime EndDate = DateTime.Now;
        private const string ProviderTin = "88888888";

        [Fact]
        public async Task WhenTransactionsNotFound_ReturnsNotFound()
        {
            var request = SetUpTrn02Request(Trn02, StartDate, EndDate, ProviderTin);
            _mockMediator
                .Setup(x => x.Send(It.Is<GetTransactionsByTrn02.Query>(q =>
                    q.StartDate == StartDate && q.EndDate == EndDate && q.Trn02 == Trn02 && q.ProviderTin == ProviderTin), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<TransactionsTrn02Details>(Enumerable.Empty<TransactionsTrn02Details>(), 1, 1, 0))
                .Verifiable();
            var result = await _handler.GetTransactionIdsByTRN02(request, CancellationToken.None);
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().BeOfType<NotFoundResult>();
            }
        }

        [Fact]
        public async Task WhenTrn02_StartDate_EndDate_ProviderTinFound_ShouldReturnSuccess()

        {
            // Arrange
            var request = SetUpTrn02Request(Trn02, StartDate, EndDate, ProviderTin);
            var items = new List<TransactionsTrn02Details>
            {
                new TransactionsTrn02Details
                {
                 VpayTransactionId = 1,
                 PaymentId = "1234",
                 PaymentType = "CLVCP",
                 PaymentDate = DateTime.Now,
                 ProviderTin = ProviderTin,
                 Trn02 = Trn02,
                 ClientID = "Client1",
                 BillingEntity = "Entity1"
                }
            };

            var pagedResult = new PagedResult<TransactionsTrn02Details>(items, 1, 1, items.Count);

            _mockMediator
                .Setup(x => x.Send(It.IsAny<GetTransactionsByTrn02.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult)
                .Verifiable();

            _userInfo.Setup(x => x.CanAccess(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act
            var result = await _handler.GetTransactionIdsByTRN02(request, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().BeOfType<OkObjectResult>();

                var okResult = result.Result as OkObjectResult;
                okResult.Should().NotBeNull();
                okResult!.Value.Should().BeOfType<Common.PlatformConfig.Pagination<TransactionsTrn02DetailsDto>>();

                var pagination = okResult.Value as Common.PlatformConfig.Pagination<TransactionsTrn02DetailsDto>;
                pagination.Should().NotBeNull();
                pagination!.Items.Should().HaveCount(1);

                var item = pagination.Items.First();
                item.VpayTransactionId.Should().Be(1);
                item.PaymentId.Should().Be("1234");
                item.PaymentType.Should().Be("CLVCP");
                //item.PaymentDate.Should().Be(DateTime.Now);
                item.ProviderTin.Should().Be(ProviderTin);
                item.Trn02.Should().Be(Trn02);
            }
        }

        [Fact]
        public async Task WhenInvalidDateRange_ReturnsBadRequest()
        {
            var request = SetUpTrn02Request(Trn02, EndDate, StartDate, ProviderTin); // EndDate before StartDate
            var result = await _handler.GetTransactionIdsByTRN02(request, CancellationToken.None);
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>();
            }
        }

        [Theory]
        [InlineData(1, 1, 1, 1)]
        public async Task WhenRequestMatchesItems_ReturnsItemList(int totalRows, int itemCount, int page, int pageSize)
        {
            // Arrange
            var request = SetUpTrn02Request(Trn02, StartDate, EndDate, ProviderTin);
            var response = MockTransactionTrn02Details(totalRows, itemCount, page, pageSize);
            var items = new List<TransactionsTrn02Details>
            { new TransactionsTrn02Details
                  {
                     VpayTransactionId = 1,
                     PaymentId = "1234",
                     PaymentType = "CLVCP",
                     PaymentDate = DateTime.Now,
                     ProviderTin = ProviderTin,
                     Trn02 = Trn02,
                     ClientID = "Client1",
                     BillingEntity = "Entity1"
                  }
            };

            var pagedResult = new PagedResult<TransactionsTrn02Details>(items, 1, 1, items.Count);

            _userInfo.Setup(x => x.CanAccess(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _mockMediator
                .Setup(x => x.Send(It.IsAny<GetTransactionsByTrn02.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult)
                .Verifiable();

            // Act
            var result = await _handler.GetTransactionIdsByTRN02(request);

            // Assert
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<Common.PlatformConfig.Pagination<TransactionsTrn02DetailsDto>>();
                var itemList = response.Items;
                var dtoList = okResult.Value as Common.PlatformConfig.Pagination<TransactionsTrn02DetailsDto>;
                //response.Should().NotBeNull();
                dtoList.CurrentPage.Should().Be(page);
                dtoList.PageSize.Should().Be(pageSize);
                dtoList.TotalRows.Should().Be(totalRows);
                dtoList.Items.Should().NotBeNull();
                dtoList.Items.Count.Should().Be(itemCount);
                for (int i = 0; i < dtoList.Items.Count; i++)
                {
                    var dto = dtoList.Items.ToList();
                    //  items = Items.ToList();
                    dto[i].PaymentId.Should().Be((string)items[i].PaymentId);
                    dto[i].PaymentDate.Should().Be((DateTime)items[i].PaymentDate);
                    dto[i].PaymentType.Should().Be((string)items[i].PaymentType);
                    dto[i].ProviderTin.Should().Be((string)items[i].ProviderTin);
                    dto[i].VpayTransactionId.Should().Be((long)items[i].VpayTransactionId);
                }
            }
        }

        private static Trn02SearchRequest SetUpTrn02Request(string trn02, DateTime startDate, DateTime endDate, string ProviderTin)
        {
            return new Trn02SearchRequest
            {
                Trn02 = trn02,
                StartDate = startDate,
                EndDate = endDate,
                ProviderTin = ProviderTin,
                Page = 1,
                PageSize = 1
            };
        }

        private static PagedResult<TransactionsTrn02DetailsDto> MockTransactionTrn02Details(int totalRows, int itemCount, int page, int pageSize)
        {
            var pageModel = new PageModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalRows = totalRows
            };

            var items = new List<TransactionsTrn02DetailsDto>();

            for (int i = 0; i < itemCount; i++)
            {
                items.Add(new TransactionsTrn02DetailsDto
                {
                    VpayTransactionId = i,
                    PaymentType = "CLVCP",
                    PaymentDate = DateTime.Now,
                    PaymentId = "1234",
                    ProviderTin = "12345678"
                });
            }

            return new PagedResult<TransactionsTrn02DetailsDto>(items, pageModel);
        }

        [Fact]
        public async Task GetTransactionIdsByTRN02_StartDateGreaterThanEndDate_ReturnsBadRequest()
        {
            // Arrange
            var request = new Trn02SearchRequest
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(-1),
                ProviderTin = "123456789",
                Trn02 = "TRN02",
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.GetTransactionIdsByTRN02(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("400", badRequestResult.StatusCode.ToString());
        }

        [Fact]
        public async Task GetTransactionIdsByTRN02_ItemNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var request = new Trn02SearchRequest
            {
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow,
                ProviderTin = "123456789",
                Trn02 = "TRN02",
                Page = 1,
                PageSize = 10
            };

            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetTransactionsByTrn02.Query>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ItemNotFoundException("No transactions found for the given search criteria."));

            // Act
            var result = await _handler.GetTransactionIdsByTRN02(request, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetTransactionIdsByTRN02_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var request = new Trn02SearchRequest
            {
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow,
                ProviderTin = "123456789",
                Trn02 = "TRN02",
                Page = 1,
                PageSize = 10
            };

            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetTransactionsByTrn02.Query>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("An unexpected error occurred."));

            // Act
            var result = await _handler.GetTransactionIdsByTRN02(request, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }

    public class Download835Document : DocumentsControllerTests
    {

        [Fact]
        public async Task Get835Document_ReturnsFile_WhenDocumentExists()
        {
            // Arrange
            const long TransactionId = 123;
            var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
            var document = new Document(stream: fileStream, contentType: "application/json", fileName: "test835.edi");
            _mockMediator.Setup(m => m.Send(It.IsAny<Download835FileByTransactionIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

            // Act
            var result = await _handler.Get835Document(TransactionId);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal(document.ContentType, fileResult.ContentType);
            Assert.Equal(document.FileName, fileResult.FileDownloadName);
        }

        [Fact]
        public async Task Get835Document_ReturnsNotFound_WhenDocumentIsNull()
        {
            // Arrange
            const long TransactionId = 123;
            _mockMediator.Setup(m => m.Send(It.IsAny<Download835FileByTransactionIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Document)null);

            // Act
            var result = await _handler.Get835Document(TransactionId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(Edi835Statuses.TransactionMissing, notFoundResult.Value);
        }

        [Fact]
        public async Task Get835Document_ReturnsNotFound_WhenNotAuthorizedExceptionThrown()
        {
            // Arrange
            const long TransactionId = 123;
            _mockMediator.Setup(m => m.Send(It.IsAny<Download835FileByTransactionIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotAuthorizedException("Not authorized"));

            // Act
            var result = await _handler.Get835Document(TransactionId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(Edi835Statuses.TransactionMissing, notFoundResult.Value);
        }

        [Fact]
        public async Task Get835Document_ReturnsNotFound_WithExceptionMessage_WhenItemNotFoundExceptionThrown()
        {
            // Arrange
            const long TransactionId = 123;
            const string ExceptionMessage = "Some other not found message";
            _mockMediator.Setup(m => m.Send(It.IsAny<Download835FileByTransactionIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ItemNotFoundException(ExceptionMessage));

            // Act
            var result = await _handler.Get835Document(TransactionId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(ExceptionMessage, notFoundResult.Value);
        }

        [Fact]
        public async Task Get835Document_ReturnsNotFound_WhenGeneralExceptionThrown()
        {
            // Arrange
            const long TransactionId = 123;
            _mockMediator.Setup(m => m.Send(It.IsAny<Download835FileByTransactionIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Unexpected error"));

            // Act
            var result = await _handler.Get835Document(TransactionId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
