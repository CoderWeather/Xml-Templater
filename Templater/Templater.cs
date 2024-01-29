using System.Text;
using System.Text.Json;
using Templater.Core;

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
        // var validation = ParseHelper.ValidateTemplate(template);
        // if (validation is false) {
        //     throw new TemplateFormatException("Invalid template.");
        // }

        using var dataDoc = JsonDocument.Parse(jsonData, JsonDocumentOptions);
        var obj = dataDoc.RootElement;
        var sb = new StringBuilder();
        var t = template.AsSpan();


        return "";
    }

    static bool BlockProcessing(ReadOnlySpan<char> template, JsonElement json, StringBuilder sb, out int offset) {
        var t = template;
        offset = 0;
        while (t.Length > 0) {
            if (ParseHelper.ParseToNextInlineBlock(template, out var index, out var length, out var type) is false) {
                break;
            }


            offset += index;
            t = t[index..];
        }

        return true;
    }

    static bool BlockForProcessing(
        ReadOnlySpan<char> template,
        ReadOnlySpan<char> enumerationObjectName,
        JsonDataContextStack jsonDataContextStack,
        JsonElement jsonArray,
        StringBuilder sb,
        out int offset
    ) {
        offset = 0;
        foreach (var el in jsonArray.EnumerateArray()) {
            var t = template;
            while (true) {
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
                            //
                        }

                        sb.Append(formattedValue);
                        t = t[(index + length)..];
                        offset += index + length;
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

                        if (ParseHelper.ExtractJsonValueFromPropertiesPath(collectionPath, el, true, out var innerJsonArray) is false) {
                            return false;
                        }

                        var startOfNextLine = ParseHelper.IndexOfNextStartOfLine(t, index);
                        if (startOfNextLine is -1) {
                            throw new TemplateFormatException("Unexpected end of file.");
                        }

                        using var innerJsonDataContextStack = jsonDataContextStack.Push(el);
                        if (BlockForProcessing(
                                t[startOfNextLine..],
                                innerEnumerationObjectName,
                                innerJsonDataContextStack,
                                innerJsonArray,
                                sb,
                                out var innerOffset) is false
                           ) {
                            return false;
                        }

                        offset += innerOffset;

                        break;
                    }
                    case InlineEntryType.ForEnd: {
                        var prevLineEndIndex = ParseHelper.IndexOfPrevEndOfLine(t, index);
                        var spanToWrite = t[..prevLineEndIndex];
                        sb.Append(spanToWrite);
                        return true;
                    }
                    case InlineEntryType.EndOfFile or InlineEntryType.Undefined:
                        throw new TemplateFormatException("Unexpected end of file.");
                }

                offset += index;
                t = t[index..];
            }
        }

        return false;
    }

    static bool ProcessInlineEntry(ReadOnlySpan<char> input, JsonElement dataObj, bool contextOpen, out string result) {
        var t = input;
        var anyFilterIndex = t.IndexOf('|');
        ReadOnlySpan<char> filter = default;
        ReadOnlySpan<char> propertiesPath;
        if (anyFilterIndex > -1) {
            filter = t[(anyFilterIndex + 1)..].Trim();
            propertiesPath = t[..anyFilterIndex];
        } else {
            propertiesPath = t.TrimEnd();
        }

        if (ParseHelper.ExtractJsonValueFromPropertiesPath(propertiesPath, dataObj, contextOpen, out var jsonProperty) is false) {
            result = default!;
            return false;
        }

        return InlineFilterFormat.FormatValue(jsonProperty, filter, out result);
    }

    static bool TryGetJsonPropertyFromOpenedContext(
        this JsonDataContextStack openedJsonDataContext,
        ReadOnlySpan<char> propertyName,
        out JsonElement property
    ) {
        foreach (var el in openedJsonDataContext) {
            if (el.TryGetProperty(propertyName, out property)) {
                return true;
            }
        }

        property = default;
        return true;
    }
}
