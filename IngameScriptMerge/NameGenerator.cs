using System.Collections.Generic;

namespace IngameScriptMerge;

public class NameGenerator
{
    private readonly struct UnicodeRange(int start, int end, string label)
    {
        public readonly int Start = start;
        public readonly int End = end;
        public readonly string Label = label;
    }

    // https://en.wikipedia.org/wiki/List_of_Unicode_characters
    private static readonly UnicodeRange[] UnicodeRanges =
    [
        new UnicodeRange(0x000c0, 0x00250, "Latin"),
        new UnicodeRange(0x00370, 0x00400, "Greek and Coptic"),
        new UnicodeRange(0x01400, 0x01680, "Unified Canadian Aboriginal Syllabic"),
        new UnicodeRange(0x01e00, 0x01f00, "Latin Extended Additional"),
        new UnicodeRange(0x020a0, 0x020c1, "Currency Symbols"),
        new UnicodeRange(0x02190, 0x02200, "Arrows"),
        new UnicodeRange(0x02200, 0x02300, "Mathematical Operators"),
        new UnicodeRange(0x02300, 0x02400, "Technical"),
        new UnicodeRange(0x02700, 0x027c0, "Dingbats"),
        new UnicodeRange(0x1f030, 0x1f094, "Domino Tiles"),
        new UnicodeRange(0x1f400, 0x1f4fe, "Emoji"),
        new UnicodeRange(0x1f500, 0x1f43e, "Emoji"),
        new UnicodeRange(0x1f600, 0x1f6c0, "Emoji"),
        new UnicodeRange(0x1f910, 0x1f930, "Emoji"),
        new UnicodeRange(0x1f950, 0x1fa70, "Emoji"),
        new UnicodeRange(0x1f700, 0x1f774, "Alchemical Symbols"),
    ];

    private readonly ICollection<char> forbidden;

    private int rangeIndex;
    private UnicodeRange unicodeRange;
    private int minifiedCharCode;

    public NameGenerator(ICollection<char> forbidden)
    {
        this.forbidden = forbidden;

        unicodeRange = UnicodeRanges[rangeIndex++];
        minifiedCharCode = unicodeRange.Start;
    }

    public string Next()
    {
        char minifiedChar;
        for (;;)
        {
            if (minifiedCharCode < 0)
            {
                return null;
            }

            minifiedChar = (char) minifiedCharCode++;

            if (minifiedCharCode == unicodeRange.End)
            {
                if (rangeIndex < UnicodeRanges.Length)
                {
                    unicodeRange = UnicodeRanges[rangeIndex++];
                    minifiedCharCode = unicodeRange.Start;
                }
                else
                {
                    minifiedCharCode = -1;
                }
            }

            if (minifiedChar.IsAllowedInIdentifier() && !forbidden.Contains(minifiedChar))
            {
                break;
            }
        }

        return minifiedChar.ToString();
    }
}