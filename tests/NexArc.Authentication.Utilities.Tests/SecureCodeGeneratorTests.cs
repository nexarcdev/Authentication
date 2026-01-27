using NexArc.Authentication.Utilities;

namespace NexArc.Authentication.Utilities.Tests;

public class SecureCodeGeneratorTests
{
    [Fact]
    public void Generates_Numeric_Codes()
    {
        var generator = new SecureCodeGenerator();
        var code = generator.Generate(12, CodeAlphabet.Numeric);

        Assert.Equal(12, code.Length);
        Assert.All(code, ch => Assert.Contains(ch, "0123456789"));
    }

    [Fact]
    public void Generates_Unambiguous_Codes()
    {
        var generator = new SecureCodeGenerator();
        var code = generator.Generate(12, CodeAlphabet.Unambiguous);

        Assert.Equal(12, code.Length);
        Assert.All(code, ch => Assert.Contains(ch, "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"));
    }

    [Fact]
    public void Rejects_Non_Positive_Length()
    {
        var generator = new SecureCodeGenerator();

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(0, CodeAlphabet.Numeric));
    }
}
