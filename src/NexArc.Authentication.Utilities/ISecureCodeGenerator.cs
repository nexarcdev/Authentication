namespace NexArc.Authentication.Utilities;

public interface ISecureCodeGenerator
{
    string Generate(int length, CodeAlphabet alphabet);
}
