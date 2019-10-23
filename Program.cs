using CommandLine;
using System;
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

            var data = new Data(options.DataDirectory, options.BaseDate);
            data.Check();
            var source = new Scraper(options.SourceURL);
            var latestRelease = await source.GetLatestRelease(data.GetEarliestDate());
            data.Write(latestRelease);
        }
    }
}
