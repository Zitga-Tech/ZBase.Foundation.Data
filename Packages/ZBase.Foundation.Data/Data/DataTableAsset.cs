using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            var length = span.Length;

            for (var i = 0; i < length; i++)
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

        public virtual DataRef<TData> GetRowRef(TId id)
        {
            var span = Rows.Span;
            var length = span.Length;

            for (var i = 0; i < length; i++)
            {
                ref readonly var item = ref span[i];

                if (GetId(item).Equals(id))
                {
                    return new(span.Slice(i, 1));
                }
            }

            return default;
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

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        protected static void LogIfCannotCast(object obj, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError($"Cannot cast {obj.GetType()} into {typeof(TData[])}", context);
        }
    }
}
