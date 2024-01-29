using System.Buffers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Templater;

public static partial class ParseHelper {
    // copied from (System.MemoryExtensions.EnumerateLines)MemoryExtensions.cs -> (MoveNext)SpanLineEnumerator.cs -> String.SearchValuesStorage.NewLineChars
    internal static readonly SearchValues<char> NewLineChars = SearchValues.Create("\r\f\u0085\u2028\u2029\n");

    [GeneratedRegex(@"\{\%\ +for\ +(\w+)\ +in\ +(\w+(\.\w+)*)\ +\%\}")]
    private static partial Regex InlineForStartRegex();

    [GeneratedRegex(@"\{\{\ *(\w+(\.\w+)*( \| \w+)?)\ *\}\}")]
    private static partial Regex InlineEntryRegex();

    [GeneratedRegex(@"\{\%\ +endfor\ +\%\}")]
    private static partial Regex InlineForEndRegex();

    public const char InlineMarkerOpen = '{';
    public const char InlineMarkerClose = '}';
    public const string InlineEntryOpen = "{{";
    public const string InlineEntryClose = "}}";
    public const string InlineBlockOpen = "{%";
    public const string InlineBlockClose = "%}";

    public static bool ValidateInlineEntry(ReadOnlySpan<char> span, out int length) {
        var declarationEndIndex = span.IndexOf("}}");
        var nextLine = span.IndexOfAny(NewLineChars);
        if (declarationEndIndex is -1 || (nextLine > -1 && nextLine < declarationEndIndex)) {
            length = default;
            return false;
        }

        var declaration = span[..(declarationEndIndex + 2)];
        var isMatch = InlineEntryRegex().IsMatch(declaration);
        length = isMatch ? declaration.Length : default;
        return isMatch;
    }

    public static bool ValidateInlineForStart(ReadOnlySpan<char> span, out int length) {
        var declarationEndIndex = span.IndexOf("%}");
        var nextLine = span.IndexOfAny(NewLineChars);
        if (declarationEndIndex is -1 || (nextLine > -1 && nextLine < declarationEndIndex)) {
            length = default;
            return false;
        }

        var declaration = span[..(declarationEndIndex + 2)];
        var isMatch = InlineForStartRegex().IsMatch(declaration);
        length = isMatch ? declaration.Length : default;
        return isMatch;
    }

    public static bool ValidateInlineForEnd(ReadOnlySpan<char> input, out int length) {
        var declarationEndIndex = input.IndexOf("%}");
        var nextLine = input.IndexOfAny(NewLineChars);
        if (declarationEndIndex is -1 || (nextLine > -1 && nextLine < declarationEndIndex)) {
            length = default;
            return false;
        }

        var declaration = input[..(declarationEndIndex + 2)];
        var isMatch = InlineForEndRegex().IsMatch(declaration);
        length = isMatch ? declaration.Length : default;
        return isMatch;
    }

    public static bool ExtractNamesFromForStart(
        ReadOnlySpan<char> input,
        out ReadOnlySpan<char> objectName,
        out ReadOnlySpan<char> collectionPath
    ) {
        var t = input;
        // for <obj> in <collection>
        var offset = t.IndexOf("for");
        t = t[(offset + 3)..];
        // for[ <obj> in <collection>
        offset = t.IndexOfAnyExcept(' ');
        t = t[offset..];
        // for [<obj> in <collection>]

        offset = t.IndexOf(' ');
        // for [<obj>] in <collection>
        objectName = t[..offset].Trim();

        t = t[(offset + 1)..];
        // for <obj> [in <collection>]
        t = t[3..];
        // for <obj> in [<collection>]
        collectionPath = t.TrimStart();

        return true;
    }

    public static bool ExtractJsonValueFromPropertiesPath(
        ReadOnlySpan<char> propertiesPath,
        JsonElement jsonData,
        bool contextOpen,
        out JsonElement result
    ) {
        var t = propertiesPath;
        var json = jsonData;

        if (contextOpen is false) {
            var dotIndex = t.IndexOf('.');
            if (dotIndex is -1) {
                result = default;
                return false;
            }

            t = t[(dotIndex + 1)..];
        }

        while (t.Length > 0) {
            var dotIndex = t.IndexOf('.');
            if (dotIndex is -1) {
                if (json.TryGetProperty(t, out var property) is false) {
                    result = default;
                    return false;
                }

                result = property;
                return true;
            }

            var objectName = t[..dotIndex];
            if (json.TryGetProperty(objectName, out var property1) is false) {
                result = default;
                return false;
            }

            json = property1;
            t = t[(dotIndex + 1)..];
        }

        result = default;
        return false;
    }

    public static bool ParseToNextInlineBlock(ReadOnlySpan<char> input, out int index, out int length, out InlineEntryType type) {
        var t = input;
        while (t.Length > 0) {
            var markerIndex = t.IndexOf(InlineMarkerOpen);
            var closeMarkerIndex = t.IndexOf(InlineMarkerClose);
            if (markerIndex is -1 && closeMarkerIndex is -1) {
                break;
            }

            if (markerIndex is -1) {
                if (closeMarkerIndex is not -1) {
                    throw new TemplateFormatException(InlineMarkerClose, closeMarkerIndex);
                }

                throw new TemplateFormatException(InlineMarkerOpen, markerIndex);
            }


            if (t[markerIndex - 1] is '\\') {
                t = t[(markerIndex + 1)..];
                continue;
            }

            t = t[markerIndex..];
            var nextLineIndex = t.IndexOfAny(NewLineChars);
            if (t.StartsWith(InlineEntryOpen) && char.IsLetter(t[2..].TrimStart()[0])) {
                var endIndex = t.IndexOf(InlineEntryClose);
                if (nextLineIndex is not -1 && nextLineIndex < endIndex) {
                    throw new TemplateFormatException(InlineEntryClose, nextLineIndex);
                }

                index = markerIndex;
                length = endIndex + InlineEntryClose.Length;
                type = InlineEntryType.FromObject;
                return true;
            } else if (t.StartsWith(InlineBlockOpen)) {
                var endIndex = t.IndexOf(InlineBlockClose);
                if (nextLineIndex is not -1 && nextLineIndex < endIndex) {
                    throw new TemplateFormatException(InlineBlockClose, nextLineIndex);
                }

                if (ValidateInlineForStart(t, out var l1)) {
                    index = markerIndex;
                    length = l1;
                    type = InlineEntryType.ForStart;
                    return true;
                }

                if (ValidateInlineForEnd(t, out var l2)) {
                    index = markerIndex;
                    length = l2;
                    type = InlineEntryType.ForEnd;
                    return true;
                }

                throw new TemplateFormatException("Unsupported inline block declaration.");
            } else {
                throw new TemplateFormatException(t[..2]);
            }
        }

        index = input.Length;
        length = default;
        type = InlineEntryType.EndOfFile;
        return true;
    }

    public static int IndexOfPrevEndOfLine(ReadOnlySpan<char> input, int currentIndex) {
        var t = input[..currentIndex];
        var index = t.LastIndexOfAny(NewLineChars);
        return index + 1;
    }

    public static int IndexOfNextStartOfLine(ReadOnlySpan<char> input, int currentIndex) {
        var t = input[currentIndex..];
        var index = t.IndexOfAny(NewLineChars);
        var newlineIndex = t[index..].IndexOfAnyExcept(NewLineChars);
        return currentIndex + index + newlineIndex;
    }
}
