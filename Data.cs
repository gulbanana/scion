using System;
using System.IO;
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
    }
}