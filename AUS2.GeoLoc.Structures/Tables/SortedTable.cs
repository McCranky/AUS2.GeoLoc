using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AUS2.GeoLoc.Structures.Tables
{
    public class SortedTable<TKey, TValue> where TKey : IComparable
    {
        public List<TableItem<TKey, TValue>> Items { get; set; }

        public SortedTable()
        {
            Items = new List<TableItem<TKey, TValue>>();
        }

        public TableItem<TKey, TValue> this[TKey key] {
            get {
                return FindTableItem(key);
            }
        }

        public int Count => Items.Count;
        public List<TKey> DescendingKeys => Items.Select(item => item.Key).OrderByDescending(key => key).ToList();

        public void Add(TKey key, TValue value)
        {
            var index = IndexOfKey(key, 0, Items.Count, out var found);
            if (found) {
                throw new ArgumentException("Item with key is already added.");
            }
            Items.Insert(index, new TableItem<TKey, TValue>(key, value));
        }

        public TValue Remove(TKey key)
        {
            var item = FindTableItem(key);
            if (item != null) {
                Items.Remove(item);
                return item.Value;
            } else {
                throw new ArgumentException("Item with given key does not exist.");
            }
        }

        public TableItem<TKey, TValue> FindTableItem(TKey key)
        {
            var index = IndexOfKey(key, 0, Items.Count, out var found);
            return found ? Items[index] : null;
        }

        public void RemoveRange(int from, int count)
        {
            //if (from > to || from < 0 || from >= Count || to >= Count) return;
            Items.RemoveRange(from, count);
        }

        public void Clear()
        {
            Items.Clear();
        }

        private int IndexOfKey(TKey key, int startIndex, int endIndex, out bool found)
        {
            found = false;

            while (true) {
                if (startIndex == Items.Count)
                    return Items.Count;

                var middleIndex = (startIndex + endIndex) / 2;
                var currentKey = Items[middleIndex].Key;

                if (key.CompareTo(currentKey) == 0) {
                    found = true;
                    return middleIndex;
                } else {
                    if (startIndex == endIndex)
                        return key.CompareTo(currentKey) < 0 ? middleIndex : middleIndex + 1;
                    else {
                        if (currentKey.CompareTo(key) < 0) {
                            startIndex = middleIndex + 1;
                        } else {
                            endIndex = middleIndex;
                        }
                    }
                }
            }

        }
    }

}
