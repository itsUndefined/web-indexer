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

        public DocumentsCatalogue()
        {
            /* Configure the database. */
            hashDatabaseConfig = new HashDatabaseConfig
            {
                Duplicates = DuplicatesPolicy.NONE,
                Creation = CreatePolicy.IF_NEEDED,
                FreeThreaded = true
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

        ~DocumentsCatalogue()
        {
            hashDatabase.Close();
            hashDatabase.Dispose();
        }

        public void InsertToDatabase(long documentId, Document document)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(documentId));
            if (!hashDatabase.Exists(key))
            {
                DatabaseEntry value = new DatabaseEntry(document.GetByteArray());
                
                try
                {
                    hashDatabase.Get(key);
                    throw new Exception("Concurrency error. Tried to write two documents with same ID");
                }
                catch(NotFoundException)
                {

                }
                
                hashDatabase.Put(key, value);
                hashDatabase.Put(new DatabaseEntry(BitConverter.GetBytes(0L)), new DatabaseEntry(BitConverter.GetBytes(this.Length() + 1)));
               // hashDatabase.Sync();
            }
        }

        public Document SearchInDatabase(long id)
        {
            DatabaseEntry key = new DatabaseEntry(BitConverter.GetBytes(id));
            return new Document(hashDatabase.Get(key).Value.Data);
        }

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
    }
}
