using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls;

namespace Md3.Avalonia.Primitives;

/// <summary>
/// Tracks subscriptions to item change events so collection controls can detach
/// handlers for removed/replaced items. A plain HashSet only prevents duplicate
/// subscriptions; it cannot unsubscribe, which keeps removed items alive through
/// the event source and lets edit callbacks target stale items.
/// </summary>
internal sealed class MdItemEventWiring<TItem, TDelegate>
    where TItem : Control
    where TDelegate : Delegate
{
    private readonly Dictionary<TItem, TDelegate> _handlers =
        new(ReferenceEqualityComparer.Instance);
    private readonly Action<TItem, TDelegate> _subscribe;
    private readonly Action<TItem, TDelegate> _unsubscribe;

    public MdItemEventWiring(
        Action<TItem, TDelegate> subscribe,
        Action<TItem, TDelegate> unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        _subscribe = subscribe;
        _unsubscribe = unsubscribe;
    }

    public bool Add(TItem item, TDelegate handler)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryAdd(item, handler))
            return false;

        _subscribe(item, handler);
        return true;
    }

    public void Remove(TItem item)
    {
        if (!_handlers.Remove(item, out var handler))
            return;

        _unsubscribe(item, handler);
        MdVisualTreeHelpers.DetachFromParent(item);
    }

    public void RemoveRange(IEnumerable? items)
    {
        if (items is null)
            return;

        foreach (var item in items)
        {
            if (item is TItem typed)
                Remove(typed);
        }
    }

    public void HandleCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        // Reset does not report which items disappeared. Clear all handlers and
        // let the owner re-add its current items in the same collection pass.
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            Clear();
            return;
        }

        RemoveRange(e.OldItems);
    }

    public void Clear()
    {
        foreach (var pair in _handlers.ToArray())
        {
            _unsubscribe(pair.Key, pair.Value);
            MdVisualTreeHelpers.DetachFromParent(pair.Key);
        }

        _handlers.Clear();
    }
}
