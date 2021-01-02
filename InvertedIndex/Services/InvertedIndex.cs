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
            public long DocumentId { get; set; }
            public List<long> Poss { get; set; }
        }

        // private class QueryResult
        // {
        //     public string Word { get; set; }
        //     public List<QueryInformation> DocumentsList { get; set; }
        // }

        private readonly HashDatabase hashDatabase;
        private readonly HashDatabase maxFreqDatabase;
        private readonly HashDatabaseConfig hashDatabaseConfig;

        private readonly DocumentsCatalogue documentsCatalogue;
        private readonly BerkeleyDB.DatabaseEnvironment env;

        //private readonly IDictionary<string, Semaphore> semaphores = new Dictionary<string, Semaphore>();

        private readonly Semaphore locked = new Semaphore(1, 1);

        /*
         * Configure and open hash databases for inverted index.
         */
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
                CacheSize = new CacheInfo(1, 0, 128),
               // Env = env.env,
            };

            /* Create the database if does not already exist and open the database file. */
            try 
            {
                hashDatabase = HashDatabase.Open("inverted_index.db", hashDatabaseConfig);
                maxFreqDatabase = HashDatabase.Open("max_freq.db", hashDatabaseConfig);
                //Console.WriteLine("{0} open.", dbFileName);
            }
            catch (Exception e)
            {
               // Console.WriteLine("Error opening {0}.", dbFileName);
                Console.WriteLine(e.Message);
                return;
            }
        }

        /*
         * Close the hash database (close inverted index).
         */
        ~InvertedIndex()
        {
            Console.WriteLine("Closing index");
            /* Close the database. */
            hashDatabase.Close();
            hashDatabase.Dispose();
        }

        /*
         * Insert new documents to the inverted index (hash databese). Each word at each document is searched in the database.
         * If the word exists in the inverted index then the word's frequency increased, else the word added to the database.
         * For each word that will go to added to the inverted index the algorithm will calculate the maximum frequency word in the whole document.
         * Finally the document is added to the database with hash keys the words that they are contained in the document.
         */
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

                DatabaseEntry maxFreqKey = new DatabaseEntry(BitConverter.GetBytes(documentId));
                DatabaseEntry maxFreqValue = new DatabaseEntry(BitConverter.GetBytes(textFreq.Values.Max()));
                maxFreqDatabase.Put(maxFreqKey, maxFreqValue);

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
            maxFreqDatabase.Sync();
            
            // return documentId;
        }

        /*
         * A sentence is searched in the database and the function returns a list with documents that they have the biggest similarity with the given sentence.
         */
        public List<RetrievedDocument> SearchInDatabase(string str)
        {
            var query = GenerateQueryVec(str);
            var documentsWithWords = GetDocumentsFromInverseIndex(query.Keys);
            var documentsCatalogueLength = this.documentsCatalogue.Length();
            var weightsInQuery = CalculateWeightsOfQuery(query, documentsWithWords, documentsCatalogueLength);
            var weightsInDocuments = CalculateWeightsOfDocuments(documentsWithWords);
            return CalculateSimilarity(weightsInDocuments, weightsInQuery, documentsCatalogueLength);
        }

        public List<RetrievedDocument> SearchInDatabaseWithFeedback(string query, long[] positiveFeedback, long[] negativeFeedback)
        {

            var originalQuery = GenerateQueryVec(query);

            var originalDocumentsWithWords = GetDocumentsFromInverseIndex(originalQuery.Keys);
            var documentsCatalogueLength = this.documentsCatalogue.Length();
            var originalQueryWeights = CalculateWeightsOfQuery(originalQuery, originalDocumentsWithWords, documentsCatalogueLength);


            IList<string> docWords = new List<string>();
            var documents = positiveFeedback.Select(docId => documentsCatalogue.SearchInDatabase(docId));
            documents = documents.Concat(negativeFeedback.Select(docId => documentsCatalogue.SearchInDatabase(docId)));

            foreach (var documentWords in documents.Select(x => GenerateQueryVec(x.Text)))
            {
                foreach (var word in documentWords)
                {
                    if(!docWords.Contains(word.Key))
                    {
                        docWords.Add(word.Key);
                    }
                }
            }

            docWords = docWords.Concat(originalQuery.Where(x => !docWords.Contains(x.Key)).Select(x => x.Key)).ToList();

            var modifiedDocumentsWithWords = GetDocumentsFromInverseIndex(docWords);

            Dictionary<string, double> positiveVec = new Dictionary<string, double>();
            Dictionary<string, double> negativeVec = new Dictionary<string, double>();

            foreach(long docId in positiveFeedback)
            {
                var document = documents.First(x => x.Id == docId);

                var words = GenerateQueryVec(document.Text);

                foreach (var word in words)
                {
                    var tf = word.Value / (double)words.Values.Max();

                    if (positiveVec.ContainsKey(word.Key))
                    {
                        positiveVec[word.Key] += tf;
                    }
                    else
                    {
                        positiveVec.Add(word.Key, tf);
                    }
                }
            }

            foreach(long docId in negativeFeedback)
            {
                var document = documents.First(x => x.Id == docId);

                var words = GenerateQueryVec(document.Text);

                foreach (var word in words)
                {
                    var tf = word.Value / (double)words.Values.Max();

                    if (negativeVec.ContainsKey(word.Key))
                    {
                        negativeVec[word.Key] += tf;
                    }
                    else
                    {
                        negativeVec.Add(word.Key, tf);
                    }
                }
            }


            foreach(var wordWeightPair in positiveVec)
            {
                var idf = Math.Log2(
                    documentsCatalogueLength / 
                    modifiedDocumentsWithWords[wordWeightPair.Key].Count
                );

                if (idf == double.PositiveInfinity)
                {
                    positiveVec[wordWeightPair.Key] = 0;
                }
                else
                {
                    positiveVec[wordWeightPair.Key] *= idf;
                }
            }

            foreach(var wordWeightPair in negativeVec)
            {
                var idf = Math.Log2(
                    documentsCatalogueLength / 
                    modifiedDocumentsWithWords[wordWeightPair.Key].Count
                );

                if (idf == double.PositiveInfinity)
                {
                    negativeVec[wordWeightPair.Key] = 0;
                }
                else
                {
                    negativeVec[wordWeightPair.Key] *= idf;
                }
            }

            var weightsInQuery = CalculateVector(originalQueryWeights, 1, CalculateVector(positiveVec, 1, negativeVec, -1), 1)
                .Where(x => x.Value > 0.1)
                .ToDictionary(x => x.Key, x => x.Value);

            modifiedDocumentsWithWords = modifiedDocumentsWithWords.Where(x => weightsInQuery.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);

            var weightsInDocuments = CalculateWeightsOfDocuments(modifiedDocumentsWithWords);

            return CalculateSimilarity(weightsInDocuments, weightsInQuery, documentsCatalogueLength);
        }

        private Dictionary<string, double> CalculateVector(Dictionary<string, double> a, double multiplierA, Dictionary<string, double> b, double multiplierB)
        {
            var resultVec = new Dictionary<string, double>();

            foreach(var word in a)
            {
                double result = word.Value * multiplierA + b.GetValueOrDefault(word.Key) * multiplierB;
                resultVec.Add(word.Key, result);
            }

            foreach(var word in b)
            {
                if(!resultVec.ContainsKey(word.Key))
                {
                    double result = word.Value * multiplierB;
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

        /*
         * The algorithm returns a dictionary that contains all the documents from the inverted index with the given words
         */
        private Dictionary<string, List<QueryInformation>> GetDocumentsFromInverseIndex(IEnumerable<string> words)
        {
            Dictionary<string, List<QueryInformation>> queryResult = new Dictionary<string, List<QueryInformation>>(words.Count());

            foreach (var word in words)
            {
                DatabaseEntry key = new DatabaseEntry(Encoding.UTF8.GetBytes(word));
                queryResult.Add(word, new List<QueryInformation>());
                if (hashDatabase.Exists(key))
                {
                    byte[] buffer = hashDatabase.Get(key).Value.Data;
                    long[] values = new long[buffer.Length / sizeof(long)];
                    Buffer.BlockCopy(buffer, 0, values, 0, buffer.Length);
                    //Document document = this.documentsCatalogue.SearchInDatabase(values[0]);
                    QueryInformation queryInformation = new QueryInformation()
                    {
                        DocumentId = values[0],
                        Poss = new List<long>()
                    };
                    queryInformation.Poss.Add(values[1]);
                    for (long j = 2; j < values.Length; j += 2)
                    {
                        if (values[j] != values[j - 2]) // If different document
                        {
                            queryResult[word].Add(queryInformation);
                            //document = this.documentsCatalogue.SearchInDatabase(values[j]);
                            queryInformation = new QueryInformation()
                            {
                                DocumentId = values[j],
                                Poss = new List<long>()
                            };
                        }
                        queryInformation.Poss.Add(values[j + 1]);
                    }
                    queryResult[word].Add(queryInformation);
                }
            }
            return queryResult;
        }

        /*
         * The algorithm calculates and returns the weights in the query using the vector space model.
         */
        private Dictionary<string, double> CalculateWeightsOfQuery(
            Dictionary<string, long> query,
            Dictionary<string, List<QueryInformation>> documentsWithWords,
            long documentsCatalogueLength
        )
        {
            Dictionary<string, double> weightsInQuery = new Dictionary<string, double>();
            long i = 0;
            foreach (KeyValuePair<string, long> word in query)
            {
                var idf = Math.Log2(documentsCatalogueLength / (double)documentsWithWords[word.Key].Count);
                if (idf == double.PositiveInfinity)
                {
                    weightsInQuery.Add(word.Key, 0);
                }
                else
                {
                    var tfidf = (word.Value / (double)query.Values.Max()) * idf;
                    weightsInQuery.Add(word.Key, tfidf);
                }
                i++;
            }
            return weightsInQuery;
        }

        /*
         * The algorithm calculates and returns the weights in documents using the vector space model.
         */
        private Dictionary<QueryInformation, List<double>> CalculateWeightsOfDocuments(Dictionary<string, List<QueryInformation>> documentsWithWords)
        {
            var weightsInDocuments = new Dictionary<QueryInformation, List<double>>();

            var maxFreqCache = new Dictionary<long, long>(); // documentId -> max freq

            foreach (var documentsWithWord in documentsWithWords)
            {
                foreach (QueryInformation document in documentsWithWord.Value)
                {
                    long maxFreq = 0;
                    if (!maxFreqCache.TryGetValue(document.DocumentId, out maxFreq))
                    {
                        DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(document.DocumentId));
                        var rawData = maxFreqDatabase.Get(key).Value.Data;
                        maxFreq = BitConverter.ToInt64(rawData);
                        maxFreqCache.Add(document.DocumentId, maxFreq);
                    }
                    var tf = document.Poss.Count / (double)maxFreq;

                    if (weightsInDocuments.TryGetValue(document, out var weight))
                    {
                        weight.Add(tf);
                    }
                    else
                    {
                        weightsInDocuments.Add(
                            document,
                            new List<double>() { tf }
                        );
                    }
                }
            }
            return weightsInDocuments;
        }

        /*
         * The algorithm calculates the similarity in the query and documents using the vector space model.
         * The function returns a documents list with the biggest similarity.
         */
        private List<RetrievedDocument> CalculateSimilarity(Dictionary<QueryInformation, List<double>> weightsInDocuments, Dictionary<string, double> weightsInQuery, long documentsCatalogueLength)
        {     
            List<dynamic> retrievedDocumentIds = new List<dynamic>();

            foreach (KeyValuePair<QueryInformation, List<double>> weightD in weightsInDocuments)
            {
                double dotProduct = weightD.Value.Zip(weightsInQuery, (d1, d2) => d1 * d2.Value).Sum();
                double vecLength = Math.Sqrt(weightD.Value.Select(x => x*x).Sum()) + Math.Sqrt(weightsInQuery.Select(x => x.Value*x.Value).Sum());

                double similarity;
                if(vecLength == 0) // Check is required because dotProcuct / vectorLength could lead to a division by zero when word exists in every document
                {
                    similarity = 0;
                } 
                else
                {
                   similarity = dotProduct / vecLength;
                }

                
                retrievedDocumentIds.Add(new 
                {
                    DocumentId = weightD.Key.DocumentId,
                    Similarity = similarity
                });
            }

            

            return retrievedDocumentIds.OrderByDescending(o => o.Similarity).Take(10).Select(x => 
                new RetrievedDocument()
                {
                    Document = this.documentsCatalogue.SearchInDatabase(x.DocumentId),
                    Similarity = x.Similarity
                }
            ).ToList();
        }
    }
}
