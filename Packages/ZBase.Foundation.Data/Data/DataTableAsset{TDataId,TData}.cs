using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace ZBase.Foundation.Data
{
    public abstract class DataTableAsset<TDataId, TData> : DataTableAsset, IDataTableAsset
        where TData : IData, IDataWithId<TDataId>
    {
        [SerializeField, FormerlySerializedAs("_rows")]
        private TData[] _entries;

        private readonly Dictionary<TDataId, int> _idToIndexMap = new();

        public ReadOnlyMemory<TData> Entries
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entries;
        }

        protected Dictionary<TDataId, int> IdToIndexMap
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
                var id = GetId(entries[i]);

                if (map.TryAdd(id, i) == false)
                {
                    ErrorDuplicateId(id, i, this);
                    continue;
                }
            }
        }

        public virtual bool TryGetEntry(TDataId id, out TData entry)
        {
            var result = _idToIndexMap.TryGetValue(id, out var index);
            entry = result ? Entries.Span[index] : default;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual DataEntry<TData> GetEntry(TDataId id)
        {
            return _idToIndexMap.TryGetValue(id, out var index) ? new(Entries.Slice(index, 1)) : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual DataEntryRef<TData> GetEntryByRef(TDataId id)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual string ToString(TDataId value)
            => value.ToString();

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        protected static void ErrorDuplicateId(TDataId id, int index, [NotNull] DataTableAsset<TDataId, TData> context)
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
