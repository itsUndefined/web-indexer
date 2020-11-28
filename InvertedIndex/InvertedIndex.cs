using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using BerkeleyDB;

namespace InvertedIndex
{

    public class InvertedIndex
    {
        private HashDatabase hashDatabase;
        private HashDatabaseConfig hashDatabaseConfig;
        private string dbFileName = "inverted_index.db";

        public InvertedIndex()
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

        ~InvertedIndex()
        {
            /* Close the database. */
            hashDatabase.Close();
            hashDatabase.Dispose();
        }

        public void InsertToDatabase(WebsiteInformation websiteInformation)
        {
            DocumentsCatalogue data = new DocumentsCatalogue();
            data.InsertToDatabase(websiteInformation);
            string[] text = websiteInformation.text.Split(" ");
            for (long i = 0; i < text.Length; i++)
            {
                DatabaseEntry key = new DatabaseEntry(Encoding.UTF8.GetBytes(text[i]));
                DatabaseEntry value;
                if (hashDatabase.Exists(key))
                {
                    byte[] oldBuffer = hashDatabase.Get(key).Value.Data;
                    long[] oldValues = new long[oldBuffer.Length / sizeof(long)];
                    Buffer.BlockCopy(oldBuffer, 0, oldValues, 0 , oldBuffer.Length);
                    //long[] oldValues = Array.ConvertAll(oldBuffer, c => (long)c);
                    long[] newValues = { websiteInformation.id, i };
                    long[] mergeValues = oldValues.Concat(newValues).ToArray();
                    byte[] newBuffer = new byte[mergeValues.Length * sizeof(long)];
                    Buffer.BlockCopy(mergeValues, 0, newBuffer, 0, newBuffer.Length);
                    value = new DatabaseEntry(newBuffer);
                }
                else
                {
                    long[] values = { websiteInformation.id, i };
                    byte[] buffer = new byte[values.Length * sizeof(long)];
                    Buffer.BlockCopy(values, 0, buffer, 0, buffer.Length);
                    value = new DatabaseEntry(buffer);
                }
                hashDatabase.Put(key, value);
                hashDatabase.Sync();
            }
        }

        public QueryResult[] SearchInDatabase(string str)
        {
            string[] text = str.Split(" ");
            QueryResult[] queryResult = new QueryResult[text.Length];
            DocumentsCatalogue documentsCatalogue = new DocumentsCatalogue();
            for (int i = 0; i < text.Length; i++)
            {
                DatabaseEntry key = new DatabaseEntry(Encoding.UTF8.GetBytes(text[i]));
                if (hashDatabase.Exists(key))
                {
                    byte[] buffer = hashDatabase.Get(key).Value.Data;
                    long[] values = new long[buffer.Length / sizeof(long)];
                    Buffer.BlockCopy(buffer, 0, values, 0, buffer.Length);
                    //long[] values = Array.ConvertAll(buffer, c => (long)c);
                    queryResult[i] = new QueryResult(text[i], values.Length / 2);
                    for (long j = 0; j < values.Length; j += 2)
                    {
                        queryResult[i].documentsList[j / 2] = documentsCatalogue.SearchInDatabase(values[j]);
                    }
                }
            }
            return queryResult;
        }
    }
}
