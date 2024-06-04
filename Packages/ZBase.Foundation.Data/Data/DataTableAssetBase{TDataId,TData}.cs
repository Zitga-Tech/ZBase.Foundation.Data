﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace ZBase.Foundation.Data
{
    public abstract class DataTableAssetBase<TDataId, TData> : DataTableAsset, IDataTableAsset
        where TData : IData, IDataWithId<TDataId>
    {
        [SerializeField, FormerlySerializedAs("_rows")]
        private TData[] _entries;

        public ReadOnlyMemory<TData> Entries
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entries;
        }

        protected abstract TDataId GetId(in TData data);

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

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ErrorCannotCast(object obj, UnityEngine.Object context)
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
