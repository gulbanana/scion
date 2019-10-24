namespace Scion
{
    class ConfigFile
    {
        public Section Manga { get; set; } = new Section
        {
            Include = true,
            Blacklist = new string[] { "ecchi", "nsfw", "sex", "yuri" }
        };

        public Section Doujinshi { get; set; } = new Section
        {
            Include = false,
            Whitelist = new string[] { "your ship here" }
        };

        public class Section
        {
            public bool Include { get; set; } = true;
            public string[] Blacklist { get; set; } = new string[0];
            public string[] Whitelist { get; set; } = new string[0];
        }
    }
}