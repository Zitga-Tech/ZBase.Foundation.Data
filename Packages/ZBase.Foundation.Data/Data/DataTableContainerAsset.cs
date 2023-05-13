using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public sealed class DataTableContainerAsset : ScriptableObject
    {
        [SerializeField]
        private DataTableAsset[] _tableAssets = new DataTableAsset[0];

        public ReadOnlyMemory<DataTableAsset> TableAssets
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _tableAssets;
        }

        internal void Clear()
        {
            _tableAssets = new DataTableAsset[0];
        }

        internal void Add(DataTableAsset tableAsset)
        {
            if (tableAsset == false)
            {
                throw new NullReferenceException(nameof(tableAsset));
            }

            var lastIndex = _tableAssets.Length;
            Array.Resize(ref _tableAssets, lastIndex + 1);
            _tableAssets[lastIndex] = tableAsset;
        }
    }
}
