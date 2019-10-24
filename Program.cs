using CommandLine;
using System;
using System.Linq;
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

            var source = new Scraper(options.SourceURL);
            var data = new Data(options.DataDirectory, options.BaseDate);
            var config = data.Load();
            
            var chapters = await source.GetChaptersFrom(data.GetEarliestDate());

            foreach (var chapter in chapters.Where(Filter(config)).OrderBy(c => c.ReleaseDate))
            {                
                Console.WriteLine(chapter);
            }
        }

        static Func<Chapter, bool> Filter(ConfigFile config) => chapter =>
        {
            if (config.TagWhitelist.Any())
            {
                if (!chapter.Tags.Any(t => config.TagWhitelist.Any(tag => t.Contains(tag, StringComparison.OrdinalIgnoreCase))))
                {
                    return false;
                }
            }

            foreach (var tag in config.TagBlacklist)
            {
                if (chapter.Tags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }
            
            return true;
        };
    }
}
