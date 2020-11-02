using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

namespace UrlCrawler
{
    class UrlCrawler
    {
        public ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _crawlResults;
        public String _rootUrl;
        public String _domainName;
        public bool _debugRun;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rootUrl">Root URL to start crawling from</param>
        public UrlCrawler( string rootUrl, bool debugRun=false)
        {
            //Crawl Results
            _crawlResults = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
            
            _rootUrl = rootUrl;

            //Domain name to make sure we do not crawl outside it
            _domainName = new Uri( _rootUrl).Authority;

            _debugRun = debugRun;
        }

        /// <summary>
        /// Main async function producing crawling results 
        /// </summary>
        /// <returns>Crawl results as ConcurrentDictionary of URLs where each value points to ConcurrentDictionary of LINKs/IMGs </returns>
        public async Task<ConcurrentDictionary<string, ConcurrentDictionary<string, string>>> Crawl()
        {
            //Scrape root URl - first/once - produce all LINKs from the root URL
            var rootLinks = await ScrapeCurrentUrl(_rootUrl);

            var R = rootLinks;

            //Keep crawling the links until the R.Count() is zerro
            do
            {
                R = await CrawlUrlLinks(R);
            } 
            while (R.Count() > 0);

            return _crawlResults;
        }

        /// <summary>
        /// Crawls URL Links by async starting all threads to Scrape corresponding pages
        /// </summary>
        /// <param name="currentUrlLinks">URLs to crawl</param>
        /// <returns>Distinct list of outstanding LINKs from the current run</returns>
        private async Task<IEnumerable<string>> CrawlUrlLinks(IEnumerable<string> currentUrlLinks)
        {
            var result = await Task.WhenAll(currentUrlLinks.Select(url => ScrapeCurrentUrl(url)));

            return result.SelectMany(x => x).Distinct();
        }

        /// <summary>
        /// Scrapes current URL to find all the LINKS/IMGs
        /// </summary>
        /// <param name="currentUrl">Current YRL</param>
        /// <returns>List of LINKs/IMGs from the current URL </returns>
        protected async Task<IEnumerable<string>> ScrapeCurrentUrl(string currentUrl)
        {
            ConcurrentDictionary<string, string> currentUrlScrapeRezults = new ConcurrentDictionary<string, string>();

            try
            {
                //Do not crowl the same page twice
                if (true == _crawlResults.ContainsKey(currentUrl))
                    throw new Exception("This Url is already acounted for");


                //var h = new Uri(currentUrl).Authority;
                //var x = h.Split('.');
                //if (x.Length > 2)
                //    h = x[1];

                //Scan for doman name in the current URL
                var urlDomainName = ScanUrlDomainName(currentUrl);

                //Do not crawl Urls from domains other than base
                if ( ! _domainName.Contains(urlDomainName) )
                    throw new Exception("This Url is not from the base domain");

                //Create this object to pars HTML
                HtmlWeb web = new HtmlWeb();
                var htmlDocument = await web.LoadFromWebAsync(currentUrl);

                //Find all href attributes and provide the list 
                var currentUrlLinks = htmlDocument.DocumentNode
                                   .Descendants("a")
                                   .Select(a => a.GetAttributeValue("href", null))
                                   .Where(u => !string.IsNullOrEmpty(u) )
                                   .Distinct();

                //Persist all LINKs found on the current URL 
                foreach (string link in currentUrlLinks)
                {
                    //Scan for doman name in the current URL
                    urlDomainName = ScanUrlDomainName(link);

                    //urlDomainName = new Uri(link).Authority;
                    //x = urlDomainName.Split('.');
                    //if (x.Length > 2)
                    //    urlDomainName = x[1];
                    
                    //Do not persist LINKs from other domains
                    if ((!_domainName.Contains(urlDomainName)) || false == currentUrlScrapeRezults.TryAdd(link, "LINK") ) 
                        continue;
                }

                //Find all img attributes and provide a list
                var imgList = htmlDocument.DocumentNode.Descendants("img").Select(img => img.GetAttributeValue("src", null)).Where(u => !string.IsNullOrEmpty(u)).Distinct();

                //Persist all IMGs into a corresponding ConcurrentDictionary
                foreach (string img in imgList)
                {
                    if (false == currentUrlScrapeRezults.TryAdd(img, "IMG"))
                        continue;
                }

                //if (currentUrl.Contains("twitter") || currentUrl.Contains("facebook") || currentUrl.Contains("linkedin"))
                //    debugInt++;

                //Add current Url scrape results to the main dictionary
                _crawlResults.TryAdd(currentUrl, currentUrlScrapeRezults);

                if (_debugRun)
                {
                    Console.WriteLine("URL: " + currentUrl);
                    foreach (KeyValuePair<string, string> link in currentUrlScrapeRezults.ToList())
                        Console.WriteLine(string.Format($"   {link.Value} : {link.Key}"));
                }

                //Return LINKs/IMGs from the curreent URL
                return currentUrlLinks;
            }
            catch (Exception ex)
            {
                if ( _debugRun )
                    Console.WriteLine(ex.Message);

                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Helper function scanning a URl to find a domain name
        /// </summary>
        /// <param name="currentUrl">URL to be scannned</param>
        /// <returns>Domain name</returns>
        private string ScanUrlDomainName(string currentUrl)
        {
            var h = new Uri(currentUrl).Authority;
            var x = h.Split('.');
            if (x.Length > 2)
                h = x[1];

            return h;
        }

    }
}
