using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BerkeleyDB;

namespace InvertedIndex
{
    public class InvertedIndex
    {
        private HashDatabase hashDatabase;

        public InvertedIndex()
        {
            hashDatabase = HashDatabase.Open("inverted_index.db", new HashDatabaseConfig());
            Console.WriteLine("DB created");
        }
    }
}
