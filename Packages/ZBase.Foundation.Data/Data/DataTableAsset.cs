using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public abstract class DataTableAsset : ScriptableObject
    {
        internal abstract void SetRows(object obj);

        internal protected virtual void Initialize() { }

        internal protected virtual void Deinitialize() { }
    }

    public abstract class DataTableAsset<TId, TData> : DataTableAsset
        where TData : IData
    {
        [SerializeField]
        private TData[] _rows;

        public ReadOnlyMemory<TData> Rows
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _rows;
        }

        public virtual bool TryGetRow(TId id, out TData row)
        {
            var span = Rows.Span;
            
            for (var i = 0; i < span.Length; i++)
            {
                ref readonly var item = ref span[i];

                if (GetId(item).Equals(id))
                {
                    row = item;
                    return true;
                }
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
                LogIfCannotCast(obj, this);
            }
        }

        protected abstract TId GetId(in TData row);

        [HideInCallstack]
        protected static void LogIfCannotCast(object obj, UnityEngine.Object context)
        {
            Debug.LogError($"Cannot cast {obj.GetType()} into {typeof(TData[])}", context);
        }
    }
}
