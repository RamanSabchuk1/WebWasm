using System.Buffers;

namespace WebWasm.Helpers;

public static class JsonKeyConverter
{
    private static readonly SearchValues<char> TargetChars = SearchValues.Create(['.']);

    public static string FormatJsonKey(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        scoped ReadOnlySpan<char> srcSpan = input.AsSpan();
        var hasDot = srcSpan.ContainsAny(TargetChars);
        var camelCaseSpaces = 0;

        for (var i = 1; i < srcSpan.Length; i++)
        {
            if (char.IsLower(srcSpan[i - 1]) && char.IsUpper(srcSpan[i]))
            {
                camelCaseSpaces++;
            }
        }

        if (!hasDot && camelCaseSpaces == 0)
        {
            return input;
        }

        var newLength = srcSpan.Length + camelCaseSpaces;
        return string.Create(newLength, (input, hasDot), static (dest, state) =>
        {
            var (originalString, needsDotReplacement) = state;
            ReadOnlySpan<char> src = originalString.AsSpan();
            var destIdx = 0;
            for (var srcIdx = 0; srcIdx < src.Length; srcIdx++)
            {
                var current = src[srcIdx];
                if (srcIdx > 0 && char.IsLower(src[srcIdx - 1]) && char.IsUpper(current))
                {
                    dest[destIdx++] = ' ';
                }

                if (needsDotReplacement && current == '.')
                {
                    dest[destIdx++] = ' ';
                }
                else
                {
                    dest[destIdx++] = current;
                }
            }
        });
    }
}
