NativePool
==========

What?
-----
Allocate managed objects (reference type) in a object pool,
backed by native memory buffer (`Marshal.AllocHGlobal`).

Why?
----
Because why not? Maybe I was drunk or sleep-coding?

When?
-----
1. You should not use it.
2. If you use it, your right to write software will likely be revoked.
3. Forget about this repo, unless you are an expert in C#, CLR, and have a legit strong reason to use it.
4. Compatibility? Memory safety? Is it edible?
6. Mono is not supported. Yet. Maybe never.

How?
----
See `Program.cs` for example.

**`NativeObject`**:
Base type for target objects.
Implements `IDisposable` for easy deallocation.

**`NativePool<T>`**:
The object pool itself.
Specify number of objects per block and number of initial blocks at creation.
Hopefully thread-safe.

**`T New(), void Free(T)`**:
Allocation and deallocation function.
Cthulhu will appear when you double free or use after free.

**`void Dispose(bool force)`**:
Dispose the object pool and free all objects in pool.
Validate no active objects in pool by default, unless `force` is specified.

**`PoolEnumerator GetEnumerator()`**:
Enumerate all active objects in pool by memory order.

License
-------
Public domain. I'm not responsible for any problems.

I really don't want to say I wrote this code.

