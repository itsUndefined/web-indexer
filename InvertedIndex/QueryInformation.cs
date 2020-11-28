using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvertedIndex
{
    public class QueryInformation
    {
        public string title { get; set; }
        public string url { get; set; }

        public QueryInformation(string title, string url)
        {
            this.title = title;
            this.url = url;
        }
    }
}
