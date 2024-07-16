using System;
using System.IO;
using System.Text.Json;

public static class ConfigManager
{
    private const string ConfigFilePath = "config.json";

    public static Config LoadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            CreateDefaultConfig();
        }

        string json = File.ReadAllText(ConfigFilePath);
        var config = JsonSerializer.Deserialize<Config>(json);
        var apiKey = config.ApiKey;//Environment.GetEnvironmentVariable(config.EnvPath);

        if (!string.IsNullOrEmpty(apiKey)) return new Config { ApiKey = apiKey };
        return config;
    }

    private static void CreateDefaultConfig()
    {
        var defaultConfig = new Config
        {
            ApiKey = "YOUR_API_KEY_HERE",
            EnvPath = "API_KEY"
        };

        string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
    }
}
