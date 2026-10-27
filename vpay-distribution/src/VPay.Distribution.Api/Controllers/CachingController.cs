using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VPay.Distribution;
using VPay.Distribution.Common;
using VPay.DocSys.Common;

namespace VPay.DocSys.Distribution.Api.Controllers
{
	/// <summary>
	/// Manages caching for the distribution services.
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
	[Produces("application/json")]
	[Route("api/Caching")]
	[ApiController]
	public class CachingController : ControllerBase
	{
		private readonly IDocSysHttpClient _docSysHttpClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="CachingController"/> class.
		/// </summary>
		public CachingController(IDocSysHttpClient docSysHttpClient)
		{
			ArgumentHelpers.ThrowIf.IsNull(docSysHttpClient, nameof(docSysHttpClient));
			ArgumentHelpers.ThrowIf.IsNull(docSysHttpClient.DocSysConfigCache, $"{nameof(docSysHttpClient)}.{nameof(docSysHttpClient.DocSysConfigCache)}");

			_docSysHttpClient = docSysHttpClient;
		}

		/// <summary>
		/// Gets the list of TPAs for which the DocSysConfigCache currently holds a DocSysConfiguration
		/// </summary>
		/// <response code="200">List of TPAs</response>
		[HttpGet("GetCachedConfigTpaList")]
		[ProducesResponseType(typeof(string[]), 200)]
		public IActionResult GetCachedConfigTpaList()
		{
			return Ok(_docSysHttpClient.DocSysConfigCache.Keys.ToArray());
		}

		/// <summary>
		/// Adds a DocSysConfiguration to the DocSysConfigCache for the TPAs specified in the <paramref name="tpaList"/> parameter if it does not already exist.
		/// </summary>
		/// <param name="tpaList">The list of TPAs to add a configuration for.</param>
		[HttpPost("AddTpaToConfigCache")]
		[ProducesDefaultResponseType]
		public async Task<IActionResult> AddTpaToConfigCache(params string[] tpaList)
		{
			ArgumentHelpers.ThrowIf.IsNullOrEmpty(tpaList, nameof(tpaList));

			if(tpaList.Any(s => string.IsNullOrWhiteSpace(s)))
				return this.BadRequest("One or more values in the request parameters is missing or empty.");

			foreach(var tpa in tpaList)
			{
				try
				{
					// calling GetDocSysConfig() will get and cache the config for the specified tpa if isn't already cached.
					await _docSysHttpClient.GetDocSysConfig(tpa);
				}
				catch(Exception exception)
				{
					return StatusCode(500, $"Failed to add configuration to cache for '{tpa}': {exception.Message}");
				}
			}

			return Ok();
		}

		/// <summary>
		/// Removes the DocSysConfiguration from the DocSysConfigCache for the TPAs specified in the <paramref name="tpaList"/> parameter if it exists.
		/// </summary>
		/// <param name="tpaList">The list of TPAs to remove the configuration for.</param>
		[HttpPost("RemoveTpaFromConfigCache")]
		[ProducesDefaultResponseType]
		public IActionResult RemoveTpaFromConfigCache(params string[] tpaList)
		{
			foreach(var tpa in tpaList)
			{
				// since the cache key is always case-sensitive but the GetDocSysConfig() method is not, let's always upper case the tpa
				_docSysHttpClient.DocSysConfigCache.TryRemove(tpa.ToUpper());
			}

			return Ok();
		}

		/// <summary>
		/// Gets a value indicating whether the specified DocSysConfiguration is cached for the specified TPA. NOTE: this will reset any sliding expiration windows for cached items.
		/// </summary>
		/// <param name="tpa">The TPA to check for cached config.</param>
		[HttpPost("GetIsTpaConfigCached")]
		[ProducesDefaultResponseType(typeof(bool))]
		public IActionResult GetIsTpaConfigCached(string tpa)
		{
			var result = _docSysHttpClient.DocSysConfigCache.TryGet(tpa.ToUpper(), out var _);

			return Ok(result);
		}

		/// <summary>
		/// Clears all DocSysConfiguration entries from the DocSysConfigCache.
		/// </summary>
		[HttpPost("ClearConfigCache")]
		[ProducesDefaultResponseType]
		public IActionResult ClearConfigCache()
		{
			_docSysHttpClient.DocSysConfigCache.Clear();

			return Ok();
		}

		/// <summary>
		/// Gets the amount of memory, in bytes, currently consumed by the DocSysConfigCache.
		/// </summary>
		/// <returns></returns>
		[HttpGet("GetConfigCacheMemoryUsage")]
		[ProducesResponseType(typeof(long), 200)]
		public IActionResult GetConfigCacheMemoryUsage()
		{
			return Ok(_docSysHttpClient.DocSysConfigCache.GetMemoryUsage());
		}
	}
}
