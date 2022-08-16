using System.Collections.Generic;

namespace LoadingScreenMod
{
    internal sealed class LinkedHashMap<K, V>
    {
        private sealed class Node
        {
            internal K key;

            internal V val;

            internal Node prev;

            internal Node next;
        }

        private readonly Dictionary<K, Node> map;

        private readonly Node head;

        private Node spare;

        internal int Count => map.Count;

        internal V Eldest => head.next.val;

        internal V this[K key]
        {
            get
            {
                return map[key].val;
            }
            set
            {
                if (map.TryGetValue(key, out var value2))
                {
                    value2.val = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        internal LinkedHashMap(int capacity)
        {
            map = new Dictionary<K, Node>(capacity);
            head = new Node();
            head.prev = head;
            head.next = head;
        }

        internal bool ContainsKey(K key)
        {
            return map.ContainsKey(key);
        }

        internal void Add(K key, V val)
        {
            Node node = CreateNode(key, val);
            map.Add(key, node);
            node.prev = head.prev;
            node.next = head;
            head.prev.next = node;
            head.prev = node;
        }

        internal bool TryGetValue(K key, out V val)
        {
            if (map.TryGetValue(key, out var value))
            {
                val = value.val;
                return true;
            }
            val = default(V);
            return false;
        }

        internal void Reinsert(K key)
        {
            if (map.TryGetValue(key, out var value))
            {
                value.prev.next = value.next;
                value.next.prev = value.prev;
                value.prev = head.prev;
                value.next = head;
                head.prev.next = value;
                head.prev = value;
            }
        }

        internal V Remove(K key)
        {
            if (map.TryGetValue(key, out var value))
            {
                map.Remove(key);
                V val = value.val;
                value.prev.next = value.next;
                value.next.prev = value.prev;
                AddSpare(value);
                return val;
            }
            return default(V);
        }

        internal void RemoveEldest()
        {
            Node next = head.next;
            map.Remove(next.key);
            head.next = next.next;
            next.next.prev = head;
            AddSpare(next);
        }

        internal void Clear()
        {
            while (Count > 0)
            {
                RemoveEldest();
                spare = null;
            }
        }

        private Node CreateNode(K key, V val)
        {
            Node node = spare;
            if (node == null)
            {
                node = new Node();
            }
            else
            {
                spare = node.next;
            }
            node.key = key;
            node.val = val;
            return node;
        }

        private void AddSpare(Node n)
        {
            n.key = default(K);
            n.val = default(V);
            n.prev = null;
            n.next = spare;
            spare = n;
        }
    }
}
