using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
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

        public async Task<IReadOnlyList<Chapter>> GetChaptersFrom(DateTime earliestDate)
        {
            Console.WriteLine($"Scraping {sourceURL} for chapters from {earliestDate.ToShortDateString()}");

            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);

            var chapters = new List<Chapter>();
            var pageNumber = 1;
            var pageChapters = new List<Chapter>();

            do 
            {
                var pageURL = new Uri(sourceURL, $"/?page={pageNumber++}");
                
                var document = await context.OpenAsync(pageURL.ToString());            
                var leftColumn = document.QuerySelectorAll(":not(.span4) > h4, :not(.span4) > a.chapter");

                var currentDate = default(DateTime?);
                pageChapters.Clear();                            
                foreach (var tag in leftColumn)
                {
                    // block of thumbnails for a given day
                    if (tag is IHtmlHeadingElement)
                    {
                        currentDate = DateTime.Parse(tag.TextContent);
                        if (currentDate < earliestDate) 
                        {
                            break;
                        }
                    }

                    // thumbnail within a day
                    else if (tag.ClassList.Contains("chapter"))
                    {
                        var chapter = ParseChapter(currentDate, tag);
                        pageChapters.Add(chapter);
                    }
                }

                if (pageChapters.Any())
                {
                    var pageDates = pageChapters.Select(c => c.ReleaseDate).ToList();
                    Console.WriteLine($"{pageURL}: {pageChapters.Count} chapters ({pageDates.Max().ToShortDateString()} - {pageDates.Min().ToShortDateString()})");
                    chapters.AddRange(pageChapters);
                }
            } while (pageChapters.Count > 0);
            
            return chapters;
        }

        private Chapter ParseChapter(DateTime? date, IElement block)
        {
            if (!date.HasValue)
            {
                throw new Exception("found chapter before <h4>Date</h4>");
            }

            if (!(block is IHtmlAnchorElement anchor))
            {
                throw new Exception($"expected <a class='chapter'>, found {block.ToString()}");
            }

            return new Chapter
            {
                ReleaseDate = date.Value,
                Link = new Uri(sourceURL, anchor.Href),
                Thumbnail = new Uri(sourceURL, block.QuerySelector("img").GetAttribute("src")),
                Title = block.QuerySelector(".title").ChildNodes.First().TextContent.Trim(), // ignoring doujins
                Authors = block.QuerySelector(".authors").TextContent,
                Tags = block.QuerySelectorAll(".tags > .label").Select(t => t.TextContent).ToList()
            };
        }
    }
}