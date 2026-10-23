using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Momentum.Models;

namespace Momentum.Services
{
    /// <summary>Single JSON file in %LOCALAPPDATA%\Momentum, written atomically with a rolling .bak.</summary>
    public static class StorageService
    {
        /// <summary>MOMENTUM_DATA_DIR overrides the location (handy for testing or a portable install).</summary>
        public static string DataDirectory { get; } =
            Environment.GetEnvironmentVariable("MOMENTUM_DATA_DIR") is { Length: > 0 } custom
                ? custom
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Momentum");

        /// <summary>Folder of the signed-in account; progress for each account lives in its own folder.</summary>
        public static string UserDirectory { get; private set; } = DataDirectory;

        public static string DataFile => Path.Combine(UserDirectory, "momentum.json");

        public static void UseAccount(string username) =>
            UserDirectory = Path.Combine(DataDirectory, "users", username.ToLowerInvariant());

        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        public static AppState? Load() => LoadFrom(DataFile);

        /// <summary>Single-file data saved before accounts existed.</summary>
        public static AppState? LoadLegacy() => LoadFrom(Path.Combine(DataDirectory, "momentum.json"));

        private static AppState? LoadFrom(string file)
        {
            foreach (var path in new[] { file, file + ".bak" })
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    var state = JsonSerializer.Deserialize<AppState>(File.ReadAllText(path), JsonOptions);
                    if (state != null) return Normalize(state);
                }
                catch (Exception ex)
                {
                    Log($"Failed to load {path}: {ex.Message}");
                }
            }
            return null;
        }

        public static void Save(AppState state)
        {
            try
            {
                Directory.CreateDirectory(UserDirectory);
                var tmp = DataFile + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(state, JsonOptions));
                if (File.Exists(DataFile)) File.Copy(DataFile, DataFile + ".bak", true);
                File.Move(tmp, DataFile, true);
            }
            catch (Exception ex)
            {
                Log($"Save failed: {ex.Message}");
            }
        }

        public static void Export(AppState state, string path) =>
            File.WriteAllText(path, JsonSerializer.Serialize(state, JsonOptions));

        public static AppState Import(string path)
        {
            var state = JsonSerializer.Deserialize<AppState>(File.ReadAllText(path), JsonOptions)
                        ?? throw new InvalidDataException("The file is not a Momentum backup.");
            return Normalize(state);
        }

        /// <summary>Re-applies case-insensitive comparers and fills nulls left by older or hand-edited files.</summary>
        private static AppState Normalize(AppState s)
        {
            s.Profile ??= new Profile();
            s.Problems = new Dictionary<string, ProblemProgress>(s.Problems ?? new(), StringComparer.OrdinalIgnoreCase);
            s.CustomProblems ??= new();
            s.Cards ??= new();
            s.CustomCards ??= new();
            s.Applications ??= new();
            s.Stories ??= new();
            s.Mocks ??= new();
            s.Tasks ??= new();
            s.Activity ??= new();
            s.SeenMail ??= new();
            foreach (var a in s.Applications) a.Emails ??= new();
            foreach (var p in s.Problems.Values) p.Srs ??= new SrsState();
            foreach (var c in s.Cards.Values) c.Srs ??= new SrsState();
            return s;
        }

        public static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(DataDirectory);
                File.AppendAllText(Path.Combine(DataDirectory, "momentum.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch { }
        }
    }
}
