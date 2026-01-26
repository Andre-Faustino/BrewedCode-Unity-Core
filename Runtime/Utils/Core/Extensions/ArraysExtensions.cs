using System;

namespace BrewedCode.Utils
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Adds an item at the end of the array.
        /// </summary>
        public static T[] Add<T>(this T[] array, T item)
        {
            if (array == null)
                return new T[] { item };

            T[] newArray = new T[array.Length + 1];
            array.CopyTo(newArray, 0);
            newArray[array.Length] = item;
            return newArray;
        }

        /// <summary>
        /// Adds multiple items at the end of the array.
        /// </summary>
        public static T[] AddRange<T>(this T[] array, params T[] items)
        {
            if (array == null) return items;

            T[] newArray = new T[array.Length + items.Length];
            array.CopyTo(newArray, 0);
            items.CopyTo(newArray, array.Length);
            return newArray;
        }

        /// <summary>
        /// Inserts an item at the specified index.
        /// </summary>
        public static T[] InsertAt<T>(this T[] array, int index, T item)
        {
            if (array == null) return new T[] { item };
            if (index < 0) index = 0;
            if (index > array.Length) index = array.Length;

            T[] newArray = new T[array.Length + 1];
            Array.Copy(array, 0, newArray, 0, index);
            newArray[index] = item;
            Array.Copy(array, index, newArray, index + 1, array.Length - index);
            return newArray;
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            if (array == null || index < 0 || index >= array.Length)
                return array;

            T[] newArray = new T[array.Length - 1];
            if (index > 0)
                Array.Copy(array, 0, newArray, 0, index);
            if (index < array.Length - 1)
                Array.Copy(array, index + 1, newArray, index, array.Length - index - 1);

            return newArray;
        }

        /// <summary>
        /// Removes the first occurrence of the given item.
        /// </summary>
        public static T[] Remove<T>(this T[] array, T item)
        {
            if (array == null) return null;

            int index = Array.IndexOf(array, item);
            if (index < 0) return array;

            return array.RemoveAt(index);
        }

        /// <summary>
        /// Checks if the array is null or has no elements.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this T[] array)
        {
            return array == null || array.Length == 0;
        }
    }
}
