using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Momentum.Models;

namespace Momentum.Services
{
    public sealed class Account
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        /// <summary>Hash of a one-time-shown recovery code that allows a password reset.</summary>
        public string RecoverySalt { get; set; } = string.Empty;
        public string RecoveryHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }
    }

    public sealed class AccountFile
    {
        public List<Account> Accounts { get; set; } = new();
        public string? RememberedUser { get; set; }
    }

    /// <summary>
    /// Local accounts on this PC. Passwords are stored only as salted PBKDF2-SHA256 hashes; each account's progress
    /// is a separate file under users/&lt;name&gt;.
    /// </summary>
    public static class AccountService
    {
        private const int Iterations = 120_000;
        private static string FilePath => Path.Combine(StorageService.DataDirectory, "accounts.json");

        public static AccountFile Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<AccountFile>(File.ReadAllText(FilePath), StorageService.JsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                StorageService.Log($"accounts.json unreadable: {ex.Message}");
            }
            return new AccountFile();
        }

        private static void Save(AccountFile file)
        {
            Directory.CreateDirectory(StorageService.DataDirectory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(file, StorageService.JsonOptions));
        }

        public static Account? Find(AccountFile file, string username) =>
            file.Accounts.FirstOrDefault(a => a.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>Returns an error message, or null on success.</summary>
        public static string? Create(string username, string password, string displayName, string leetCode, bool remember)
        {
            username = username.Trim();
            if (username.Length < 3 || username.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.'))
                return "Username needs 3+ characters: letters, numbers, _ - or .";
            if (password.Length < 6) return "Password needs at least 6 characters.";

            var file = Load();
            if (Find(file, username) != null) return "That username is taken on this PC.";

            bool isFirst = file.Accounts.Count == 0;
            var salt = RandomNumberGenerator.GetBytes(16);
            var account = new Account
            {
                Username = username,
                DisplayName = displayName.Trim(),
                Salt = Convert.ToBase64String(salt),
                Hash = Convert.ToBase64String(HashPassword(password, salt)),
                LastLogin = DateTime.Now,
            };
            file.Accounts.Add(account);
            file.RememberedUser = remember ? username : null;
            Save(file);

            // Seed the account's progress file.
            StorageService.UseAccount(username);
            var state = isFirst ? StorageService.LoadLegacy() : null;
            if (state == null)
            {
                state = new AppState();
                if (isFirst) MigrationService.TryImportV3(state);
            }
            state.Profile.Name = account.DisplayName;
            if (leetCode.Trim().Length > 0) state.Profile.LeetCodeUsername = leetCode.Trim();
            state.Profile.TargetDate ??= DateTime.Today.AddDays(60);
            state.Profile.Onboarded = true;
            StorageService.Save(state);
            return null;
        }

        public static bool HasRecoveryCode(string username) => Find(Load(), username) is { RecoveryHash.Length: > 0 };

        /// <summary>Creates a new recovery code (replacing any old one) and returns it; only its hash is stored.</summary>
        public static string NewRecoveryCode(string username)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // no 0/O or 1/I
            var chars = Enumerable.Range(0, 16).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray();
            var code = string.Join("-", Enumerable.Range(0, 4).Select(i => new string(chars, i * 4, 4)));
            var file = Load();
            var account = Find(file, username) ?? throw new InvalidOperationException("No such account.");
            var salt = RandomNumberGenerator.GetBytes(16);
            account.RecoverySalt = Convert.ToBase64String(salt);
            account.RecoveryHash = Convert.ToBase64String(HashPassword(NormalizeCode(code), salt));
            Save(file);
            return code;
        }

        public static bool VerifyRecoveryCode(string username, string code)
        {
            if (Find(Load(), username) is not { RecoveryHash.Length: > 0 } account) return false;
            var actual = HashPassword(NormalizeCode(code), Convert.FromBase64String(account.RecoverySalt));
            return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(account.RecoveryHash));
        }

        private static string NormalizeCode(string code) => new(code.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

        /// <summary>Sets a new password for a local account. Callers must verify the person first (Windows Hello or recovery code).
        /// Progress is untouched (it isn't encrypted with the password).</summary>
        public static string? ResetPassword(string username, string password)
        {
            var file = Load();
            if (Find(file, username) is not { } account) return "No account with that username on this PC.";
            if (password.Length < 6) return "Password needs at least 6 characters.";
            var salt = RandomNumberGenerator.GetBytes(16);
            account.Salt = Convert.ToBase64String(salt);
            account.Hash = Convert.ToBase64String(HashPassword(password, salt));
            Save(file);
            return null;
        }

        public static string? SignIn(string username, string password, bool remember)
        {
            var file = Load();
            var account = Find(file, username);
            if (account == null) return "No account with that username on this PC.";
            var expected = Convert.FromBase64String(account.Hash);
            var actual = HashPassword(password, Convert.FromBase64String(account.Salt));
            if (!CryptographicOperations.FixedTimeEquals(expected, actual)) return "Wrong password.";

            account.LastLogin = DateTime.Now;
            file.RememberedUser = remember ? account.Username : null;
            Save(file);
            StorageService.UseAccount(account.Username);
            return null;
        }

        /// <summary>Signs in the remembered account without a password ("keep me signed in").</summary>
        public static bool TryResume()
        {
            var file = Load();
            if (file.RememberedUser is not { } user || Find(file, user) is not { } account) return false;
            StorageService.UseAccount(account.Username);
            return true;
        }

        public static void SignOut()
        {
            var file = Load();
            file.RememberedUser = null;
            Save(file);
        }

        private static byte[] HashPassword(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
    }
}
