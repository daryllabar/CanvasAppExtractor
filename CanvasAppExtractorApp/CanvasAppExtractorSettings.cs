namespace CanvasAppExtractorApp
{
    public class CanvasAppExtractorSettings
    {
        public bool AutoNotify { get; set; }

        public Dictionary<string, string> ExtractDirStartsWithMapping { get; set; } = new();

        private string _pacPath = string.Empty;
        public string PacPath
        {
            get => _pacPath;
            set => _pacPath = ReplaceEnvironmentTokens(value);
        }

        private string ReplaceEnvironmentTokens(string input)
        {
            foreach (Environment.SpecialFolder folder in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {
                var token = $"{{{folder}}}";
                if (input.Contains(token))
                {
                    input = input.Replace(token, Environment.GetFolderPath(folder));
                }
            }
            return input;
        }
    }
}
