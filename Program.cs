using CommandLine;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Scion
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args).MapResult(Run, _ => Task.CompletedTask);
        }

        static async Task Run(Options options)
        {
            if (options.DataDirectory == null)
            {
                Console.Error.WriteLine("Set SCION_DATA or pass -d");
                Environment.Exit(0);
            }

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {                
                if (!cts.IsCancellationRequested)
                {
                    Console.WriteLine("Requesting cancellation...");
                    cts.Cancel();
                }
                e.Cancel = true;
            };

            try
            {            
                var source = new Scraper(options.SourceURL);
                var data = new Data(options.DataDirectory, options.BaseDate);
                var config = data.Load();

                var chapters = await source.GetIndexChapters(data.GetEarliestDate(), cts.Token);

                foreach (var chapter in chapters.Where(Filter(config)).OrderBy(c => c.ReleaseDate))
                {
                    if (!data.HasChapter(chapter))
                    {
                        // XXX download images
                        if (chapter.Series != null)
                        {
                            var allSeriesChapters = await source.GetSeriesChapters(chapter, cts.Token);
                            foreach (var seriesChapter in allSeriesChapters)
                            {
                                data.WriteChapter(seriesChapter);
                            }
                        }
                        else
                        {
                            data.WriteChapter(chapter);
                        }
                    }
                }

                Console.WriteLine("Done.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancelled.");
            }
        }

        static Func<Chapter, bool> Filter(ConfigFile config) => chapter =>
        {
            var section = chapter.Doujin == null ? config.Manga : config.Doujinshi;

            if (!section.Include)
            {
                return false;
            }

            if (section.Whitelist.Any())
            {
                var match = false;

                foreach (var tag in section.Whitelist)
                {
                    match = match || chapter.Tags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase));
                    match = match || chapter.Authors.Equals(tag, StringComparison.OrdinalIgnoreCase);
                    match = match || (chapter.Doujin?.Equals(tag, StringComparison.OrdinalIgnoreCase) ?? false);
                    match = match || (chapter.Series?.Equals(tag, StringComparison.OrdinalIgnoreCase) ?? false);
                }
                
                if (!match) return false;
            }

            foreach (var tag in section.Blacklist)
            {
                var match = false;

                match = match || chapter.Tags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase));
                match = match || chapter.Authors.Equals(tag, StringComparison.OrdinalIgnoreCase);
                match = match || (chapter.Doujin?.Equals(tag, StringComparison.OrdinalIgnoreCase) ?? false);
                match = match || (chapter.Series?.Equals(tag, StringComparison.OrdinalIgnoreCase) ?? false);

                if (match) return false;
            }
            
            return true;
        };
    }
}
