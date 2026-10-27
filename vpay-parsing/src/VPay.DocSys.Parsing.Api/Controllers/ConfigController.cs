using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Refit.Insane.PowerPack.Caching;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;

namespace VPay.DocSys.Parsing.Api.Controllers
{
    [ApiVersionNeutral]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IDocSysConfigService _configurationService;
        private readonly IXmlConfigParserService _xmlConfigParserService;
        private readonly IPlatformConfigurationApi _platformConfigApi;
        private readonly ILogger<ConfigController> _logger;

        public ConfigController(
            IDocSysConfigService configuration,
            IXmlConfigParserService xmlConfigParserService,
            IPlatformConfigurationApi platformConfigApi,
            ILogger<ConfigController> logger)
        {
            _configurationService = configuration;
            _xmlConfigParserService = xmlConfigParserService;
            _platformConfigApi = platformConfigApi;
            _logger = logger;
        }

        /// <summary>
        /// Reads the document system configuration settings at the system level and TPA level from ini files
        /// </summary>
        /// <param name="tpa">Three letter code for the third party administrator</param>
        /// <returns>An object with a dictionary of EnvironmentSettings and a dictionary of AppSettings</returns>
        [HttpGet]
        [Route("{tpa}")]
        [ProducesResponseType(typeof(DocSysConfiguration), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ReadConfig(string tpa)
        {
            var result = _configurationService.ReadConfig(tpa);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Reads the document system configuration settings at the system level and TPA level from ini files
        /// </summary>
        /// <param name="tpa">Three letter code for the third party administrator</param>
        /// <param name="useCache">If true, prefers to get list of tpa</param>
        /// <param name="cancellationToken">callback to cancel the thread action</param>
        /// <returns>An object with a dictionary of EnvironmentSettings and a dictionary of AppSettings</returns>
        [HttpGet]
        [Route("{tpa}/custom-send-back")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReadIsCustomSendBackEnabledForClient(string tpa, [FromQuery]bool useCache = true, CancellationToken cancellationToken = default)
        {
            if (!useCache)
            {
                await RefitCacheService.Instance.ClearGetCustomSendBackByClientCache().ConfigureAwait(false);
                await RefitCacheService.Instance.ClearGetCustomSendBackByClientCache(tpa).ConfigureAwait(false);
            }

            var result = await _platformConfigApi.IsCustomSendBackEnabledForClient(clientCode:tpa, cancellationToken: cancellationToken).ConfigureAwait(false);

            return Ok(result);
        }

        /// <summary>
        /// Reads the document system configuration settings at the system level and TPA level from ini files
        /// </summary>
        /// <param name="useCache">If true, prefers to get list of tpa</param>
        /// <param name="cancellationToken">callback to cancel the thread action</param>
        /// <returns>An object with a dictionary of EnvironmentSettings and a dictionary of AppSettings</returns>
        [HttpGet]
        [Route("custom-send-back")]
        [ProducesResponseType(typeof(HashSet<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReadCustomSendBackEnabledClientCodes([FromQuery] bool useCache = true, CancellationToken cancellationToken = default)
        {
            if (!useCache)
            {
                await RefitCacheService.Instance.ClearGetCustomSendBackByClientCache();
            }

            var result = await _platformConfigApi.GetCustomSendBackEnabledClientCodes(cancellationToken).ConfigureAwait(false);

            if (result?.Any() != true)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Reads the document system configuration settings at the system level and TPA level from ini files
        /// </summary>
        /// <param name="client">Three letter code for the third party administrator</param>
        /// <param name="bilingEntity">Code for the billing entity</param>
        /// <param name="useCache">If true, prefers to get list of tpa</param>
        /// <param name="cancellationToken">callback to cancel the thread action</param>
        /// <returns>An object with a dictionary of EnvironmentSettings and a dictionary of AppSettings</returns>
        [HttpGet]
        [Route("{client}/edi-delivery")]
        [ProducesResponseType(typeof(IEnumerable<EdiDeliveryClientBoardingConfigDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReadEdiDeliveryForClient(string client, [FromQuery]string bilingEntity, [FromQuery] bool useCache = true, CancellationToken cancellationToken = default)
        {
            if (!useCache)
            {
                await RefitCacheService.Instance.ClearGetEdiDeliveryByClientCache(client, bilingEntity).ConfigureAwait(false);
            }

            var result = await _platformConfigApi.GetEdiDeliveryForClient(clientCode: client, billingEntityCode: bilingEntity, cancellationToken: cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Reads the document system configuration settings at the system level and TPA level from ini files
        /// </summary>
        /// <param name="client">Three letter code for the third party administrator</param>
        /// <param name="bilingEntity">Code for the billing entity</param>
        /// <param name="origin">Location that the edi file should be sent</param>
        /// <param name="paymentType">File extension, with period, that should be sent</param>
        /// <param name="useCache">If true, prefers to get list of tpa</param>
        /// <param name="cancellationToken">callback to cancel the thread action</param>
        /// <returns>An object with a dictionary of EnvironmentSettings and a dictionary of AppSettings</returns>
        [HttpGet]
        [Route("{client}/{key}")]
        [ProducesResponseType(typeof(List<ClientBoardingConfigDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReadConfigurationKeyForClient(string client, string key,
            [FromQuery] string bilingEntity,
            [FromQuery] string origin,
            [FromQuery] string paymentType,
            [FromQuery] bool useCache = true, CancellationToken cancellationToken = default)
        {
            origin ??= "";
            client ??= "";
            paymentType ??= "";
            bilingEntity ??= "";

            if (!useCache)
            {
                await RefitCacheService.Instance.ClearCache<IPlatformConfigurationApi, Pagination<ClientBoardingConfigDto>>(
                    s => s.GetClientBoardingConfigs<ClientBoardingConfigDto>(
                        key,
                        origin,
                        client,
                        bilingEntity,
                        paymentType,
                        null, 1, 50, default));
            }

            var result = await _platformConfigApi.GetClientBoardingConfigs<ClientBoardingConfigDto>(
                configurationName: key,
                originCode: origin,
                clientCode: client,
                billingEntityCode: bilingEntity,
                paymentTypeCode: paymentType,
                cancellationToken: cancellationToken);

            return Ok(result.Items.OrderBy(o => o.RelevanceByDefaults()).ToList());
        }


        [HttpGet]
        [Route("{tpa}/xml/{be}")]
        [ProducesResponseType(typeof(ConfigurationXmlDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ReadConfigXmls(string tpa, string be)
        {
            tpa = tpa.ToUpper();
            be = be.ToUpper();

            try
            {
                var result = _xmlConfigParserService.ReadXmlConfiguration(tpa, be);

                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (Exception e) when (KeepScope(e))
            {
                _logger.LogError(e, "Could not read xml configuration for TPA:{TPA}, BE:{BE}",
                    tpa.Replace(Environment.NewLine, ""),
                    be.Replace(Environment.NewLine, ""));

                return BadRequest(e);
            }
        }

        private bool KeepScope(Exception e) { return true; }
    }
}
