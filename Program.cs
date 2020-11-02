using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

namespace UrlCrawler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string rootUrl;

            if (args.Length == 0)
                rootUrl = "https://wiprodigital.com";
            else
                rootUrl = args[1];

            //Create the crawler object
            var urlCrawler = new UrlCrawler(rootUrl);


            //Start the crawl process - return results
            var crawlResults = await urlCrawler.Crawl();

            //Print Crawl Results
            foreach (string urlKey in crawlResults.Keys)
            {
                Console.WriteLine(string.Format($"Page: {urlKey}"));
                foreach ( KeyValuePair<string, string> pageLinks in crawlResults[urlKey])
                {
                    //foreach (KeyValuePair<string, string> link in pageLinks.ToList())
                        Console.WriteLine(string.Format($"   {pageLinks.Value} : {pageLinks.Key}"));

                    //Console.ReadKey();
                }

                Console.ReadKey();
            }
        }
    }
}
