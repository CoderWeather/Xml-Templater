using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Templater.Core;

static class SystemTextJsonUnsafeAccessors {
    static SystemTextJsonUnsafeAccessors() {
        var jsonReaderHelperType = Type.GetType("System.Text.Json.JsonReaderHelper, System.Text.Json") ??
                                   throw new Exception("JsonReaderHelper type not found.");

        var transcodeHelper = jsonReaderHelperType.GetMethod(
            "TranscodeHelper",
            BindingFlags.Static | BindingFlags.Public,
            [typeof(ReadOnlySpan<byte>), typeof(Span<char>)]
        ) ?? throw new Exception("TranscodeHelper method not found.");

        var p1 = Expression.Parameter(typeof(ReadOnlySpan<byte>));
        var p2 = Expression.Parameter(typeof(Span<char>));
        var expr = Expression.Lambda<TranscodeHelperDelegate>(
            Expression.Call(transcodeHelper, p1, p2),
            p1, p2
        );
        TranscodeHelperInstance = expr.Compile();
    }

    delegate int TranscodeHelperDelegate(ReadOnlySpan<byte> utf8Unescaped, Span<char> destination);

    static readonly TranscodeHelperDelegate TranscodeHelperInstance;

    static int JsonReaderHelper_TranscodeHelper(ReadOnlySpan<byte> utf8Unescaped, Span<char> destination) =>
        TranscodeHelperInstance(utf8Unescaped, destination);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CheckValidInstance")]
    static extern void JsonElement_CheckValidInstance(ref JsonElement je);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_idx")]
    static extern ref int JsonElement_Index(ref JsonElement je);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_parent")]
    static extern ref JsonDocument JsonElement_Parent(ref JsonElement je);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetRawValue")]
    static extern ReadOnlyMemory<byte> JsonDocument_GetRawValue(JsonDocument jd, int index, bool includeQuotes);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetJsonTokenType")]
    static extern JsonTokenType JsonDocument_GetJsonTokenType(JsonDocument jd, int index);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetArrayLength")]
    static extern int JsonDocument_GetArrayLength(JsonDocument jd, int index);

    public static int WriteTo(this JsonElement jsonElement, Span<char> destination) {
        JsonElement_CheckValidInstance(ref jsonElement);
        var parent = JsonElement_Parent(ref jsonElement);
        var rawValue = JsonDocument_GetRawValue(parent, JsonElement_Index(ref jsonElement), false);
        return JsonReaderHelper_TranscodeHelper(rawValue.Span, destination);
    }

    public static int GetByteLength(this JsonElement jsonElement) {
        JsonElement_CheckValidInstance(ref jsonElement);
        var parent = JsonElement_Parent(ref jsonElement);
        var rawValue = JsonDocument_GetRawValue(parent, JsonElement_Index(ref jsonElement), false);
        return rawValue.Length;
    }

    public static JsonTokenType GetTokenType(this JsonElement jsonElement) {
        JsonElement_CheckValidInstance(ref jsonElement);
        var parent = JsonElement_Parent(ref jsonElement);
        return JsonDocument_GetJsonTokenType(parent, JsonElement_Index(ref jsonElement));
    }

    public static int GetArrayLength(this JsonElement jsonElement) {
        JsonElement_CheckValidInstance(ref jsonElement);
        var parent = JsonElement_Parent(ref jsonElement);
        return JsonDocument_GetArrayLength(parent, JsonElement_Index(ref jsonElement));
    }
}
