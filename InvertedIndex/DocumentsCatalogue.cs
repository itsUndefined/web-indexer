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

        private class Data
        {
            public string title { get; set; }
            public string url { get; set; }

            public Data()
            {
                title = "";
                url = "";
            }
            public Data(WebsiteInformation websiteInformation)
            {
                title = websiteInformation.title;
                url = websiteInformation.url;
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
                Console.WriteLine("{0} created.", dbFileName);
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
        }

        public void InsertToDatabase(WebsiteInformation websiteInformation)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(websiteInformation.id));
            if (!hashDatabase.Exists(key))
            {
                DatabaseEntry value = new DatabaseEntry(new Data(websiteInformation).GetByteArray());
                hashDatabase.Put(key, value);
            }
        }
        public QueryInformation SearchInDatabase(long id)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(id));
            Data data = new Data();
            data.SetData(hashDatabase.Get(key).Value.Data);
            QueryInformation queryInformation = new QueryInformation();
            queryInformation.title = data.title;
            queryInformation.url = data.url;
            return queryInformation;
        }
    }
}
