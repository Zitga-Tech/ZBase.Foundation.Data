using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public sealed class DatabaseAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        private DataTableAsset[] _tableAssets = new DataTableAsset[0];

        private readonly Dictionary<string, DataTableAsset> _tableAssetMap = new();

        public ReadOnlyMemory<DataTableAsset> TableAssets
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _tableAssets;
        }

        public bool TryGetTableAsset<T>(string name, out T tableAsset)
            where T : DataTableAsset
        {
            if (_tableAssetMap.TryGetValue(name, out var asset) && asset is T assetT)
            {
                tableAsset = assetT;
                return true;
            }

            tableAsset = null;
            return false;
        }

        internal void Clear()
        {
            _tableAssets = new DataTableAsset[0];
        }

        internal void AddRange(IEnumerable<DataTableAsset> assets)
        {
            if (assets == null)
            {
                throw new NullReferenceException(nameof(assets));
            }

            _tableAssets = assets.Where(x => x == true).ToArray();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            var assets = _tableAssets;
            var assetMap = _tableAssetMap;
            assetMap.Clear();
            assetMap.EnsureCapacity(assets.Length);

            foreach (var asset in assets)
            {
                assetMap[asset.name] = asset;
            }
        }
    }
}
