using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvertedIndex
{
    public class RetrievedDocuments
    {
        public string title { get; set; }
        public string url { get; set; }
        public string text { get; set; }
        public double similarity { get; set; }

        public RetrievedDocuments(string title, string url, string text, double similarity)
        {
            this.title = title;
            this.url = url;
            this.text = text;
            this.similarity = similarity;
        }
    }
}
