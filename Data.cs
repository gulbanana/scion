using System;
using System.IO;

namespace Scion
{
    class Data
    {
        private readonly string directory;
        private readonly DateTime? baseDate;

        private string outputFile => Path.Combine(directory, "output.txt");

        public Data(string directory, DateTime? baseDate)
        {
            this.directory = directory;
            this.baseDate = baseDate?.Date;
        }

        public void Check()
        {
            if (IsInited())
            {
                Console.WriteLine($"Using data directory: {directory}");
            }
            else
            {
                Init();
            }
        }

        private bool IsInited()
        {
            return Directory.Exists(directory) && File.Exists(outputFile);
        }

        private void Init()
        {
            Console.WriteLine($"Initialising data directory: {directory}");
            Directory.CreateDirectory(directory);
            File.Create(outputFile).Dispose();
        }

        public DateTime GetEarliestDate() 
        {
            return baseDate ?? throw new Exception("base date is currently required");
        }

        public void Write(DateTime latestRelease)
        {
            File.WriteAllText(outputFile, latestRelease.ToShortDateString());
        }
    }
}