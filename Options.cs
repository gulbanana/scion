using CommandLine;
using System;

namespace Scion
{
    class Options
    {
        [Option('d', "data", HelpText = "Data directory - defaults to SCION_DATA")]
        public string? DataDirectory { get; set; } = System.Environment.GetEnvironmentVariable("SCION_DATA");

        [Option('s', "source", HelpText = "Source URL - defaults to Dynasty Scans")]
        public Uri SourceURL { get; set; } = new Uri("https://dynasty-scans.com/");

        [Option('b', "base", HelpText = "Base date to scan from - set if no downloads have been done or to go back in time")]
        public DateTime? BaseDate { get; set; }
    }
}
