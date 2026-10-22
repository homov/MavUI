namespace Mav.Platform;

/// <summary>
/// Small thread-safe bounded cache with least-recently-used eviction.
/// Used for immutable, cheap-to-recreate render resources (brushes, pens, text layouts).
/// When full, exactly one entry (the least recently used) is evicted per insert, so hot
/// entries survive and the working set is not thrashed by bursts of new keys.
/// </summary>
internal sealed class MavLruCache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _map;
    private readonly LinkedList<Entry> _order = new();
    private readonly object _gate = new();

    /// <summary>Creates a cache that keeps at most <paramref name="capacity"/> entries.</summary>
    public MavLruCache(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<Entry>>(capacity);
    }

    public int Count
    {
        get
        {
            lock (_gate) return _map.Count;
        }
    }

    /// <summary>Returns the cached value for <paramref name="key"/> and marks it as recently used.</summary>
    public bool TryGet(TKey key, out TValue value)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out var node))
            {
                // Move to the front (most recently used).
                _order.Remove(node);
                _order.AddFirst(node);
                value = node.Value.Value;
                return true;
            }
        }

        value = default!;
        return false;
    }

    /// <summary>Stores a value and evicts the least recently used entry when the cache is full.</summary>
    public void Set(TKey key, TValue value)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out var existing))
            {
                existing.Value = new Entry(key, value);
                _order.Remove(existing);
                _order.AddFirst(existing);
                return;
            }

            var node = _order.AddFirst(new Entry(key, value));
            _map[key] = node;

            if (_map.Count > _capacity)
            {
                var last = _order.Last!;
                _order.RemoveLast();
                _map.Remove(last.Value.Key);
            }
        }
    }

    /// <summary>Removes all entries from the cache.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _map.Clear();
            _order.Clear();
        }
    }

    private struct Entry
    {
        /// <summary>
        /// Creates a cache entry that holds the key and its value.
        /// </summary>
        public Entry(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public TKey Key { get; }
        public TValue Value { get; }
    }
}
