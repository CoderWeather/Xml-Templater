using System.Buffers;
using System.Globalization;
using System.Text.Json;
using Templater.Core;

namespace Templater;

public static class InlineFilterFormat {
    public static bool FormatValue(JsonElement jsonValue, ReadOnlySpan<char> filter, out string result) {
        var valueByteLength = jsonValue.GetByteLength();
        char[] buffer = null!;
        try {
            buffer = ArrayPool<char>.Shared.Rent(valueByteLength);
            var writtenChars = jsonValue.WriteTo(buffer.AsSpan());
            var resultValue = buffer.AsSpan()[..writtenChars];
            result = resultValue.ToString();
        }
        finally {
            if (buffer != null!) {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        if (filter != default) {
            if (ApplyFilter(result, filter, out var filteredResult) is false) {
                result = default!;
                return false;
            } else {
                result = filteredResult;
            }
        }

        return true;
    }

    // current support only for one filter without any parameters which can be applied as the function parameters
    // currently: {{<value> | filter}}
    // can be: {{<value> | filter(F2)
    static bool ApplyFilter(ReadOnlySpan<char> input, ReadOnlySpan<char> filter, out string result) {
        switch (filter) {
            case "price": {
                if (FormatPrice(input, out var formattedPrice) is false) {
                    result = default!;
                    return false;
                }

                result = formattedPrice;
                break;
            }
            case "paragraph": {
                if (FormatParagraph(input, out var formattedParagraph) is false) {
                    result = default!;
                    return false;
                }

                result = formattedParagraph;
                break;
            }
            default: {
                throw new TemplateFormatException("Unsupported filter.");
            }
        }

        return true;
    }


    public static bool FormatPrice(ReadOnlySpan<char> input, out string result) {
        if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) is false) {
            result = default!;
            return false;
        }

        result = d.ToString("C", CultureInfo.InvariantCulture);
        return true;
    }

    public static bool FormatParagraph(ReadOnlySpan<char> input, out string result) {
        result = input.ToString();
        return true;
    }
}
