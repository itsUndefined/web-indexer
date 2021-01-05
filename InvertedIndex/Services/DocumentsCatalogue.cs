using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using BerkeleyDB;
using InvertedIndex.Models;

namespace InvertedIndex.Services
{
    public class DocumentsCatalogue
    {
        private readonly HashDatabase hashDatabase;
        private readonly HashDatabaseConfig hashDatabaseConfig;
        private readonly string dbFileName = "documents_catalogue.db";

        /*
         * Configure and open hash database for documents.
         * Each document has a unique ID and is stored in the database with hash key the document's ID and value the document.
         */
        public DocumentsCatalogue(DatabaseEnvironment env)
        {
            /* Configure the database. */
            hashDatabaseConfig = new HashDatabaseConfig
            {
                Duplicates = DuplicatesPolicy.NONE,
                Creation = CreatePolicy.IF_NEEDED,
                FreeThreaded = true,
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

        /*
         * Close the database.
         */
        ~DocumentsCatalogue()
        {
            hashDatabase.Close();
            hashDatabase.Dispose();
        }

        /*
         * Insert a document to the database.
         */
        public void InsertToDatabase(long documentId, Document document)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(documentId));
            if (!hashDatabase.Exists(key))
            {
                DatabaseEntry value = new DatabaseEntry(document.GetByteArray()); 
                hashDatabase.Put(key, value);

            }
        }

        /*
         * Get a document from the database using the document's ID.
         */
        public Document SearchInDatabase(long id)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(id));
            return new Document(id, hashDatabase.Get(key).Value.Data);
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
         * Increases the database's length.
         */
        public void IncrementLength()
        {
            hashDatabase.Put(
                new DatabaseEntry(BitConverter.GetBytes(0L)),
                new DatabaseEntry(BitConverter.GetBytes(this.Length() + 1))
            );
        }

        /*
         * Synchronizes the database data (from RAM to HDD).
         */
        public void SyncToDisk()
        {
            hashDatabase.Sync();
        }
    }
}
