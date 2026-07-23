using System.Text;
using System.Text.RegularExpressions;

namespace LogicExporter.Logic;

public static class SymbolNormalizer
{
    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var normalized = input
            .Replace("<->", "↔", StringComparison.Ordinal)
            .Replace("[]", "□", StringComparison.Ordinal)
            .Replace("<>", "◇", StringComparison.Ordinal)
            .Replace("->", "→", StringComparison.Ordinal)
            .Replace("!=", "≠", StringComparison.Ordinal);

        normalized = Regex.Replace(normalized, @"\bforall\b", "∀", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\bexists\b", "∃", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            builder.Append(character switch
            {
                '~' or '!' => '¬',
                '&' => '∧',
                '|' => '∨',
                _ => character
            });
        }

        return builder.ToString();
    }
}
