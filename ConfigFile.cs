namespace Scion
{
    class ConfigFile
    {
        public string[] TagBlacklist { get; set; } = new string[] { "ecchi", "nsfw", "sex", "yuri" };
        public string[] TagWhitelist { get; set; } = new string[0];
    }
}