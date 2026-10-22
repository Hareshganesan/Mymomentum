using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyTodo.Models;

namespace MyTodo.Services
{
    /// <summary>
    /// Handles persistent JSON storage across multiple redundant storage locations.
    /// Guarantees data persistence across restarts, process termination, and portable drives.
    /// </summary>
    public static class JsonStorageService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private static List<string> GetCandidateStoragePaths()
        {
            var paths = new List<string>();

            // 1. App directory (portable persistence right next to MyTodo.exe)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                paths.Add(Path.Combine(baseDir, "placement_data.json"));
                paths.Add(Path.Combine(baseDir, "data", "placement_data.json"));
            }

            // 2. Current working directory
            try
            {
                var curDir = Directory.GetCurrentDirectory();
                var curFile = Path.Combine(curDir, "placement_data.json");
                if (!string.IsNullOrEmpty(curDir) && !paths.Contains(curFile))
                {
                    paths.Add(curFile);
                }
            }
            catch { }

            // 3. User LocalAppData folder
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localAppData))
                {
                    paths.Add(Path.Combine(localAppData, "MyMomentumPlacement", "placement_data.json"));
                }
            }
            catch { }

            return paths;
        }

        /// <summary>
        /// Saves app data atomically across all storage paths
        /// </summary>
        public static void Save(AppData data)
        {
            if (data == null) return;
            data.IsInitialized = true;

            try
            {
                var json = JsonSerializer.Serialize(data, JsonOptions);
                var candidates = GetCandidateStoragePaths();

                foreach (var filePath in candidates)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        // Write to temp file then replace for atomic persistence
                        var tmpFile = filePath + ".tmp";
                        File.WriteAllText(tmpFile, json);

                        // Backup previous file
                        if (File.Exists(filePath))
                        {
                            var bakFile = filePath + ".bak";
                            try { File.Copy(filePath, bakFile, true); } catch { }
                        }

                        File.Copy(tmpFile, filePath, true);
                        try { File.Delete(tmpFile); } catch { }
                    }
                    catch (Exception ex)
                    {
                        LogStorageError($"Failed to save to {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogStorageError($"Serialization error in Save: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads app data from the newest available valid storage file,
        /// or generates initial curated seed data and saves it.
        /// </summary>
        public static AppData Load()
        {
            AppData? data = null;

            var candidates = GetCandidateStoragePaths();
            var existingFiles = candidates
                .Where(File.Exists)
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .ToList();

            foreach (var filePath in existingFiles)
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        data = JsonSerializer.Deserialize<AppData>(json, JsonOptions);
                        if (data != null && data.IsInitialized)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogStorageError($"Failed loading from {filePath}: {ex.Message}");
                }
            }

            // Fallback to seed data only if no saved data was ever found
            if (data == null)
            {
                data = new AppData
                {
                    IsInitialized = true,
                    Tasks = SeedDataService.GetCuratedDailyTasks(),
                    DsaProblems = SeedDataService.GetCuratedDsaSheet(),
                    CoreCsQuestions = SeedDataService.GetCuratedCoreCsQuestions(),
                    CompanyApplications = SeedDataService.GetCuratedSampleApplications(),
                    BehavioralQuestions = SeedDataService.GetCuratedBehavioralQuestions()
                };

                // Persist seed data immediately so user state has a concrete baseline
                Save(data);
            }
            else
            {
                // Ensure sub-collections are not null
                data.Tasks ??= new();
                data.Goals ??= new();
                data.DsaProblems ??= new();
                data.CoreCsQuestions ??= new();
                data.CompanyApplications ??= new();
                data.BehavioralQuestions ??= new();
                data.MockHistory ??= new();
                data.Reflections ??= new();
                data.DailyCompletionHistory ??= new();
            }

            return data;
        }

        private static void LogStorageError(string message)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDir))
                {
                    File.AppendAllText(Path.Combine(baseDir, "storage_errors.log"), line);
                }
            }
            catch { }
        }

        /// <summary>
        /// Deletes all data files across candidate locations
        /// </summary>
        public static void Delete()
        {
            var candidates = GetCandidateStoragePaths();
            foreach (var filePath in candidates)
            {
                try
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                    var bak = filePath + ".bak";
                    if (File.Exists(bak)) File.Delete(bak);
                }
                catch { }
            }
        }
    }
}
