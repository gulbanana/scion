using CommandLine;
using System;

namespace Scion
{
    class Options
    {
        [Option('d', "data")]
        public string? DataDirectory { get; set; }

        [Option('s', "source")]
        public Uri? SourceURL { get; set; }
    }
}
