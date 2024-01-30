using System.Buffers;
using System.Globalization;
using System.Text.Json;
using Templater.Core;

namespace Templater;

static class InlineFilterFormat {
    public static bool FormatValue(JsonElement jsonValue, ReadOnlySpan<char> filter, out string result) {
        var valueByteLength = jsonValue.GetByteLength();
        using var buffer = ArrayPool<char>.Shared.RentStruct(valueByteLength);
        var writtenChars = jsonValue.WriteTo(buffer.Span);
        var resultValue = buffer.Span[..writtenChars];
        result = resultValue.ToString();

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

        result = d.ToString("F2", CultureInfo.InvariantCulture);
        return true;
    }

    public static bool FormatParagraph(ReadOnlySpan<char> input, out string result) {
        result = input.ToString();
        return true;
    }
}
