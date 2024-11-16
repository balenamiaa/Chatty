namespace Chatty.Backend.Security.Hashing;

public sealed class HashingOptions
{
    public int Iterations { get; init; }
    public int SaltSize { get; init; } = 16;
    public int KeySize { get; init; } = 32;
}
