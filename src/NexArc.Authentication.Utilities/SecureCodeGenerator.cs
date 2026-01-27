using System.Security.Cryptography;

namespace NexArc.Authentication.Utilities;

public sealed class SecureCodeGenerator : ISecureCodeGenerator
{
    private const string NumericAlphabet = "0123456789";
    private const string UnambiguousAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Generate(int length, CodeAlphabet alphabet)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");
        }

        var chars = GetAlphabet(alphabet);
        var buffer = new char[length];

        for (var i = 0; i < buffer.Length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(chars.Length);
            buffer[i] = chars[index];
        }

        return new string(buffer);
    }

    private static string GetAlphabet(CodeAlphabet alphabet)
        => alphabet switch
        {
            CodeAlphabet.Numeric => NumericAlphabet,
            CodeAlphabet.Unambiguous => UnambiguousAlphabet,
            _ => throw new ArgumentOutOfRangeException(nameof(alphabet), "Unsupported alphabet.")
        };
}
