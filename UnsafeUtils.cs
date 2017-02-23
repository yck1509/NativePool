using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Unsafe {
    public unsafe static class UnsafeUtils {
        delegate IntPtr getHandleFunc(ref RuntimeTypeHandle hnd);
        static getHandleFunc getHandle;
        static UnsafeUtils() {
            var method = typeof(RuntimeTypeHandle).GetProperty("Value").GetGetMethod();
            getHandle = (getHandleFunc)method.CreateDelegate(typeof(getHandleFunc));
        }

        public static IntPtr GetTypeHandle<T>() {
            var hnd = typeof(T).TypeHandle;
            return getHandle(ref hnd);
        }

        public static int GetTypeSize<T>() {
            return ((int*)GetTypeHandle<T>())[1];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct AddressFinder<T> {
            public T obj;
            public fixed byte addr[1];
        }

        public static IntPtr AddressOf<T>(T obj) where T : class {
            var finder = new AddressFinder<T> { obj = obj };
            var addr = &finder.addr;
            return *(IntPtr*)&addr[-1];
        }

        public static T ToObject<T>(IntPtr ptr) where T : class {
            var finder = new AddressFinder<T>();
            var addr = &finder.addr;
            *(IntPtr*)&addr[-1] = ptr;
            return finder.obj;
        }
    }
}
