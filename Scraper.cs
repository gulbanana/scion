using AngleSharp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Scion
{
    class Scraper
    {
        private readonly Uri sourceURL;

        public Scraper(Uri sourceURL)
        {
            this.sourceURL = sourceURL;
        }

        public async Task<DateTime> GetLatestRelease()
        {
            Console.WriteLine($"Scraping {sourceURL}");

            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(sourceURL.ToString());
            var leftColumn = document.QuerySelector("#main .span8");
            var dates = leftColumn.QuerySelectorAll("h4").Select(heading => DateTime.Parse(heading.TextContent));

            foreach (var date in dates)
            {
                Console.WriteLine($"Found chapters for {date.ToShortDateString()}");
            }

            return dates.First();
        }
    }
}