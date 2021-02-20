using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvertedIndex.Models
{
    [Serializable]
    public class Document
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Text { get; set; }

        /*
         * Constructs a Document class from the given byte array parameter.
         */
        public Document(byte[] byteArr)
        {
            var temp = JsonConvert.DeserializeObject<Document>(Encoding.UTF8.GetString(byteArr));
            this.Id = temp.Id;
            this.Title = temp.Title;
            this.Url = temp.Url;
            this.Text = temp.Text;
        }

        public Document()
        {
        }

        /*
         * Returns the class as a byte array.
         */
        public byte[] GetByteArray()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }

    }
}
