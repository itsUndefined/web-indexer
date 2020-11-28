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
            Dictionary<string, long> textFreq = new Dictionary<string, long>();
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
                if (textFreq.ContainsKey(text[i]))
                {
                    textFreq[text[i]] = textFreq[text[i]] + 1;
                }
                else
                {
                    textFreq.Add(text[i], 1);
                }
                hashDatabase.Put(key, value);
                hashDatabase.Sync();
            }
            DocumentsCatalogue data = new DocumentsCatalogue();
            data.InsertToDatabase(websiteInformation, textFreq.Values.Max());
        }

        public List<RetrievedDocuments> SearchInDatabase(string str)
        {
            Dictionary<string, long> textFreq = new Dictionary<string, long>();
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
                    queryResult[i] = new QueryResult(text[i]);
                    QueryInformation queryInformation = documentsCatalogue.SearchInDatabase(values[0]);
                    queryInformation.poss.Add(values[1]);
                    for (long j = 2; j < values.Length; j += 2)
                    {
                        
                        if (values[j] == values[j - 2])
                        {
                            queryInformation.poss.Add(values[j + 1]);
                        }
                        else
                        {
                            queryResult[i].documentsList.Add(queryInformation);
                            queryInformation = documentsCatalogue.SearchInDatabase(values[j]);
                            queryInformation.poss.Add(values[j + 1]);
                        }
                    }
                    queryResult[i].documentsList.Add(queryInformation);
                }
                if (textFreq.ContainsKey(text[i]))
                {
                    textFreq[text[i]] = textFreq[text[i]] + 1;
                }
                else
                {
                    textFreq.Add(text[i], 1);
                }
            }

            /*foreach (QueryResult res in queryResult)
            {
                Console.WriteLine("Word: {0}", res.word);
                foreach (QueryInformation inf in res.documentsList)
                {
                    Console.WriteLine("Title: {0}, URL: {1}, Max Freq.: {2}", inf.title, inf.url, inf.maxFreq);
                    foreach (long p in inf.poss)
                    {
                        Console.WriteLine("Poss.: {0}", p);
                    }
                }
            }*/

            List<double> weightsInQuery = new List<double>();
            foreach (KeyValuePair<string, long> pair in textFreq)
            {
                weightsInQuery.Add(pair.Value / textFreq.Values.Max());
            }

            
            return CalculateSimilarity(queryResult, weightsInQuery, documentsCatalogue.Length());
        }

        private List<RetrievedDocuments> CalculateSimilarity(QueryResult[] queryResult, List<double> weightsInQuery, long documentsCatalogueLength)
        {
            List<RetrievedDocuments> retrievedDocuments = new List<RetrievedDocuments>();
            Dictionary<string, List<double>> weightsInDocuments = new Dictionary<string, List<double>>();
            foreach (QueryResult query in queryResult)
            {
                foreach (QueryInformation document in query.documentsList)
                {
                    if (weightsInDocuments.ContainsKey(document.title))
                    {
                        weightsInDocuments[document.title].Add((document.poss.Count / document.maxFreq) * Math.Log2(documentsCatalogueLength / query.documentsList.Count));
                    }
                    else
                    {
                        weightsInDocuments.Add(document.title, new List<double>() { (document.poss.Count / document.maxFreq) * Math.Log2(documentsCatalogueLength / query.documentsList.Count) });
                    }
                }
            }

            /*foreach (KeyValuePair<string, List<double>> val in weightsInDocuments)
            {
                Console.WriteLine("Key: {0}", val.Key);
                foreach (double val2 in val.Value)
                {
                    Console.WriteLine("Value: {0}", val2);
                }
            }*/

            foreach (KeyValuePair<string, List<double>> weightD in weightsInDocuments)
            {
                double dotProduct = weightD.Value.Zip(weightsInQuery, (d1, d2) => d1 * d2).Sum();
                double vector = Math.Sqrt(weightD.Value.Sum()) * Math.Sqrt(weightsInQuery.Sum());
                retrievedDocuments.Add(new RetrievedDocuments(weightD.Key, "", "", dotProduct / vector));
            }

            return retrievedDocuments.OrderByDescending(o => o.similarity).ToList();
        }
    }
}
