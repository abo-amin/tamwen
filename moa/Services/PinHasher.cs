using System.Security.Cryptography;

namespace moa.Services;

public interface IPinHasher
{
    (byte[] hash, byte[] salt) HashPin(string pin);
    bool Verify(string pin, byte[] hash, byte[] salt);
}

public class PinHasher : IPinHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public (byte[] hash, byte[] salt) HashPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            throw new ArgumentException("PIN is required", nameof(pin));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password: pin,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: KeySize);

        return (hash, salt);
    }

    public bool Verify(string pin, byte[] hash, byte[] salt)
    {
        if (string.IsNullOrWhiteSpace(pin)) return false;
        if (hash is null || hash.Length == 0) return false;
        if (salt is null || salt.Length == 0) return false;

        var testHash = Rfc2898DeriveBytes.Pbkdf2(
            password: pin,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: hash.Length);

        return CryptographicOperations.FixedTimeEquals(testHash, hash);
    }
}
