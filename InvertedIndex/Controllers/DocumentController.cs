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
        public void InsertDocument([FromBody] InsertDocument document)
        {
            Console.WriteLine("Attempting to write document with url: " + document.Url);
            var id = _invertedIndex.InsertToDatabase(document);
            Console.WriteLine("Written document: " + id);
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
    }
}
