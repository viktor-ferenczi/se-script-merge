using System.Collections.Generic;
using System.Linq;

namespace IngameScriptMerge;

public static class DebugBlocks
{
    private enum IfDirective
    {
        Keep,
        Remove,
        Unrelated
    }

    public static IEnumerable<string> FilterLines(this IEnumerable<string> scriptLines, bool releaseMode)
    {
        var stack = new Stack<IfDirective>();

        foreach (var line in scriptLines)
        {
            var trimmedLine = line.Trim();
            switch (trimmedLine)
            {
                case "#if DEBUG":
                    stack.Push(releaseMode ? IfDirective.Remove : IfDirective.Keep);
                    continue;

                case "#if !DEBUG":
                    stack.Push(releaseMode ? IfDirective.Keep : IfDirective.Remove);
                    continue;

                case "#endif":
                    if (stack.Any() && stack.Pop() != IfDirective.Unrelated)
                    {
                        continue;
                    }
                    break;

                case "#endregion":
                    if (releaseMode)
                    {
                        continue;
                    }
                    break;

                default:
                    if (releaseMode && trimmedLine.StartsWith("#region "))
                    {
                        continue;
                    }
                    if (trimmedLine.StartsWith("#if "))
                    {
                        stack.Push(IfDirective.Unrelated);
                    }
                    break;
            }

            if (!stack.Contains(IfDirective.Remove))
            {
                yield return line;
            }
        }
    }
}