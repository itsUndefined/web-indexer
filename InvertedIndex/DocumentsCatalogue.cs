using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using BerkeleyDB;

namespace InvertedIndex
{
    public class DocumentsCatalogue
    {
        private HashDatabase hashDatabase;
        private HashDatabaseConfig hashDatabaseConfig;
        private string dbFileName = "documents_catalogue.db";

        [Serializable]
        private class Data
        {
            public string title { get; set; }
            public string url { get; set; }
            public string text { get; set; }
            public long maxFreq { get; set; }

            public Data()
            {
                title = "";
                url = "";
                text = "";
            }
            public Data(WebsiteInformation websiteInformation, long maxFreq)
            {
                title = websiteInformation.title;
                url = websiteInformation.url;
                text = websiteInformation.text;
                this.maxFreq = maxFreq;
            }
            public byte[] GetByteArray()
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    binaryFormatter.Serialize(memoryStream, this);
                    return memoryStream.ToArray();
                }
            }
            public void SetData(byte[] byteArr)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    memoryStream.Write(byteArr, 0, byteArr.Length);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    Data data = (Data)binaryFormatter.Deserialize(memoryStream);
                    title = data.title;
                    url = data.url;
                    text = data.text;
                    maxFreq = data.maxFreq;
                }
            }
        }

        public DocumentsCatalogue()
        {
            /* Configure the database. */
            hashDatabaseConfig = new HashDatabaseConfig();
            hashDatabaseConfig.Duplicates = DuplicatesPolicy.NONE;
            hashDatabaseConfig.Creation = CreatePolicy.IF_NEEDED;

            /* Create the database if does not already exist and open the database file. */
            try
            {
                hashDatabase = HashDatabase.Open(dbFileName, hashDatabaseConfig);
                Console.WriteLine("{0} open.", dbFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error opening {0}.", dbFileName);
                Console.WriteLine(e.Message);
                return;
            }
        }

        ~DocumentsCatalogue()
        {
            hashDatabase.Close();
            hashDatabase.Dispose();
        }

        public void InsertToDatabase(WebsiteInformation websiteInformation, long maxFreq)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(websiteInformation.id));
            if (!hashDatabase.Exists(key))
            {
                DatabaseEntry value = new DatabaseEntry(new Data(websiteInformation, maxFreq).GetByteArray());
                hashDatabase.Put(key, value);
                hashDatabase.Sync();
            }
        }
        public QueryInformation SearchInDatabase(long id)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(id));
            Data data = new Data();
            data.SetData(hashDatabase.Get(key).Value.Data);
            QueryInformation queryInformation = new QueryInformation(data.title, data.url, data.maxFreq);
            return queryInformation;
        }
        public long Length()
        {
            return hashDatabase.TableSize;
        }
    }
}
