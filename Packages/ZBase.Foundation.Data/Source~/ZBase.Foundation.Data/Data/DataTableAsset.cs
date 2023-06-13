using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public abstract class DataTableAsset : ScriptableObject
    {
        internal abstract void SetRows(object obj);

        internal protected virtual void Initialize() { }
    }

    public abstract class DataTableAsset<TId, TData> : DataTableAsset, ISerializationCallbackReceiver
        where TData : IData
    {
        [SerializeField]
        private TData[] _rows;

        private readonly Dictionary<TId, int> _rowMap = new();

        public ReadOnlyMemory<TData> Rows
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _rows;
        }

        public bool TryGetRow(TId id, out TData row)
        {
            if (_rowMap.TryGetValue(id, out var index))
            {
                row = _rows[index];
                return true;
            }

            row = default;
            return false;
        }

        internal sealed override void SetRows(object obj)
        {
            if (obj is TData[] rows)
            {
                _rows = rows;
            }
            else
            {
                _rows = Array.Empty<TData>();
                Debug.LogError($"Cannot cast {obj.GetType()} into {typeof(TData[])}");
            }
        }

        protected abstract TId GetId(in TData row);

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            var rowMap = _rowMap;
            rowMap.Clear();

            var rows = _rows;
            var length = rows.Length;
            rowMap.EnsureCapacity(length);

            for (var i = 0; i < length; i++)
            {
                var row = rows[i];
                var id = GetId(row);
                rowMap[id] = i;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }
    }
}
