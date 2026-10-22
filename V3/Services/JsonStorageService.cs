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
    /// Automatically ensures default curated placement data is populated.
    /// </summary>
    public static class JsonStorageService
    {
        private static readonly string DataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyMomentumPlacement");

        private static readonly string DataFile = Path.Combine(DataFolder, "placement_data.json");

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
        /// Loads app data from JSON file, returns populated AppData if file doesn't exist
        /// </summary>
        public static AppData Load()
        {
            AppData? data = null;

            try
            {
                if (File.Exists(DataFile))
                {
                    var json = File.ReadAllText(DataFile);
                    data = JsonSerializer.Deserialize<AppData>(json, JsonOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }

            data ??= new AppData();

            // Populate curated data if empty
            if (data.DsaProblems == null || data.DsaProblems.Count == 0)
            {
                data.DsaProblems = SeedDataService.GetCuratedDsaSheet();
            }

            if (data.CoreCsQuestions == null || data.CoreCsQuestions.Count == 0)
            {
                data.CoreCsQuestions = SeedDataService.GetCuratedCoreCsQuestions();
            }

            if (data.CompanyApplications == null || data.CompanyApplications.Count == 0)
            {
                data.CompanyApplications = SeedDataService.GetCuratedSampleApplications();
            }

            if (data.BehavioralQuestions == null || data.BehavioralQuestions.Count == 0)
            {
                data.BehavioralQuestions = SeedDataService.GetCuratedBehavioralQuestions();
            }

            if (data.Tasks == null || data.Tasks.Count == 0)
            {
                data.Tasks = SeedDataService.GetCuratedDailyTasks();
            }

            return data;
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
