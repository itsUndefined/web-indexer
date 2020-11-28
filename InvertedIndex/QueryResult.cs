using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvertedIndex
{
    public class QueryResult
    {
        public string word { get; }
        public List<QueryInformation> documentsList { get; }

        public QueryResult(string word)
        {
            this.word = word;
            documentsList = new List<QueryInformation>();
        }
    }
}
