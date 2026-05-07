using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;

namespace MigracaoDados.Infrastructure.Security;

[SupportedOSPlatform("windows")]
public sealed class WindowsDpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MigracaoDados.LocalSettings.v1");

    public string Protect(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return string.Empty;
        }

        var protectedBytes = Convert.FromBase64String(protectedValue);
        var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(bytes);
    }
}
