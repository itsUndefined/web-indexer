using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BerkeleyDB;
using InvertedIndex.Models;

namespace InvertedIndex.Services
{
    public class DocumentsCatalogue
    {
        private readonly HashDatabase hashDatabase;
        private readonly HashDatabase urlDatabase;

        /*
         * Configure and open hash database for documents.
         * Each document has a unique ID and is stored in the database with hash key the document's ID and value the document.
         */
        public DocumentsCatalogue(DatabaseEnvironment env)
        {
            /* Configure the database. */
            var hashDatabaseConfig = new HashDatabaseConfig
            {
                Duplicates = DuplicatesPolicy.NONE,
                Creation = CreatePolicy.IF_NEEDED,
                FreeThreaded = true,
            };

            /* Create the database if does not already exist and open the database file. */
            try
            {
                hashDatabase = HashDatabase.Open("documents_catalogue.db", hashDatabaseConfig);
                urlDatabase = HashDatabase.Open("visited_urls.db", hashDatabaseConfig);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }

        /*
         * Close the database.
         */
        ~DocumentsCatalogue()
        {
            hashDatabase.Close();
            hashDatabase.Dispose();
            urlDatabase.Close();
            urlDatabase.Dispose();
        }

        /*
         * Insert a document to the database.
         */
        public void InsertToDatabase(Document document)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(document.Id));
            DatabaseEntry value = new DatabaseEntry(document.GetByteArray()); 
            hashDatabase.Put(key, value);

            key = new DatabaseEntry(Encoding.UTF8.GetBytes(document.Url));
            value = new DatabaseEntry(BitConverter.GetBytes(document.Id));
            urlDatabase.Put(key, value);
        }

        /*
         * Get a document from the database based with the given document's ID.
         */
        public Document SearchInDatabaseById(long id)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(id));
            try {
                return new Document(hashDatabase.Get(key).Value.Data);
            } catch(NotFoundException) {
                return null;
            }
        }

        public long SearchInDatabaseByUrl(string url)
        {
            DatabaseEntry key = new DatabaseEntry(Encoding.UTF8.GetBytes(url));
            try {
                return BitConverter.ToInt64(urlDatabase.Get(key).Value.Data);
            } catch(NotFoundException) {
                return 0;
            }
        }

        public void Truncate()
        {
            hashDatabase.Truncate();
            urlDatabase.Truncate();
        }

        /*
         * Get the length of the whole database.
         */
        public long Length()
        {
            try
            {
                return BitConverter.ToInt64(hashDatabase.Get(new DatabaseEntry(BitConverter.GetBytes(0L))).Value.Data);
            } 
            catch(NotFoundException)
            {
                hashDatabase.Put(
                    new DatabaseEntry(BitConverter.GetBytes(0L)),
                    new DatabaseEntry(BitConverter.GetBytes(0L))
                );
                hashDatabase.Sync();
                return 0;
            } 
        }

        /*
         * Increases the length of the database.
         */
        public void IncrementLength()
        {
            hashDatabase.Put(
                new DatabaseEntry(BitConverter.GetBytes(0L)),
                new DatabaseEntry(BitConverter.GetBytes(this.Length() + 1))
            );
        }

        /*
         * Syncronizes the database with the data from RAM to HDD.
         */
        public void SyncToDisk()
        {
            hashDatabase.Sync();
            urlDatabase.Sync();
        }
    }
}
