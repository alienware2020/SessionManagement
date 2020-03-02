using System;
using System.Collections.Generic;

namespace TrainingSessionManagement.Helper
{
    public static class CollectionExtension
    {
        private static Random _random = new Random();

        public static T RandomElement<T>(this IList<T> list)
        {
            return list[_random.Next(list.Count)];
        }

        public static T RandomElement<T>(this T[] array)
        {
            return array[_random.Next(array.Length)];
        }
    }
}
