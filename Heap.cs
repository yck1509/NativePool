using System;

class Heap<T> where T : struct, IComparable<T> {
    T[] heap;
    int count;

    public int Count => count;

    public Heap() {
        heap = Array.Empty<T>();
        count = 0;
    }

    public void Clear() {
        Array.Clear(heap, 0, count);
        count = 0;
    }

    public void Put(T item) {
        if (count == heap.Length)
            Array.Resize(ref heap, heap.Length * 2 + 1);

        heap[count++] = item;
        UpHeap(count - 1);
    }

    public void PutAll(T[] items) {
        if (count + items.Length >= heap.Length)
            Array.Resize(ref heap, Math.Max(heap.Length * 2, count + items.Length));

        for (int i = 0; i < items.Length; i++)
            heap[count++] = items[i];

        MakeHeap();
    }

    public T Take() {
        if (count == 0)
            throw new InvalidOperationException("Cannot take from empty heap!");
        T item = heap[0];
        heap[0] = heap[--count];
        DownHeap(0);
        return item;
    }

    void Swap(int a, int b) {
        T tmp = heap[a];
        heap[a] = heap[b];
        heap[b] = tmp;
    }

    void UpHeap(int index) {
        while (index > 0) {
            int parent = (index - 1) / 2;
            if (heap[index].CompareTo(heap[parent]) >= 0)
                break;
            Swap(index, parent);
            index = parent;
        }
    }

    void DownHeap(int index) {
        while (index < count) {
            int left = 2 * index + 1,
                right = 2 * index + 2,
                target = index;

            if (left < count && heap[left].CompareTo(heap[target]) < 0)
                target = left;
            if (right < count && heap[right].CompareTo(heap[target]) < 0)
                target = right;

            if (target == index)
                break;

            Swap(index, target);
            index = target;
        }
    }

    void MakeHeap() {
        for (int index = count - 1; index >= 0; index--) {
            int parent = (index - 1) / 2;
            if (heap[index].CompareTo(heap[parent]) < 0)
                Swap(index, parent);
        }
    }
}
