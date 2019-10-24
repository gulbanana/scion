using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Scion
{
    class Data
    {
        private readonly JsonSerializerOptions serializerOptions;
        private readonly string directory;
        private readonly DateTime? baseDate;

        private string configFile => Path.Combine(directory, "config.json");
        private ConfigFile config;

        public Data(string directory, DateTime? baseDate)
        {
            serializerOptions = new JsonSerializerOptions 
            {
                 WriteIndented = true,
                 PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            this.directory = directory;
            this.baseDate = baseDate?.Date;
        }

        public ConfigFile Load()
        {
            if (IsInited())
            {
                Console.WriteLine($"Using data directory: {directory}");
            }
            else
            {
                Init();
            }

            // load and normalise config
            config = JsonSerializer.Deserialize<ConfigFile>(File.ReadAllText(configFile), serializerOptions);
            File.WriteAllText(configFile, JsonSerializer.Serialize(config, serializerOptions));
            return config;
        }

        private bool IsInited()
        {
            return Directory.Exists(directory) 
                && File.Exists(configFile);
        }

        private void Init()
        {
            Console.WriteLine($"Initialising data directory: {directory}");
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(configFile))
            {
                var defaultConfig = JsonSerializer.Serialize(new ConfigFile(), serializerOptions);
                File.WriteAllText(configFile, defaultConfig);
            }
        }

        public DateTime GetEarliestDate() 
        {
            return baseDate ?? throw new Exception("base date is currently required");
        }

        public bool HasChapter(Chapter chapter)
        {
            var (containerPath, filePath) = GetLocation(chapter);
            var filename = Path.GetFileName(filePath);

            return File.Exists(filePath) || 
                   Directory.Exists(containerPath) && 
                   Directory.GetFiles(containerPath).Any(f => f.EndsWith(filename) && 
                                                              Path.GetFileName(f).StartsWith("Volume"));
        }

        public void WriteChapter(Chapter chapter)
        {
            var (containerPath, filePath) = GetLocation(chapter);
            
            if (!Directory.Exists(containerPath))
            {
                Directory.CreateDirectory(containerPath);
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Writing file: {filePath}");
                File.WriteAllText(filePath, "");
                File.SetCreationTime(filePath, chapter.ReleaseDate);
                File.SetLastWriteTime(filePath, chapter.ReleaseDate);
                
                var firstRelease = Directory.GetFiles(containerPath).Select(File.GetCreationTime).Min();
                var lastRelease = Directory.GetFiles(containerPath).Select(File.GetCreationTime).Max();
                Directory.SetCreationTime(containerPath, firstRelease);
                Directory.SetLastWriteTime(containerPath, lastRelease);

                var parentContainer = Directory.GetParent(containerPath).FullName;
                var firstContainer = Directory.GetDirectories(parentContainer).Select(Directory.GetCreationTime).Min();
                var lastContainer = Directory.GetDirectories(parentContainer).Select(Directory.GetLastWriteTime).Max();
                Directory.SetCreationTime(parentContainer, firstContainer);
                Directory.SetLastWriteTime(parentContainer, lastContainer);
            }
            else
            {
                Console.Error.WriteLine($"Skipping write: {filePath} already exists");
            }
        }

        private (string containerPath, string filePath) GetLocation(Chapter chapter)
        {
            var container = chapter.Series != null
                ? $"{chapter.Authors} - {chapter.Series}"
                : $"{chapter.Authors}";

            var filename = chapter.Series != null
                ? chapter.Subtitle ?? "Chapter Title Missing"
                : chapter.Title;

            var containerPath = chapter.Doujin != null
                ? Path.Combine(directory, Sanitise(chapter.Doujin), Sanitise(container))
                : Path.Combine(directory, Sanitise(container));
            
            var filePath = Path.Combine(containerPath, Sanitise(filename));

            return (containerPath, filePath);
        }

        private string Sanitise(string input)
        {
            return input
                .Replace('\\', '＼')
                .Replace('/', '／')
                .Replace(':', '：')
                .Replace('*', '⋆')
                .Replace('?', '？')
                .Replace('"', '\'')
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('|', '_')
                .TrimEnd('.');
        }
    }
}