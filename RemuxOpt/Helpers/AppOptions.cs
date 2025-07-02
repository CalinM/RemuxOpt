using System.Xml.Linq;

namespace RemuxOpt
{
    public class AppOptions
    {
        private const string _configPath = "config.xml"; // adjust path as needed

        public bool ReadFilesRecursively { get; set; } = false;
        public bool DeleteOriginalsAfterSuccessfulRemux { get; set; } = false;
        public bool RemoveUnlistedLanguageTracks { get; set; } = false;
        public bool ApplyNamingConventions { get; set; } = false;

        public AppOptions()
        {
            LoadOptions();
        }

        /// <summary>
        /// Load window size, state, and position 
        /// </summary>
        /// <param name="form"></param>
        public void LoadFormSettings(Form form)
        {
            if (File.Exists("config.xml"))
            {
                var doc = XDocument.Load("config.xml");
                var formSettings = doc.Root?.Element("FormSettings");

                if (formSettings != null)
                {
                    form.Width = int.Parse(formSettings.Attribute("Width")?.Value ?? "800");
                    form.Height = int.Parse(formSettings.Attribute("Height")?.Value ?? "600");
                    form.Left = int.Parse(formSettings.Attribute("Left")?.Value ?? "100");
                    form.Top = int.Parse(formSettings.Attribute("Top")?.Value ?? "100");

                    if (Enum.TryParse(formSettings.Attribute("WindowState")?.Value, out FormWindowState state))
                    {
                        form.WindowState = state;
                    }
                }
            }
        }
        
        /// <summary>
        ///  Save window size, state, and position            
        /// </summary>
        /// <param name="form"></param>
        public void SaveFormSettings(Form form)
        {
            var doc = File.Exists("config.xml")
                ? XDocument.Load("config.xml")
                : new XDocument(new XElement("Configuration"));

            var formSettings = new XElement("FormSettings",
                new XAttribute("Width", form.Width),
                new XAttribute("Height", form.Height),
                new XAttribute("Left", form.Left),
                new XAttribute("Top", form.Top),
                new XAttribute("WindowState", form.WindowState.ToString())
            );

            doc.Root?.Element("FormSettings")?.Remove(); // Remove old settings
            doc.Root?.Add(formSettings);
            doc.Save("config.xml");
        }

        public void LoadOptions()
        {
            if (!File.Exists(_configPath))
                return;

            XDocument doc = XDocument.Load(_configPath);
            XElement options = doc.Root.Element("Options");
            if (options != null)
            {
                foreach (var option in options.Elements("Option"))
                {
                    string name = option.Attribute("Name")?.Value;
                    string value = option.Attribute("Value")?.Value;

                    if (name == "ReadFilesRecursively" && bool.TryParse(value, out bool readRecursive))
                        ReadFilesRecursively = readRecursive;

                    if (name == "DeleteOriginalsAfterSuccessfulRemux" && bool.TryParse(value, out bool deleteOriginals))
                        DeleteOriginalsAfterSuccessfulRemux = deleteOriginals;

                    if (name == "RemoveUnlistedLanguageTracks" && bool.TryParse(value, out bool removeUnlistedLanguageTracks))
                        RemoveUnlistedLanguageTracks = removeUnlistedLanguageTracks;
                
                    if (name == "ApplyNamingConventions" && bool.TryParse(value, out bool applyNamingConventions))
                        ApplyNamingConventions = applyNamingConventions;
                }
            }
        }

        public void SaveOptions()
        {
            XDocument doc = File.Exists(_configPath)
                ? XDocument.Load(_configPath)
                : new XDocument(new XElement("Configuration"));

            XElement root = doc.Root;

            // Remove old Options node if exists
            root.Element("Options")?.Remove();

            XElement options = new XElement("Options",
                new XElement("Option",
                    new XAttribute("Name", "ReadFilesRecursively"),
                    new XAttribute("Value", ReadFilesRecursively.ToString().ToLower())),
                new XElement("Option",
                    new XAttribute("Name", "DeleteOriginalsAfterSuccessfulRemux"),
                    new XAttribute("Value", DeleteOriginalsAfterSuccessfulRemux.ToString().ToLower())),
                new XElement("Option",
                    new XAttribute("Name", "RemoveUnlistedLanguageTracks"),
                    new XAttribute("Value", RemoveUnlistedLanguageTracks.ToString().ToLower())),
                new XElement("Option",
                    new XAttribute("Name", "ApplyNamingConventions"),
                    new XAttribute("Value", ApplyNamingConventions.ToString().ToLower()))   
            );

            root.Add(options);
            doc.Save(_configPath);
        }
    }
}
