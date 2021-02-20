using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvertedIndex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InvertedIndex.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly ILogger<DocumentsController> _logger;
        private readonly InvertedIndex.Services.InvertedIndex _invertedIndex;

        public DocumentsController(ILogger<DocumentsController> logger, InvertedIndex.Services.InvertedIndex invertedIndex)
        {
            _logger = logger;
            _invertedIndex = invertedIndex;
        }

        [HttpPost]
        public void InsertDocument([FromBody] InsertDocument[] documents)
        {
            Console.WriteLine("Attempting to write " + documents.Length + " documents");
            _invertedIndex.InsertToDatabase(documents);
            Console.WriteLine("Written the documents");
            return;
        }

        [HttpGet]
        public ActionResult<IList<RetrievedDocument>> SearchDocuments([FromQuery] string q)
        {
            if (q == null)
            {
                return BadRequest("q parameter is required");
            }
            var a = _invertedIndex.SearchInDatabase(q);

            return Ok(a);
        }

        [HttpGet("search-with-feedback")]
        public ActionResult<IList<RetrievedDocument>> SearchDocumentsWithFeedback(
            [FromQuery] string q,
            [FromQuery(Name = "p[]")] long[] p, //positiveFeedback
            [FromQuery(Name = "n[]")] long[] n // negativeFeedback
        )
        {
            if (q == null || p.Length == 0 && n.Length == 0)
            {
                return BadRequest("q, p and n parameters are required");
            }
            var a = _invertedIndex.SearchInDatabaseWithFeedback(q, p, n);

            return Ok(a);
        }

        [HttpDelete]
        public ActionResult DeleteAllDocuments([FromQuery] string code)
        {
            if(code != "secret_code")
            {
                return Forbid();
            }
            _invertedIndex.ResetIndex();
            return Ok();
        }
    }
}
