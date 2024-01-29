using System.Buffers;
using System.Text.Json;
using Templater.Core;

namespace Templater;

public struct JsonDataContextStack(int length) : IDisposable {
    PooledArrayOwnerStruct<JsonElement>? array = ArrayPool<JsonElement>.Shared.RentStruct(length);

    public JsonDataContextStack Push(JsonElement element) {
        JsonDataContextStack newStack;
        if (array is null) {
            newStack = new JsonDataContextStack(1);
            newStack.array!.Value.Span[0] = element;
        } else {
            newStack = new JsonDataContextStack(array.Value.Length + 1);
            array.Value.Span.CopyTo(newStack.array!.Value.Span);
            newStack.array.Value.Span[^1] = element;
        }

        return newStack;
    }

    public static JsonDataContextStack CreateWith(JsonElement el) {
        var stack = new JsonDataContextStack(1);
        stack.array!.Value.Span[0] = el;
        return stack;
    }

    public void Dispose() {
        var ar = array;
        if (ar is null) {
            return;
        }

        array = null;
        ar.Value.Dispose();
    }

    public Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator {
        readonly ReadOnlySpan<JsonElement> stack;
        readonly int length;
        int index;

        internal Enumerator(JsonDataContextStack stack) {
            this.stack = stack.array!.Value.Span;
            length = stack.array!.Value.Length;
            index = length;

        }

        public JsonElement Current => stack[index];

        public bool MoveNext() {
            if (index < 0 || index >= length) {
                return false;
            }

            index--;
            return index >= 0;
        }
    }
}
