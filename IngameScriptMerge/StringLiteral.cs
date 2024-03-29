using Microsoft.CodeAnalysis;

namespace IngameScriptMerge;

public record StringLiteral
{
    public string Shortened;
    public int Count;

    public bool ShouldShorten(string text)
    {
        // const string x=;
        return 17 + text.Length + Count < (2 + text.Length) * Count;
    }

    public override string ToString() => $"{Shortened} [{Count}]";
}