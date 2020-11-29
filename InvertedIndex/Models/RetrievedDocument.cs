using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvertedIndex.Models
{
    public class RetrievedDocument
    {
        public Document Document { get; set; }
        public double Similarity { get; set; }
    }
}
