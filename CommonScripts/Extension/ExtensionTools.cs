using System;
using System.Collections.Generic;

public static class Extensions
{
    public static void Swap<T>(this List<T> list, int i, int j)
    {
        T temp = list[i];
        list[i] = list[j];
        list[j] = temp;
    }
}
