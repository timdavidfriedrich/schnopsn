namespace Schnopsn.core.Utilities;

using System;
using System.Collections.Generic;


public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        if (list == null || list.Count <= 1) return;
        var rng = Random.Shared;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
