using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace InvertedIndex.Models
{
    public class InsertDocument
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Url { get; set; }
        [Required]
        public string Text { get; set; }

        /* 
         * The class gets a document from the crawler.
         */
        public InsertDocument(string title, string url, string text)
        {
            this.Title = title;
            this.Url = url;
            this.Text = text;
        }
    }
}
