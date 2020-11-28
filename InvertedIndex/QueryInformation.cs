﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvertedIndex
{
    public class QueryInformation
    {
        public string title { get; set; }
        public string url { get; set; }
        public long maxFreq { get; set; }
        public List<long> poss { get; set; }

        public QueryInformation(string title, string url, long maxFreq)
        {
            this.title = title;
            this.url = url;
            this.maxFreq = maxFreq;
            poss = new List<long>();
        }
    }
}
