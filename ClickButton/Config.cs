using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

public class Config
{
    public Keys Keybind { get; set; } = Keys.Tab;
    public bool ShowIndicator { get; set; } = false;

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClumsyController",
        "config.xml"
    );

    public static Config Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                return SaveDefault();
            }

            using (var stream = File.OpenRead(ConfigPath))
            {
                var serializer = new XmlSerializer(typeof(Config));
                return (Config)serializer.Deserialize(stream);
            }
        }
        catch (Exception)
        {
            return SaveDefault();
        }
    }

    public void Save()
    {
        try
        {
            string dirPath = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            using (var stream = File.Create(ConfigPath))
            {
                var serializer = new XmlSerializer(typeof(Config));
                serializer.Serialize(stream, this);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save config: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Config SaveDefault()
    {
        var config = new Config();
        config.Save();
        return config;
    }
}