using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using BerkeleyDB;
using InvertedIndex.Models;
using System.Threading;
using System.Collections;

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

        //private readonly IDictionary<string, Semaphore> semaphores = new Dictionary<string, Semaphore>();

        private readonly Semaphore locked = new Semaphore(1, 1);

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
                CacheSize = new CacheInfo(4, 0, 128),
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
            Console.WriteLine("Closing index");
            /* Close the database. */
            hashDatabase.Close();
            hashDatabase.Dispose();
        }

        public void InsertToDatabase(InsertDocument[] documents)
        {
            this.locked.WaitOne();

            var partialInvertedIndex = new Dictionary<string, IList<(long, long)>>();

            foreach(InsertDocument document in documents) 
            {

                Dictionary<string, long> textFreq = new Dictionary<string, long>();
                string[] text = document.Text.Split(" ");

                /*
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
                */

                long documentId = this.documentsCatalogue.Length() + 1;
                this.documentsCatalogue.IncrementLength();

                for (long i = 0; i < text.Length; i++)
                {
                    if (partialInvertedIndex.TryGetValue(text[i], out IList<(long, long)> value))
                    {
                        value.Add((documentId, i));
                    }
                    else
                    {
                        partialInvertedIndex.Add(text[i], new List<(long, long)>(new (long, long)[] { (documentId, i) } ));
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

                this.documentsCatalogue.InsertToDatabase(documentId, new Document() { 
                    Title = document.Title,
                    Url = document.Url,
                    Text = document.Text,
                    MaxFreq = textFreq.Values.Max()
                });

                /*
                foreach (var token in text.Distinct())
                {
                    semaphores[token].Release();
                }
                */
            }


            foreach(var token in partialInvertedIndex.ToList())
            {
                DatabaseEntry key = new DatabaseEntry(Encoding.UTF8.GetBytes(token.Key));
                DatabaseEntry value;
                if (hashDatabase.Exists(key))
                {
                    /*
                    byte[] oldBuffer = hashDatabase.Get(key).Value.Data;
                    long[] oldValues = new long[oldBuffer.Length / sizeof(long)];
                    Buffer.BlockCopy(oldBuffer, 0, oldValues, 0, oldBuffer.Length);
                    long[] newValues = { documentId, i };
                    long[] mergeValues = oldValues.Concat(newValues).ToArray();
                    byte[] newBuffer = new byte[mergeValues.Length * sizeof(long)];
                    Buffer.BlockCopy(mergeValues, 0, newBuffer, 0, newBuffer.Length);
                    value = new DatabaseEntry(newBuffer);
                    */


                    byte[] oldBuffer = hashDatabase.Get(key).Value.Data;

                    long[] newValues = token.Value.SelectMany(x => new long[] { x.Item1, x.Item2 }).ToArray();
                    byte[] newBuffer = new byte[newValues.Length * sizeof(long)];
                    Buffer.BlockCopy(newValues, 0, newBuffer, 0, newBuffer.Length);

                    byte[] mergedBuffer = oldBuffer.Concat(newBuffer).ToArray();
                    value = new DatabaseEntry(mergedBuffer);
                }
                else
                {
                    long[] values = token.Value.SelectMany(x => new long[] { x.Item1, x.Item2 }).ToArray();
                    byte[] buffer = new byte[values.Length * sizeof(long)];
                    Buffer.BlockCopy(values, 0, buffer, 0, buffer.Length);
                    value = new DatabaseEntry(buffer);
                }

                hashDatabase.Put(key, value);
            }
            this.locked.Release();
            hashDatabase.Sync();
            documentsCatalogue.SyncToDisk();
            
            // return documentId;
        }

        public List<RetrievedDocument> SearchInDatabase(string str)
        {
            return Query(GenerateQueryVec(str));
        }

        public List<RetrievedDocument> SearchInDatabaseWithFeedback(string query, long[] positiveFeedback, long[] negativeFeedback)
        {

            Dictionary<string, long> positiveVec = new Dictionary<string, long>();
            Dictionary<string, long> negativeVec = new Dictionary<string, long>();

            foreach(long docId in positiveFeedback)
            {
                var document = documentsCatalogue.SearchInDatabase(docId);

                foreach(var word in document.Text.Split(' '))
                {
                    if (positiveVec.ContainsKey(word))
                    {
                        positiveVec[word] = positiveVec[word] + 1;
                    }
                    else
                    {
                        positiveVec.Add(word, 1);
                    }
                }
            }

            foreach(long docId in negativeFeedback)
            {
                var document = documentsCatalogue.SearchInDatabase(docId);

                foreach(var word in document.Text.Split(' '))
                {
                    if (negativeVec.ContainsKey(word))
                    {
                        negativeVec[word] = negativeVec[word] + 1;
                    }
                    else
                    {
                        negativeVec.Add(word, 1);
                    }
                }
            }

            return Query(CalculateVector(GenerateQueryVec(query), 1, CalculateVector(positiveVec, 1, negativeVec, -1), 1));
        }

        private Dictionary<string, long> CalculateVector(Dictionary<string, long> a, long multiplierA, Dictionary<string, long> b, long multiplierB)
        {
            var resultVec = new Dictionary<string, long>();

            foreach(var word in a)
            {
                long result = word.Value * multiplierA + b.GetValueOrDefault(word.Key) * multiplierB;
                resultVec.Add(word.Key, result);
            }

            foreach(var word in b)
            {
                if(!resultVec.ContainsKey(word.Key))
                {
                    long result = word.Value * multiplierB;
                    resultVec.Add(word.Key, result);
                }
            }

            return resultVec;
        }

        private Dictionary<string, long> GenerateQueryVec(string query) {
            var queryVec = new Dictionary<string, long>();

            foreach(var word in query.Split(' '))
            {
                if (queryVec.ContainsKey(word))
                {
                    queryVec[word] = queryVec[word] + 1;
                }
                else
                {
                    queryVec.Add(word, 1);
                }
            }
            return queryVec;
        }

        private List<RetrievedDocument> Query(Dictionary<string, long> query)
        {
            Dictionary<string, long> textFreq = new Dictionary<string, long>();
            QueryResult[] queryResult = new QueryResult[query.Count];

            long i = 0;

            foreach (var wordWithWeight in query)
            {
                var word = wordWithWeight.Key;
                DatabaseEntry key = new DatabaseEntry(Encoding.UTF8.GetBytes(word));
                if (hashDatabase.Exists(key))
                {
                    byte[] buffer = hashDatabase.Get(key).Value.Data;
                    long[] values = new long[buffer.Length / sizeof(long)];
                    Buffer.BlockCopy(buffer, 0, values, 0, buffer.Length);
                    queryResult[i] = new QueryResult()
                    {
                        Word = word,
                        DocumentsList = new List<QueryInformation>()
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
                        if (values[j] != values[j - 2]) // If different document
                        {
                            queryResult[i].DocumentsList.Add(queryInformation);
                            document = this.documentsCatalogue.SearchInDatabase(values[j]);
                            queryInformation = new QueryInformation()
                            {
                                Document = document,
                                Poss = new List<long>()
                            };
                        }
                        queryInformation.Poss.Add(values[j + 1]);
                    }
                    queryResult[i].DocumentsList.Add(queryInformation);
                }
                if (textFreq.ContainsKey(word))
                {
                    textFreq[word] = textFreq[word] + 1;
                }
                else
                {
                    textFreq.Add(word, 1);
                }
                i++;
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
                        weightsInDocuments[document].Add((document.Poss.Count / (double) document.Document.MaxFreq) * Math.Log2(documentsCatalogueLength / (double) query.DocumentsList.Count));
                    }
                    else
                    {
                        weightsInDocuments.Add(
                            document,
                            new List<double>() {
                                (document.Poss.Count / (double) document.Document.MaxFreq) * Math.Log2(documentsCatalogueLength / (double) query.DocumentsList.Count) 
                            }
                        );
                    }
                }
            }

            /*
            foreach (KeyValuePair <QueryInformation, List<double>> val in weightsInDocuments)
            {
                foreach (double val2 in val.Value)
                {
                    Console.WriteLine("Value: {0}", val2);
                }
            }
            */
          

            foreach (KeyValuePair<QueryInformation, List<double>> weightD in weightsInDocuments)
            {
                double dotProduct = weightD.Value.Zip(weightsInQuery, (d1, d2) => d1 * d2).Sum();
                double vector = Math.Sqrt(weightD.Value.Sum()) * Math.Sqrt(weightsInQuery.Sum());

                double similarity;
                if(vector == 0) // Check is required because dotProcuct / vector could lead to a division by zero when token exists in every document
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
