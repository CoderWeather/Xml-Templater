using System.Buffers;
using System.Runtime.CompilerServices;

namespace Templater.Core;

public static class ArrayPoolExtensions {
    public static PooledArrayOwnerStruct<T> RentStruct<T>(this ArrayPool<T> pool, int length) where T : struct => new(length, pool);
}

public struct PooledArrayOwnerStruct<T> : IDisposable where T : struct {
    readonly ArrayPool<T> pool;
    readonly int length;
    T[]? array;

    public PooledArrayOwnerStruct(int length, ArrayPool<T> pool) {
        this.pool = pool;
        this.length = length;
        if (length is 0) {
            array = [];
        } else {
            array = pool.Rent(length);
            array.AsSpan(0, length).Clear();
        }
    }

    public int Length => length;

    public Memory<T> Memory {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ValidateNotDisposed();
            return array.AsMemory(0, length);
        }
    }

    public Span<T> Span {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ValidateNotDisposed();
            return array.AsSpan(0, length);
        }
    }

    void ValidateNotDisposed() {
        if (array is null) {
            throw new ObjectDisposedException(nameof(PooledArrayOwnerStruct<T>), "The array has already been returned to the pool.");
        }
    }

    public void Dispose() {
        var ar = array;
        if (ar is null) {
            return;
        }

        array = null;
        if (ar.Length > 0) {
            pool.Return(ar);
        }
    }
}
