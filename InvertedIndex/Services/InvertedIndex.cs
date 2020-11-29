using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using BerkeleyDB;
using InvertedIndex.Models;
using System.Threading;

namespace InvertedIndex.Services
{

   
    public class InvertedIndex
    {
        private class QueryInformation
        {
            public Document Document { get; set; }
            public List<long> Poss { get; set; }
        }

        private class QueryResult
        {
            public string Word { get; set; }
            public List<QueryInformation> DocumentsList { get; set; }
        }

        private readonly HashDatabase hashDatabase;
        private readonly HashDatabaseConfig hashDatabaseConfig;
        private readonly string dbFileName = "inverted_index.db";

        private readonly DocumentsCatalogue documentsCatalogue;
        private readonly BerkeleyDB.DatabaseEnvironment env;

        private readonly IDictionary<string, Semaphore> semaphores = new Dictionary<string, Semaphore>();

        public InvertedIndex(DocumentsCatalogue documentsCatalogue, DatabaseEnvironment env)
        {
            this.documentsCatalogue = documentsCatalogue;
            this.env = env.env;
            /* Configure the database. */
            hashDatabaseConfig = new HashDatabaseConfig()
            {
                Duplicates = DuplicatesPolicy.NONE,
                Creation = CreatePolicy.IF_NEEDED,
                FreeThreaded = true,
               // AutoCommit = true,
               // Env = env.env,
            };

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

        public long InsertToDatabase(InsertDocument document)
        {
            

            Dictionary<string, long> textFreq = new Dictionary<string, long>();
            string[] text = document.Text.Split(" ");

            
            lock (semaphores)
            {
                foreach (var token in text.Distinct())
                {
                    if (semaphores.ContainsKey(token) == false)
                    {
                        semaphores.Add(token, new Semaphore(1, 1));
                    }
                    semaphores[token].WaitOne();
                }
            }


            Console.WriteLine("Went through :D");

            long documentId = this.documentsCatalogue.Length() + 1;
            this.documentsCatalogue.IncrementLength();

            for (long i = 0; i < text.Length; i++)
            {
                DatabaseEntry key = new DatabaseEntry(Encoding.UTF8.GetBytes(text[i]));
                DatabaseEntry value;
                if (hashDatabase.Exists(key))
                {
                    byte[] oldBuffer = hashDatabase.Get(key).Value.Data;
                    long[] oldValues = new long[oldBuffer.Length / sizeof(long)];
                    Buffer.BlockCopy(oldBuffer, 0, oldValues, 0 , oldBuffer.Length);
                    long[] newValues = { documentId, i };
                    long[] mergeValues = oldValues.Concat(newValues).ToArray();
                    byte[] newBuffer = new byte[mergeValues.Length * sizeof(long)];
                    Buffer.BlockCopy(mergeValues, 0, newBuffer, 0, newBuffer.Length);
                    value = new DatabaseEntry(newBuffer);
                }
                else
                {
                    long[] values = { documentId, i };
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
                //hashDatabase.Sync();
            }
            this.documentsCatalogue.InsertToDatabase(documentId, new Document() { 
                Title = document.Title,
                Url = document.Url,
                Text = document.Text,
                MaxFreq = textFreq.Values.Max()
            });

            foreach (var token in text.Distinct())
            {
                semaphores[token].Release();
            }
            return documentId;
        }

        public List<RetrievedDocument> SearchInDatabase(string str)
        {
            var txn = env.BeginTransaction();
            Dictionary<string, long> textFreq = new Dictionary<string, long>();
            string[] text = str.Split(" ");
            QueryResult[] queryResult = new QueryResult[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                DatabaseEntry key = new DatabaseEntry(Encoding.UTF8.GetBytes(text[i]));
                if (hashDatabase.Exists(key, txn))
                {
                    byte[] buffer = hashDatabase.Get(key, txn).Value.Data;
                    long[] values = new long[buffer.Length / sizeof(long)];
                    Buffer.BlockCopy(buffer, 0, values, 0, buffer.Length);
                    queryResult[i] = new QueryResult()
                    {
                        Word = text[i]
                    };
                    Document document = this.documentsCatalogue.SearchInDatabase(values[0]);
                    QueryInformation queryInformation = new QueryInformation()
                    {
                        Document = document,
                        Poss = new List<long>()
                    };
                    queryInformation.Poss.Add(values[1]);
                    for (long j = 2; j < values.Length; j += 2)
                    {
                        
                        if (values[j] == values[j - 2])
                        {
                            queryInformation.Poss.Add(values[j + 1]);
                        }
                        else
                        {
                            queryResult[i].DocumentsList.Add(queryInformation);
                            document = this.documentsCatalogue.SearchInDatabase(values[j]);
                            queryInformation = new QueryInformation()
                            {
                                Document = document,
                                Poss = new List<long>()
                            };
                            queryInformation.Poss.Add(values[j + 1]);
                        }
                    }
                    queryResult[i].DocumentsList.Add(queryInformation);
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

            txn.Commit();
            return CalculateSimilarity(queryResult, weightsInQuery, this.documentsCatalogue.Length());
        }

        private List<RetrievedDocument> CalculateSimilarity(QueryResult[] queryResult, List<double> weightsInQuery, long documentsCatalogueLength)
        {
            List<RetrievedDocument> retrievedDocuments = new List<RetrievedDocument>();
            var weightsInDocuments = new Dictionary<QueryInformation, List<double>>();
            foreach (QueryResult query in queryResult)
            {
                if(query == null)
                {
                    continue;
                }
                foreach (QueryInformation document in query.DocumentsList)
                {
                    if (weightsInDocuments.ContainsKey(document))
                    {
                        weightsInDocuments[document].Add((document.Poss.Count / document.Document.MaxFreq) * Math.Log2(documentsCatalogueLength / query.DocumentsList.Count));
                    }
                    else
                    {
                        weightsInDocuments.Add(
                            document,
                            new List<double>() {
                                (document.Poss.Count / document.Document.MaxFreq) * Math.Log2(documentsCatalogueLength / query.DocumentsList.Count) 
                            }
                        );
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

            foreach (KeyValuePair<QueryInformation, List<double>> weightD in weightsInDocuments)
            {
                double dotProduct = weightD.Value.Zip(weightsInQuery, (d1, d2) => d1 * d2).Sum();
                double vector = Math.Sqrt(weightD.Value.Sum()) * Math.Sqrt(weightsInQuery.Sum());

                double similarity;
                if(dotProduct == 0) // Check is required because dotProcuct / vector could lead to a division by zero when token exists in every document
                {
                    similarity = 0;
                } else
                {
                    similarity = dotProduct / vector;
                }
                retrievedDocuments.Add(new RetrievedDocument() {
                    Document = weightD.Key.Document,
                    Similarity = similarity
                });
            }

            return retrievedDocuments.OrderByDescending(o => o.Similarity).ToList();
        }
    }
}
