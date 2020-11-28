using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvertedIndex
{
    public class WebsiteInformation
    {
        public long id { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string text { get; set; }

        public WebsiteInformation(long id, string title, string url, string text)
        {
            this.id = id;
            this.title = title;
            this.url = url;
            this.text = text;
        }
    }
}
