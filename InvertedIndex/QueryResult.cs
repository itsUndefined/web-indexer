using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvertedIndex
{
    public class QueryResult
    {
        public string word { get; }
        public QueryInformation[] documentsList { get; }

        public QueryResult(string word, long n)
        {
            this.word = word;
            documentsList = new QueryInformation[n];
        }
    }
}
