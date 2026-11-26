using System.Security.Cryptography;

namespace Aurora.Core;

// os três perfis do sistema
public enum PerfilDeAcesso { Administrador, RecursosHumanos, Gestor }

// Uma conta de login. A senha não fica guardada, só o salt e o hash dela.
public record Conta(string Login, string NomeDeExibicao, PerfilDeAcesso Perfil, string SaltEmHex, string HashEmHex, int Iteracoes);

// Quem sabe procurar as contas: ContasDeExemplo (memória) ou ContasDoBanco (tabela usuarios)
public interface IBuscaDeContas
{
    Conta? BuscarConta(string usuario);
}

// Guarda quem está logado e o perfil dele
public class Sessao
{
    public string NomeDeExibicao { get; }
    public PerfilDeAcesso Perfil { get; }
    public bool Ativa { get; private set; }

    internal Sessao(string nomeDeExibicao, PerfilDeAcesso perfil)
    {
        NomeDeExibicao = nomeDeExibicao;
        Perfil = perfil;
        Ativa = true;
    }

    public void Sair()
    {
        Ativa = false;
    }
}

// Faz o login conferindo usuário e senha
public class Autenticacao
{
    private const int IteracoesDeExemplo = 600000;
    private static readonly byte[] SaltVazio = new byte[16];
    private static readonly byte[] HashVazio = new byte[32];

    private IBuscaDeContas contas;

    public Autenticacao(IBuscaDeContas contas)
    {
        this.contas = contas;
    }

    // Devolve a sessão se a senha estiver certa, ou null se estiver errada
    public Sessao? Entrar(string usuario, string senha)
    {
        usuario = usuario.Trim();
        if (usuario.Length == 0 || usuario.Length > 64) return null;
        if (senha.Length == 0 || senha.Length > 128) return null;

        Conta? conta = contas.BuscarConta(usuario);

        // Mesmo se a conta não existir, calcula o hash do mesmo jeito.
        // Assim o tempo de resposta é parecido e não dá pra descobrir quais usuários existem.
        byte[] salt = SaltVazio;
        byte[] esperado = HashVazio;
        int iteracoes = IteracoesDeExemplo;
        if (conta != null)
        {
            salt = Convert.FromHexString(conta.SaltEmHex);
            esperado = Convert.FromHexString(conta.HashEmHex);
            iteracoes = conta.Iteracoes;
        }

        // PBKDF2 com SHA-256: transforma a senha digitada num hash para comparar com o guardado
        byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, esperado.Length);
        try
        {
            // compara os dois hashes sempre no mesmo tempo
            bool senhaCerta = CryptographicOperations.FixedTimeEquals(calculado, esperado);
            if (senhaCerta && conta != null)
            {
                return new Sessao(conta.NomeDeExibicao, conta.Perfil);
            }
            return null;
        }
        finally
        {
            // apaga o hash calculado da memória
            CryptographicOperations.ZeroMemory(calculado);
        }
    }
}

// Contas de teste que ficam no código (usadas antes do banco e nos testes)
public class ContasDeExemplo : IBuscaDeContas
{
    // A senha das três é Aurora.demo26 (está no README)
    private static readonly Conta[] Contas =
    {
        new Conta("admin", "Administrador", PerfilDeAcesso.Administrador, "7907557BF2ECD226C5A03C3A5F2B4FD2", "2A1AF8E248DD14D1A1FFD3B299D7461B1A61237461F402924B4BB6259F454202", 600000),
        new Conta("rh", "Helena", PerfilDeAcesso.RecursosHumanos, "D96EE1CB7D75841B50F537815DB5352A", "3378310030710DD857016843E2C91781FFF7665942A8B81A4E35BA6238B64F3A", 600000),
        new Conta("gestor", "Gestor", PerfilDeAcesso.Gestor, "4162217F056D5918171A220D05C764B3", "4611262F6DFCBB5CA51F7F898DB60563D2C60BC752165079DA0823DB6981C772", 600000)
    };

    public Conta? BuscarConta(string usuario)
    {
        foreach (Conta conta in Contas)
        {
            // ignora maiúscula e minúscula ("ADMIN" acha "admin")
            if (string.Equals(conta.Login, usuario, StringComparison.OrdinalIgnoreCase))
            {
                return conta;
            }
        }
        return null;
    }
}
