using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public sealed class DatabaseAsset : ScriptableObject
    {
        [SerializeField, HideInInspector]
        internal Asset[] _assets = new Asset[0];

        private readonly Dictionary<string, LazyLoadReference<DataTableAsset>> _assetMap = new();
        
        public void LazyInitialize()
        {
            var assets = _assets;
            var assetMap = _assetMap;

            assetMap.Clear();
            assetMap.EnsureCapacity(assets.Length);

            foreach (var asset in assets)
            {
                assetMap[asset.name] = asset.reference;
            }
        }

        public bool TryGetDataTableAsset<T>(out T tableAsset)
            where T : DataTableAsset
            => TryGetDataTableAsset<T>(typeof(T).Name, out tableAsset);

        public bool TryGetDataTableAsset<T>(string name, out T tableAsset)
            where T : DataTableAsset
        {
            if (_assetMap.TryGetValue(name, out var reference)
                && reference.asset is T assetT
            )
            {
                tableAsset = assetT;
                return true;
            }

            tableAsset = null;
            return false;
        }

        internal void Clear()
        {
            _assets = new Asset[0];
        }

        internal void AddRange(IEnumerable<DataTableAsset> assets)
        {
            if (assets == null)
            {
                throw new NullReferenceException(nameof(assets));
            }

            var list = new List<Asset>();

            foreach (var asset in assets)
            {
                list.Add(new Asset {
                    name = asset.GetType().Name,
                    reference = asset,
                });
            }

            _assets = list.ToArray();
        }

        [Serializable]
        internal class Asset
        {
            [HideInInspector]
            public string name;

            [HideInInspector]
            public LazyLoadReference<DataTableAsset> reference;
        }
    }
}
