using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvertedIndex
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebsiteInformation w1 = new WebsiteInformation(0, "D1", "url0", "i am running free again");
            WebsiteInformation w2 = new WebsiteInformation(1, "D2", "url1", "free free set them free");
            WebsiteInformation w3 = new WebsiteInformation(2, "D3", "url2", "running in the night");
            WebsiteInformation w4 = new WebsiteInformation(3, "D4", "url3", "totally irrelevent");
            InvertedIndex i = new InvertedIndex();
            i.InsertToDatabase(w1);
            i.InsertToDatabase(w2);
            i.InsertToDatabase(w3);
            i.InsertToDatabase(w4);
            QueryResult[] q = i.SearchInDatabase("running free");
            foreach (QueryResult res in q)
            {
                Console.WriteLine("{0}:", res.word);
                foreach (QueryInformation inf in res.documentsList)
                {
                    Console.WriteLine("Title: {0}, URL: {1}", inf.title, inf.url);
                }
            }
            //CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
