using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Scion
{
    class Scraper
    {
        private readonly Uri sourceURL;
        private readonly IConfiguration config;

        public Scraper(Uri sourceURL)
        {
            this.sourceURL = sourceURL;
            config = Configuration.Default.WithDefaultLoader();
        }

        public async Task<IReadOnlyList<Chapter>> GetIndexChapters(DateTime earliestDate, CancellationToken ct)
        {
            Console.WriteLine($"Reading {sourceURL} index from page 1 back to {earliestDate.ToShortDateString()}");

            var context = BrowsingContext.New(config);

            var chapters = new List<Chapter>();
            var pageNumber = 1;
            var pageChapters = new List<Chapter>();

            do 
            {
                var pageURL = new Uri(sourceURL, $"/?page={pageNumber++}");                
                var document = await context.OpenAsync(pageURL.ToString(), ct);            
                ct.ThrowIfCancellationRequested();

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
                        try
                        {
                            var chapter = ParseIndexChapter(currentDate, tag);
                            pageChapters.Add(chapter);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine("Parse failed. HTML and exception follow:");
                            Console.Error.WriteLine(tag.ToHtml());
                            Console.Error.WriteLine(e.ToString());
                        }
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

        private Chapter ParseIndexChapter(DateTime? date, IElement block)
        {
            if (!date.HasValue)
            {
                throw new Exception("found chapter before <h4>Date</h4>");
            }

            if (!(block is IHtmlAnchorElement anchor))
            {
                throw new Exception($"expected <a class='chapter'>, found {block.ToString()}");
            }

            var doujin = block.QuerySelector(".title .doujins")?.TextContent;
            if (doujin != null)
            {
                doujin = doujin.Substring(0, doujin.Length - " Doujin".Length);
            }

            var title = block.QuerySelector(".title").ChildNodes.First().TextContent.Trim();
            
            var series = default(string?);
            var subtitle = default(string?);
            var regularSeries = Regex.Match(title, @"(.*?) ch([^: ]*)(: (.*))?");
            if (regularSeries.Success)
            {
                series = regularSeries.Groups[1].Value;
                subtitle = regularSeries.Groups[3].Success ? 
                    $"Chapter {decimal.Parse(regularSeries.Groups[2].Value)} - {regularSeries.Groups[4].Value}" :
                    $"Chapter {decimal.Parse(regularSeries.Groups[2].Value)}";
            }
            else
            {
                var irregularSeries = Regex.Match(title, @"(.*?): (.*)");
                if (irregularSeries.Success)
                {
                    series = irregularSeries.Groups[1].Value.Trim();
                    subtitle = irregularSeries.Groups[2].Value;
                }
            }

            return new Chapter
            {
                ReleaseDate = date.Value,
                Link = new Uri(sourceURL, anchor.Href),
                Thumbnail = new Uri(sourceURL, block.QuerySelector("img").GetAttribute("src")),
                Title = title,
                Authors = block.QuerySelector(".authors").TextContent.Trim(),
                Tags = block.QuerySelectorAll(".tags > .label").Select(t => t.TextContent).ToList(),
                Doujin = doujin,
                Series = series,
                Subtitle = subtitle?.Replace(":", " -")
            };
        }

        public async Task<IReadOnlyList<Chapter>> GetSeriesChapters(Chapter sampleChapter, CancellationToken ct)
        {
            Console.WriteLine($"Reading chapters from \"{sampleChapter.Series}\"");
            if (sampleChapter.Series == null) throw new ArgumentException(nameof(sampleChapter.Series));

            var context = BrowsingContext.New(config);

            var samplePage = await context.OpenAsync(sampleChapter.Link.ToString(), ct);
            ct.ThrowIfCancellationRequested();

            var seriesLink = samplePage.QuerySelector("#chapter-title > b > a").GetAttribute("href");

            var seriesPage = await context.OpenAsync(new Uri(sourceURL, seriesLink).ToString(), ct);
            ct.ThrowIfCancellationRequested();

            var seriesDoujin = seriesPage.QuerySelector(".doujin_tags > *")?.TextContent;          
            if (seriesDoujin != null) seriesDoujin = seriesDoujin.Substring(0, seriesDoujin.Length - " Doujin".Length);
            var seriesTags = seriesPage.QuerySelectorAll("#main > tag-tags > a").Select(t => t.TextContent).ToList();
            var chapterBlocks = seriesPage.QuerySelectorAll(".chapter-list > *");

            var chapters = new List<Chapter>();
            var currentVolume = default(string?);
            foreach (var tag in chapterBlocks)
            {
                // volume header
                if (tag.TagName == "DT")
                {
                    currentVolume = tag.TextContent;
                }

                // chapter within a volume
                else if (tag.TagName == "DD")
                {
                    var chapter = ParseSeriesChapter(tag, seriesDoujin, sampleChapter.Authors, sampleChapter.Series, currentVolume, sampleChapter.Thumbnail, seriesTags);
                    chapters.Add(chapter);
                }
            }

            return chapters;
        }

        private Chapter ParseSeriesChapter(IElement block, string? doujin, string authors, string series, string? volume, Uri thumbnail, IReadOnlyList<string> tags)
        {
            var date = block.QuerySelector("small").TextContent.Substring("released ".Length);
            var subtitle = block.QuerySelector(".name").TextContent;
            var title = subtitle.Contains(':') ? $"{series} {subtitle}" : $"{series}: {subtitle}";
            if (volume != null) subtitle = $"{volume} - {subtitle}";
            var link = block.QuerySelector(".name").GetAttribute("href");
            var extraTags = block.QuerySelectorAll(".tags > a").Select(t => t.TextContent);         

            return new Chapter
            {
                ReleaseDate = DateTime.ParseExact(date.Replace("'", ""), "MMM d y", null, DateTimeStyles.AllowInnerWhite),
                Link = new Uri(sourceURL, block.QuerySelector(".name").GetAttribute("href")),
                Thumbnail = thumbnail,
                Doujin = doujin,
                Authors = authors,
                Title = title,
                Series = series,
                Subtitle = subtitle.Replace(":", " -"),
                Tags = tags.Concat(extraTags).ToList()
            };
        }
    }
}