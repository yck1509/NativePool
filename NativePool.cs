using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Unsafe {
    public abstract class NativeObject : IDisposable {
        protected NativeObject() => throw new InvalidOperationException("Must be instantiate through NativePool!");

        internal PoolBlock block;

        public void Free() => block.Pool.Free(this);
        void IDisposable.Dispose() => block.Pool.Free(this);
    }

    internal class PoolBlock {
        public readonly IntPtr Address;
        public readonly int Id;
        public readonly INativePool Pool;
        public readonly PoolBlock Previous;

        public PoolBlock(int size, int id, INativePool pool, PoolBlock prev) {
            Address = Marshal.AllocHGlobal(size);
            Id = id;
            Pool = pool;
            Previous = prev;
        }
    }

    struct PoolEntry : IComparable<PoolEntry> {
        public PoolBlock Block;
        public int Index;

        public PoolEntry(PoolBlock block, int index) {
            Block = block;
            Index = index;
        }

        public int CompareTo(PoolEntry other) {
            int result = Block.Id - other.Block.Id;
            if (result == 0)
                result = Index - other.Index;
            return result;
        }
    }

    interface INativePool {
        void Free(NativeObject obj);
    }

    public unsafe class NativePool<T> : INativePool, IDisposable where T : NativeObject {
        static readonly IntPtr typeHnd;
        static readonly int objSize;

        static NativePool() {
            if (typeof(T).GetTypeInfo().IsAbstract)
                throw new InvalidOperationException("Cannot create abstract types!");

            Exception error = null;
            try {
                typeHnd = UnsafeUtils.GetTypeHandle<T>();
                objSize = UnsafeUtils.GetTypeSize<T>();
            } catch (Exception ex) {
                error = ex;
            }
            if (error != null)
                throw new NotSupportedException("Cannot obtain runtime info!", error);
        }

        readonly int numBlockElems;
        readonly Heap<PoolEntry> free;
        readonly ReaderWriterLockSlim sync;
        readonly PoolEntry[] tmpEnts;
        PoolBlock tail;
        bool disposed;

        public int NumObjects => (tail?.Id + 1 ?? 0) * numBlockElems - free.Count;
        public bool IsFull => free.Count == 0;

        public NativePool(int numBlockElems = 0x100, int numInitialBlocks = 1) {
            this.numBlockElems = numBlockElems;
            free = new Heap<PoolEntry>();
            sync = new ReaderWriterLockSlim();
            tmpEnts = new PoolEntry[numBlockElems];
            tail = null;
            disposed = false;

            while (numInitialBlocks-- > 0)
                MakeBlock();
        }

        void MakeBlock() {
            int blockSize = numBlockElems * objSize;
            int id = tail?.Id + 1 ?? 0;
            tail = new PoolBlock(blockSize, id, this, tail);

            for (int i = 0; i < numBlockElems; i++)
                tmpEnts[i] = new PoolEntry(tail, i);
            free.PutAll(tmpEnts);
        }

        (PoolBlock block, IntPtr ptr) Alloc(IntPtr typeHnd) {
            sync.EnterWriteLock();
            try {
                PoolEntry ent;
                if (free.Count == 0)
                    MakeBlock();

                ent = free.Take();
                var ptr = ent.Block.Address + ent.Index * objSize;
                byte* p = (byte*)ptr;
                for (int i = 0; i < objSize; i++)
                    p[i] = 0;
                *(IntPtr*)ptr = typeHnd;

                return (ent.Block, ptr);
            } finally {
                sync.ExitWriteLock();
            }
        }

        void Free(PoolBlock block, IntPtr ptr) {
            sync.EnterWriteLock();
            try {
                var index = (int)((byte*)ptr - (byte*)block.Address) / objSize;
                free.Put(new PoolEntry(block, index));
            } finally {
                sync.ExitWriteLock();
            }
        }

        static void SetBlock(ref PoolBlock target, PoolBlock value) => target = value;
        public T New() {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);

            var (block, ptr) = Alloc(typeHnd);
            var obj = UnsafeUtils.ToObject<T>(ptr);
            SetBlock(ref obj.block, block);

            return obj;
        }

        void INativePool.Free(NativeObject obj) => Free((T)obj);
        public void Free(T obj) {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);

            var block = obj.block;
            SetBlock(ref obj.block, null);

            var ptr = UnsafeUtils.AddressOf(obj);
            *(IntPtr*)ptr = IntPtr.Zero;
            Free(block, ptr);
        }

        public void Dispose(bool force = false) {
            sync.EnterWriteLock();
            try {
                if (!force && NumObjects != 0)
                    throw new InvalidOperationException("There are active objects in pool!");

                var b = tail;
                while (b != null) {
                    Marshal.FreeHGlobal(b.Address);
                    b = b.Previous;
                }

                free.Clear();
                disposed = true;
            } finally {
                sync.ExitWriteLock();
            }
        }

        public void Dispose() {
            Dispose(false);
        }

        public PoolEnumerator GetEnumerator() => new PoolEnumerator(this);

        public struct PoolEnumerator : IEnumerator<T> {
            NativePool<T> pool;
            PoolBlock block;
            IntPtr current;
            internal PoolEnumerator(NativePool<T> pool) {
                this.pool = pool;
                this.block = null;
                current = IntPtr.Zero;
            }

            object IEnumerator.Current => UnsafeUtils.ToObject<T>(current);
            public T Current => UnsafeUtils.ToObject<T>(current);

            public bool MoveNext() {
                pool.sync.EnterReadLock();
                try {
                    if (current == IntPtr.Zero) {
                        block = pool.tail;
                        if (block == null)
                            return false;
                        current = block.Address;
                    } else if (block == null)
                        return false;
                    else
                        current += objSize;

                    bool found;
                    do {
                        found = true;
                        var end = block.Address + pool.numBlockElems * objSize;
                        while (current != end && *(IntPtr*)current != typeHnd)
                            current += objSize;

                        if (current == end) {
                            block = block.Previous;
                            if (block == null)
                                return false;
                            current = block.Address;
                            found = false;
                        }
                    } while (!found);
                    return true;
                } finally {
                    pool.sync.ExitReadLock();
                }
            }

            public void Dispose() { }
            public void Reset() => throw new NotSupportedException();
        }
    }
}
