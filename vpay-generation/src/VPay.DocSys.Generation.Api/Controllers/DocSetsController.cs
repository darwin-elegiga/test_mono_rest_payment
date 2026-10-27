using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiVersionNeutral]
    [Produces("application/json")]
    [Route("api/DocSets")]
    [ApiController]
    [DisableRequestSizeLimit]
    public class DocSetsController : ControllerBase
    {

        private readonly IDocSysOperations _docSysOperations;

        public DocSetsController(IDocSysOperations docSysOperations)
        {
            _docSysOperations = docSysOperations;
        }

        /// <summary>
        /// This will build the manifest of documents that will be generated during the Document Generation process
        /// Uses DocGenParamValidator
        /// </summary>
        /// <param name="entity">This is the information needed to generate documents for a Transaction</param>
        /// <returns>The manifest of documents that need to be generated for a Transaction</returns>
        /// <response code="200">Returns an manifest of documents that need to be generated for a Transaction</response>
        [HttpPost("BuildManifest")]
        [ProducesResponseType(typeof(DocGenManifest), 200)]
        public async Task<IActionResult> PostBuildGenerationSet(DocGenParam entity)
        {
            var result = await _docSysOperations.GetDocumentManifest(entity);
            return Ok(result);
        }

        /// <summary>
        /// This will generate all documents for the passed in transaction
        /// </summary>
        /// <param name="entity">This is the information needed to generate all documents for a Transaction</param>
        /// <returns>The list of all documents that are generated for a Transaction</returns>
        /// <response code="200">Returns list of all documents that are generated for a Transaction</response>
        [HttpPost("GenerateDocs")]
        [ProducesResponseType(typeof(IEnumerable<IGeneratedFile>), 200)]
        public async Task<IActionResult> PostGenerateDocs(DocGenParam entity)
        {
            var result = await _docSysOperations.GenerateAllDocs(entity);

            return Ok(result);
        }


        /// <summary>
        /// This will generate the eob xml and pdf document for the passed in transaction
        /// </summary>
        /// <param name="entity">This is the information needed to generate the eob document for a Transaction</param>
        /// <returns>The eob document that are generated for a Transaction</returns>
        /// <response code="200">Returns file information for eob xml and pdf documents generated for a Transaction</response>
        /// <response code="400">An eob xml and pdf document is not allowed to be generated for this document</response>
        [HttpPost("GenerateEOB")]
        [ProducesResponseType(typeof(IEnumerable<EobGeneratedFile>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostGenerateEOB(DocGenParam entity)
        {
            var result = await _docSysOperations.GenerateEobDoc(entity);

            if (result == null)
            {
                return BadRequest();
            }

            return Ok(result);
        }

        /// <summary>
        /// This will generate all vpayges for the passed in transaction
        /// </summary>
        /// <param name="entity">This is the information needed to generate all vpayge documents for a Transaction</param>
        /// <returns>The list of all vpayge documents that are generated for a Transaction</returns>
        /// <response code="200">Returns list of all vpayge documents that are generated for a Transaction</response>
        [HttpPost("GenerateVPayge")]
        [ProducesResponseType(typeof(IEnumerable<VPaygeGeneratedFile>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostGenerateVPayge(DocGenParam entity)
        {
            var result = await _docSysOperations.GenerateVPaygeDocs(entity);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }


    }
}
