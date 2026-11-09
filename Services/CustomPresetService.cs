
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LumaCtrl.Services
{
    public class CustomPresetService
    {
        public const int CustomPresetCount = 5;

        private readonly string _filePath;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public CustomPresetService()
        {
            string appDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

            string appFolder = Path.Combine(
                appDataPath,
                "LumaCtrl");

            Directory.CreateDirectory(appFolder);

            _filePath = Path.Combine(
                appFolder,
                "custom-presets.json");
        }

        public string?[] LoadPresets()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return CreateEmptyPresets();

                string json = File.ReadAllText(_filePath);

                string?[]? presets =
                    JsonSerializer.Deserialize<string?[]>(json);

                if (presets == null)
                    return CreateEmptyPresets();

                return Enumerable.Range(0, CustomPresetCount)
                    .Select(i => i < presets.Length ? presets[i] : null)
                    .ToArray();
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is JsonException)
            {
                return CreateEmptyPresets();
            }
        }

        public void SavePresets(string?[] presets)
        {
            ArgumentNullException.ThrowIfNull(presets);

            if (presets.Length != CustomPresetCount)
                throw new ArgumentException(
                    "Exactly five custom presets are required.",
                    nameof(presets));

            string json = JsonSerializer.Serialize(
                presets,
                JsonOptions);

            File.WriteAllText(_filePath, json);
        }

        private static string?[] CreateEmptyPresets()
        {
            return new string?[CustomPresetCount];
        }
    }
}
