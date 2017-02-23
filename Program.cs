using System;
using System.Numerics;
using Unsafe;

namespace NativeHeap {
    class TestObject : NativeObject {
        BigInteger bigValue;
        Guid guid;

        public void Initialize(int num) {
            bigValue = new BigInteger(num);
            guid = Guid.NewGuid();
        }

        public override string ToString() => $"{bigValue}: {guid}";
    }

    class Program {
        static void PrintInfo<T>(NativePool<T> pool) where T : NativeObject {
            Console.WriteLine($"full: {pool.IsFull}; numObjs: {pool.NumObjects}");
        }

        static void Main(string[] args) {
            const int NumObjs = 10, Iterations = 100;

            using (var pool = new NativePool<TestObject>(2, 2)) {
                var objs = new TestObject[NumObjs];
                var rand = new Random();

                for (int i = 0; i < Iterations; i++) {
                    int index = rand.Next(NumObjs);
                    var obj = objs[index];
                    if (obj == null) {
                        objs[index] = (TestObject)pool.New();
                        GC.Collect();
                        objs[index].Initialize(i);
                        GC.Collect();
                    } else if (rand.Next() % 2 == 0) {
                        objs[index].Free();
                        GC.Collect();
                        objs[index] = null;
                        GC.Collect();
                    }
                    Console.WriteLine($"iteration {i}: objs[{index}] = {objs[index]?.ToString() ?? "null"}");
                    PrintInfo(pool);
                    Console.WriteLine();
                    GC.Collect();
                }

                Console.WriteLine("-----");
                foreach (var obj in pool) {
                    Console.WriteLine(obj);
                    obj.Free();
                    PrintInfo(pool);
                }
            }
        }
    }
}
