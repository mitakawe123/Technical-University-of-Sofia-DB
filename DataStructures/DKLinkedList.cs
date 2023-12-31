using System.Collections;

namespace DataStructures;

public class DKLinkedList<T> : ICollection<T>, IReadOnlyCollection<T>
{
    public Node<T> Head { get; private set; }
    public Node<T> Tail { get; private set; }
    public int Count { get; private set; }
    public bool IsReadOnly => false;

    public void AddFirst(T value)
    {
        var newNode = new Node<T>(value);

        if (Head is null)
        {
            Head = Tail = newNode;
        }
        else
        {
            newNode.Next = Head;
            Head.Previous = newNode;
            Head = newNode;
        }

        Count++;
    }

    public void AddLast(T value)
    {
        var newNode = new Node<T>(value);

        if (Head is null)
        {
            Head = Tail = newNode;
        }
        else
        {
            Tail.Next = newNode;
            newNode.Previous = Tail;
            Tail = newNode;
        }

        Count++;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("The destination array is not large enough to contain the elements.");

        Node<T> current = Head;
        while (current is not null)
        {
            array[arrayIndex++] = current.Value;
            current = current.Next;
        }
    }

    public bool Remove(T value)
    {
        var current = Head;

        while (current is not null)
        {
            if (!current.Value.Equals(value))
                current = current.Next;
            else
            {
                if (current.Previous is not null)
                    current.Previous.Next = current.Next;
                else
                    Head = current.Next;

                if (current.Next is not null)
                    current.Next.Previous = current.Previous;
                else
                    Tail = current.Previous;

                Count--;
                return true;
            }
        }

        return false;
    }

    public void Add(T item)
    {
        AddLast(item);
    }

    public void Clear()
    {
        Head = null;
        Tail = null;
        Count = 0;
    }

    public bool Contains(T value)
    {
        var current = Head;
        while (current is not null)
        {
            if (current.Value.Equals(value))
                return true;

            current = current.Next;
        }

        return false;
    }

    public IEnumerator<T> GetEnumerator()
    {
        Node<T> current = Head;
        while (current is not null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class Node<T>
{
    public T Value { get; set; }
    public Node<T> Next { get; set; }
    public Node<T> Previous { get; set; }

    public Node(T value)
    {
        Value = value;
    }
}