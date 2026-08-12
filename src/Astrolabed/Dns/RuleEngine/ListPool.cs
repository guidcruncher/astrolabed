using System;
using System.Collections.Generic;
using System.Threading;

namespace Astrolabed.Utils;

internal static class ListPool<T>
{
    private const int MaxCapacity = 1024;
    private const int PoolSize = 32;

    [ThreadStatic]
    private static List<T>? t_fastItem;

    private static readonly List<T>?[] _items = new List<T>?[PoolSize];

    public static List<T> Rent()
    {
        // Fast Path: Thread-local slot (zero atomic instructions)
        var item = t_fastItem;
        if (item is not null)
        {
            t_fastItem = null;
            return item;
        }

        // Slow Path: Lock-free shared array
        var items = _items;
        for (int i = 0; i < items.Length; i++)
        {
            item = Interlocked.Exchange(ref items[i], null);
            if (item is not null)
            {
                return item;
            }
        }

        return new List<T>();
    }

    public static void Return(List<T>? list)
    {
        if (list is null || list.Capacity > MaxCapacity)
        {
            return;
        }

        // Clear immediately so contained items can be GC-collected while idle
        list.Clear();

        // Fast Path: Thread-local slot
        if (t_fastItem is null)
        {
            t_fastItem = list;
            return;
        }

        // Slow Path: Lock-free shared array slot
        var items = _items;
        for (int i = 0; i < items.Length; i++)
        {
            if (Interlocked.CompareExchange(ref items[i], list, null) is null)
            {
                return;
            }
        }
    }
}
