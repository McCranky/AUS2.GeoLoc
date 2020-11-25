using System;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.Structures.Tables
{
    public class TableItem<TKey, TValue> where TKey : IComparable
    {
        public TKey Key { get; private set; }
        public TValue Value { get; set; }

        public TableItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        //public static implicit operator TableItem<TKey, TValue>(TableItem<TKey, TValue> tableItem)
        //{
        //    return new TableItem<TKey, TValue>(tableItem.Key, tableItem.Value);
        //}
    }
}
