using System.Text;
using System.Text.Json;

namespace Templater;

/*
<ul id="products">
   {% for product in products %}
   <li>
     <h2>{{product.name}}</h2>
     Only {{product.price | price }}
     {{product.description | paragraph }}
   </li>
   {% endfor %}
</ul>
*/
/*
{
  "products": [
   {
    "name": "Apple",
    "price": 329,
    "description": "flat-out fun"
   },
   {
    "name": "Orange",
    "price": 25,
    "description": "colorful"
   },
   {
    "name": "Banana",
    "price": 99,
    "description": "peel it"
   }
  ]
}
*/
/*
<ul id="products">
   <li>
     <h2>Apple</h2>
     $329
     flat-out fun
   </li>
   <li>
     <h2>Orange</h2>
     $25
     colorful
   </li>
   <li>
     <h2>Banana</h2>
     $99
     peel it
   </li>
</ul>
*/

public static class Templater {
    static readonly JsonDocumentOptions JsonDocumentOptions = new() {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static string CreateHtml(string template, string jsonData) {
        using var dataDoc = JsonDocument.Parse(jsonData, JsonDocumentOptions);
        var obj = dataDoc.RootElement;
        var sb = new StringBuilder();

        if (Processing(template.AsSpan(), obj, sb) is false) {
            return default!;
        }

        return sb.ToString();
    }

    static bool Processing(ReadOnlySpan<char> template, JsonElement rootObject, StringBuilder sb) {
        var t = template;

        while (t.Length > 0) {
            if (ParseHelper.ParseToNextInlineBlock(t, out var index, out var length, out var type) is false) {
                break;
            }

            var offset = 0;

            switch (type) {
                case InlineEntryType.FromObject: {
                    var spanToWrite = t[..index];
                    sb.Append(spanToWrite);
                    var inlineValue = t[index..(index + length)].Trim(" {}");
                    if (ProcessInlineEntry(inlineValue, rootObject, true, out var formattedValue) is false) {
                        return false;
                    }

                    sb.Append(formattedValue);
                    offset = index + length;
                    break;
                }
                case InlineEntryType.ForStart: {
                    var prevLineEndIndex = ParseHelper.IndexOfPrevEndOfLine(t, index);
                    var spanToWrite = t[..prevLineEndIndex];
                    sb.Append(spanToWrite);
                    var inlineValue = t[index..(index + length)].Trim(" {}%");
                    if (ParseHelper.ExtractNamesFromForStart(inlineValue, out var innerEnumerationObjectName, out var collectionPath) is false) {
                        return false;
                    }

                    if (ParseHelper.ExtractJsonValueFromPropertiesPath(collectionPath, rootObject, true, out var innerJsonArray) is false) {
                        return false;
                    }

                    var startOfNextLine = ParseHelper.IndexOfNextStartOfLine(t, index);
                    if (startOfNextLine is -1) {
                        throw new TemplateFormatException("Unexpected end of file.");
                    }

                    offset += startOfNextLine;

                    if (BlockForProcessing(t[startOfNextLine..], innerEnumerationObjectName, rootObject, innerJsonArray, sb,
                            out var innerOffset) is false) {
                        return false;
                    }

                    offset += innerOffset;
                    break;
                }
                case InlineEntryType.ForEnd:
                    throw new TemplateFormatException("Unexpected inline entry.");
                case InlineEntryType.EndOfFile: {
                    sb.Append(t);
                    return true;
                }
                case InlineEntryType.Undefined:
                    throw new TemplateFormatException("Unexpected end of file.");
            }

            t = t[offset..];
        }

        return true;
    }

    static bool BlockForProcessing(
        ReadOnlySpan<char> template,
        ReadOnlySpan<char> enumerationObjectName,
        JsonElement rootObject,
        JsonElement jsonArray,
        StringBuilder sb,
        out int offset
    ) {
        var anyIterations = false;
        offset = 0;
        foreach (var el in jsonArray.EnumerateArray()) {
            offset = 0;
            anyIterations = true;
            var t = template;
            var iterationEnd = false;
            while (true) {
                var iterationOffset = 0;
                if (ParseHelper.ParseToNextInlineBlock(t, out var index, out var length, out var type) is false) {
                    return false;
                }

                switch (type) {
                    case InlineEntryType.FromObject: {
                        var spanToWrite = t[..index];
                        sb.Append(spanToWrite);
                        var inlineValue = t[index..(index + length)].Trim(" {}");
                        string formattedValue;
                        if (inlineValue.StartsWith(enumerationObjectName)) {
                            if (ProcessInlineEntry(inlineValue, el, false, out formattedValue) is false) {
                                return false;
                            }
                        } else {
                            if (ProcessInlineEntry(inlineValue, rootObject, true, out formattedValue) is false) {
                                return false;
                            }
                        }

                        sb.Append(formattedValue);
                        iterationOffset += index + length;
                        break;
                    }
                    case InlineEntryType.ForStart: {
                        var prevLineEndIndex = ParseHelper.IndexOfPrevEndOfLine(t, index);
                        var spanToWrite = t[..prevLineEndIndex];
                        sb.Append(spanToWrite);
                        var inlineValue = t[index..(index + length)].Trim(" {}%");
                        if (ParseHelper.ExtractNamesFromForStart(inlineValue, out var innerEnumerationObjectName, out var collectionPath) is false) {
                            return false;
                        }

                        JsonElement innerJsonArray;
                        if (collectionPath.StartsWith(enumerationObjectName)) {
                            if (ParseHelper.ExtractJsonValueFromPropertiesPath(collectionPath, el, true, out innerJsonArray) is false) {
                                return false;
                            }
                        } else {
                            if (ParseHelper.ExtractJsonValueFromPropertiesPath(collectionPath, rootObject, true, out innerJsonArray) is false) {
                                return false;
                            }
                        }

                        var startOfNextLine = ParseHelper.IndexOfNextStartOfLine(t, index);
                        if (startOfNextLine is -1) {
                            throw new TemplateFormatException("Unexpected end of file.");
                        }

                        iterationOffset += startOfNextLine;

                        if (BlockForProcessing(t[startOfNextLine..], innerEnumerationObjectName, rootObject, innerJsonArray, sb,
                                out var innerOffset) is false) {
                            return false;
                        }

                        iterationOffset += innerOffset;

                        break;
                    }
                    case InlineEntryType.ForEnd: {
                        var prevLineEndIndex = ParseHelper.IndexOfPrevEndOfLine(t, index);
                        var spanToWrite = t[..prevLineEndIndex];
                        sb.Append(spanToWrite);
                        var startOfNextLine = ParseHelper.IndexOfNextStartOfLine(t, index);
                        if (startOfNextLine is -1) {
                            // add to offset length of the rest of the template
                            iterationOffset += t.Length;
                            iterationEnd = true;
                            break;
                        }

                        iterationOffset += startOfNextLine;
                        iterationEnd = true;
                        break;
                    }
                    case InlineEntryType.EndOfFile or InlineEntryType.Undefined:
                        throw new TemplateFormatException("Unexpected end of file.");
                }

                t = t[iterationOffset..];
                offset += iterationOffset;
                if (iterationEnd) {
                    break;
                }
            }
        }

        if (anyIterations is false) {
            if (ParseHelper.ParseToNextInlineBlock(template, out _, out _, out var type) is false) {
                return false;
            }

            if (type is not InlineEntryType.ForEnd) {
                throw new TemplateFormatException("Unexpected inline entry.");
            }
        }

        return true;
    }

    static bool ProcessInlineEntry(ReadOnlySpan<char> input, JsonElement dataObj, bool contextOpen, out string result) {
        var t = input;
        var anyFilterIndex = t.IndexOf('|');
        ReadOnlySpan<char> filter = default;
        ReadOnlySpan<char> propertiesPath;
        if (anyFilterIndex > -1) {
            filter = t[(anyFilterIndex + 1)..].TrimStart();
            propertiesPath = t[..anyFilterIndex].TrimEnd();
        } else {
            propertiesPath = t;
        }

        if (ParseHelper.ExtractJsonValueFromPropertiesPath(propertiesPath, dataObj, contextOpen, out var jsonProperty) is false) {
            result = default!;
            return false;
        }

        return InlineFilterFormat.FormatValue(jsonProperty, filter, out result);
    }
}
