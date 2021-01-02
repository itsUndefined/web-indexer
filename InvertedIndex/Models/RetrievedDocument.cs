using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvertedIndex.Models
{
    /*
     * The class retrieve a document from the catalogue with the similarity.
     */
    public class RetrievedDocument
    {
        public Document Document { get; set; }
        public double Similarity { get; set; }
    }
}
