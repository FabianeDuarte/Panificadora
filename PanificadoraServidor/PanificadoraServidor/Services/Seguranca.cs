using System.Security.Cryptography;
using System.Text;

namespace PanificadoraServidor.Services;

public static class Senha
{
    const int Iteracoes = 100_000;
    static readonly string Falso = Hash("senha-falsa-para-igualar-o-tempo");

    public static string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var h = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(h)}";
    }

    public static bool Verificar(string senha, string? armazenado)
    {
        var p = (armazenado ?? Falso).Split(':');
        if (p.Length != 2) return false;
        var salt = Convert.FromBase64String(p[0]);
        var esperado = Convert.FromBase64String(p[1]);
        var h = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(h, esperado) && armazenado != null;
    }
}

public static class TokenService
{
    public static string Novo() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
