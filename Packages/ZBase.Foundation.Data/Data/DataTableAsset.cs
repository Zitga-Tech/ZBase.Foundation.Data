using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace ZBase.Foundation.Data
{
    public abstract class DataTableAsset : ScriptableObject
    {
        internal abstract void SetEntries(object obj);

        internal protected virtual void Initialize() { }

        internal protected virtual void Deinitialize() { }
    }

    public abstract class DataTableAsset<TId, TData> : DataTableAsset
        where TData : IData
    {
        [SerializeField, FormerlySerializedAs("_rows")]
        private TData[] _entries;

        public ReadOnlyMemory<TData> Entries
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entries;
        }

        public virtual bool TryGetEntry(TId id, out TData entry)
        {
            var span = Entries.Span;
            var length = span.Length;

            for (var i = 0; i < length; i++)
            {
                ref readonly var item = ref span[i];

                if (GetId(item).Equals(id))
                {
                    entry = item;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public virtual DataEntry<TData> GetEntry(TId id)
        {
            var span = Entries.Span;
            var length = span.Length;

            for (var i = 0; i < length; i++)
            {
                ref readonly var item = ref span[i];

                if (GetId(item).Equals(id))
                {
                    return new(Entries.Slice(i, 1));
                }
            }

            return default;
        }
        
        public virtual DataEntryRef<TData> GetEntryByRef(TId id)
        {
            var span = Entries.Span;
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

        internal sealed override void SetEntries(object obj)
        {
            if (obj is TData[] entries)
            {
                _entries = entries;
            }
            else
            {
                _entries = Array.Empty<TData>();
                LogIfCannotCast(obj, this);
            }
        }

        protected abstract TId GetId(in TData row);

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        protected static void LogIfCannotCast(object obj, UnityEngine.Object context)
        {
            if (obj == null)
            {
                UnityEngine.Debug.LogError($"Cannot cast null into {typeof(TData[])}", context);
            }
            else
            {
                UnityEngine.Debug.LogError($"Cannot cast {obj.GetType()} into {typeof(TData[])}", context);
            }
        }
    }
}
