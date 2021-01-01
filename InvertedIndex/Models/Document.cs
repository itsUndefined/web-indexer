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
        public long MaxFreq { get; set; }


        public Document(long id, byte[] byteArr)
        {
            this.Id = id;
            var temp = JsonConvert.DeserializeObject<Document>(Encoding.UTF8.GetString(byteArr));
            this.Title = temp.Title;
            this.Url = temp.Url;
            this.Text = temp.Text;
            this.MaxFreq = temp.MaxFreq;
        }

        public Document()
        {
        }

        public byte[] GetByteArray()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }

    }
}
