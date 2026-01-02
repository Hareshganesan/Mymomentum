using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyTodo.Models;

namespace MyTodo.Services
{
    /// <summary>
    /// Handles JSON file save/load for persistent storage.
    /// Data is stored in the user's AppData folder.
    /// </summary>
    public static class JsonStorageService
    {
        private static readonly string DataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyTodo");

        private static readonly string DataFile = Path.Combine(DataFolder, "data.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Saves app data to JSON file
        /// </summary>
        public static void Save(AppData data)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(DataFolder))
                    Directory.CreateDirectory(DataFolder);

                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(DataFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving data: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads app data from JSON file, returns new AppData if file doesn't exist
        /// </summary>
        public static AppData Load()
        {
            try
            {
                if (File.Exists(DataFile))
                {
                    var json = File.ReadAllText(DataFile);
                    return JsonSerializer.Deserialize<AppData>(json, JsonOptions) ?? new AppData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }

            return new AppData();
        }

        /// <summary>
        /// Deletes the data file (for testing/reset)
        /// </summary>
        public static void Delete()
        {
            try
            {
                if (File.Exists(DataFile))
                    File.Delete(DataFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting data: {ex.Message}");
            }
        }
    }
}
