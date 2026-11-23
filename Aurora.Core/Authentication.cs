using System.Security.Cryptography;

namespace Aurora.Core;

public enum RhRole { Administrator, HumanResources, Manager }

public sealed record RhAccount(string Username, string DisplayName, RhRole Role, string SaltHex, string HashHex, int Iterations);

// Contrato de busca das contas de login: DemoAccounts (memória) ou ContasDoBanco (tabela usuarios).
public interface IAccountDirectory
{
    RhAccount? Find(string username);
}

public sealed class RhSession
{
    internal RhSession(string displayName, RhRole role) => (DisplayName, Role) = (displayName, role);
    public string DisplayName { get; }
    public RhRole Role { get; }
    public bool IsActive { get; private set; } = true;
    public void SignOut() => IsActive = false;
}

public sealed class Authentication(IAccountDirectory accounts)
{
    private const int DemoIterations = 600_000;
    private static readonly byte[] MissingSalt = new byte[16];
    private static readonly byte[] MissingHash = new byte[32];

    public RhSession? SignIn(string username, string password)
    {
        username = username.Trim();
        if (username.Length is 0 or > 64 || password.Length is 0 or > 128) return null;

        var account = accounts.Find(username);
        // Uma conta inexistente também passa pelo hash para reduzir diferenças na resposta.
        var salt = account is null ? MissingSalt : Convert.FromHexString(account.SaltHex);
        var expected = account is null ? MissingHash : Convert.FromHexString(account.HashHex);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, account?.Iterations ?? DemoIterations, HashAlgorithmName.SHA256, expected.Length);
        try
        {
            var matches = CryptographicOperations.FixedTimeEquals(actual, expected);
            return matches && account is not null ? new RhSession(account.DisplayName, account.Role) : null;
        }
        finally { CryptographicOperations.ZeroMemory(actual); }
    }
}

public sealed class DemoAccounts : IAccountDirectory
{
    // Contas fictícias. A senha é pública no README para todo mundo conseguir abrir a demonstração.
    private static readonly RhAccount[] Accounts =
    [
        new("admin", "Administrador", RhRole.Administrator, "7907557BF2ECD226C5A03C3A5F2B4FD2", "2A1AF8E248DD14D1A1FFD3B299D7461B1A61237461F402924B4BB6259F454202", 600_000),
        new("rh", "Helena", RhRole.HumanResources, "D96EE1CB7D75841B50F537815DB5352A", "3378310030710DD857016843E2C91781FFF7665942A8B81A4E35BA6238B64F3A", 600_000),
        new("gestor", "Gestor", RhRole.Manager, "4162217F056D5918171A220D05C764B3", "4611262F6DFCBB5CA51F7F898DB60563D2C60BC752165079DA0823DB6981C772", 600_000)
    ];

    public RhAccount? Find(string username) => Array.Find(Accounts, item => StringComparer.OrdinalIgnoreCase.Equals(item.Username, username));
}
