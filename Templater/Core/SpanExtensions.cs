using System.Runtime.CompilerServices;

namespace Templater.Core;

static class SpanExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsLineSeparator(this ReadOnlySpan<char> span) => span.IndexOfAny('\n', '\r') is not -1;
}
