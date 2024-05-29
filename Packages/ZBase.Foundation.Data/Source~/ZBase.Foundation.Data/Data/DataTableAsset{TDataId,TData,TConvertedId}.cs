using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public abstract class DataTableAsset<TDataId, TData, TConvertedId> : DataTableAsset
        where TData : IData, IDataWithId<TDataId>
    {
        [SerializeField]
        private TData[] _entries;

        private readonly Dictionary<TConvertedId, int> _idToIndexMap = new();

        public ReadOnlyMemory<TData> Entries
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entries;
        }

        protected Dictionary<TConvertedId, int> IdToIndexMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _idToIndexMap;
        }

        internal protected override void Initialize()
        {
            var map = _idToIndexMap;
            var entries = Entries.Span;

            map.Clear();
            map.EnsureCapacity(entries.Length);

            for (var i = 0; i < entries.Length; i++)
            {
                var id = Convert(GetId(entries[i]));

                if (map.TryAdd(id, i) == false)
                {
                    ErrorDuplicateId(id, i, this);
                    continue;
                }
            }
        }

        public virtual bool TryGetEntry(TConvertedId id, out TData entry)
        {
            var result = _idToIndexMap.TryGetValue(id, out var index);
            entry = result ? Entries.Span[index] : default;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual DataEntry<TData> GetEntry(TConvertedId id)
        {
            return _idToIndexMap.TryGetValue(id, out var index) ? new(Entries.Slice(index, 1)) : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual DataEntryRef<TData> GetEntryByRef(TConvertedId id)
        {
            return _idToIndexMap.TryGetValue(id, out var index) ? new(Entries.Span.Slice(index, 1)) : default;
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
                ErrorCannotCast(obj, this);
            }
        }

        protected abstract TDataId GetId(in TData data);

        protected abstract TConvertedId Convert(TDataId value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual string ToString(TConvertedId value)
            => value.ToString();

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        protected static void ErrorDuplicateId(TConvertedId id, int index, DataTableAsset<TDataId, TData, TConvertedId> context)
        {
            UnityEngine.Debug.LogErrorFormat(
                  context
                , "Id with value \"{0}\" is duplicated at row \"{1}\""
                , context.ToString(id)
                , index
            );
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        protected static void ErrorCannotCast(object obj, UnityEngine.Object context)
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
