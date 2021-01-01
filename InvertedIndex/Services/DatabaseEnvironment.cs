using BerkeleyDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace InvertedIndex.Services
{
    public class DatabaseEnvironment
    {

        public BerkeleyDB.DatabaseEnvironment env;

        public DatabaseEnvironment()
        {


            var config = new DatabaseEnvironmentConfig()
            {
                UseLocking = false,
                UseLogging = true,
                UseMPool = true,
                UseTxns = false,
                Private = true,
                Create = true,
                FreeThreaded = true,
               // LockSystemCfg = new LockingConfig()
              //  {
               //     DeadlockResolution = DeadlockPolicy.MIN_WRITE
              //  }
            };
            //env = BerkeleyDB.DatabaseEnvironment.Open("database", config);
            /*new Thread(() =>
            {
                while (true)
                {
                    //Thread.Sleep(10000);
                    //var rejected = env.DetectDeadlocks(DeadlockPolicy.MIN_WRITE);
                    // Console.WriteLine("Rejected " + rejected + " locks");
                }

            }).Start(); */
        }

        ~DatabaseEnvironment()
        {
            try
            {
                //env.Close();
            }
            catch (DatabaseException e)
            {
                Console.WriteLine("Error closing env: " + e.ToString());
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
