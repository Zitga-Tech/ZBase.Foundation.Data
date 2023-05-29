using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    public sealed class DatabaseAsset : ScriptableObject
    {
        [SerializeField, HideInInspector]
        internal TableAssetRef[] _assetRefs = Array.Empty<TableAssetRef>();

        [SerializeField, HideInInspector]
        internal TableAssetRef[] _redundantAssetRefs = Array.Empty<TableAssetRef>();

        private readonly Dictionary<string, DataTableAsset> _assetMap = new();
        private bool _initialized;

        public void Initialize()
        {
            var assetRefs = _assetRefs;
            var assetMap = _assetMap;

            assetMap.Clear();
            assetMap.EnsureCapacity(assetRefs.Length);

            foreach (var assetRef in assetRefs)
            {
                assetMap[assetRef.name] = assetRef.reference.asset;
            }

            _initialized = true;
        }

        public bool TryGetDataTableAsset(string name, out DataTableAsset tableAsset)
        {
            if (_initialized == false)
            {
                LogIfDatabaseIsNotInitialized();
                tableAsset = null;
                return false;
            }

            if (_assetMap.TryGetValue(name, out var asset))
            {
                tableAsset = asset;
                return true;
            }
            else
            {
                LogIfCannotFindAsset(name);
            }

            tableAsset = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetDataTableAsset(Type type, out DataTableAsset tableAsset)
            => TryGetDataTableAsset(type, type.Name, out tableAsset);

        public bool TryGetDataTableAsset(Type type, string name, out DataTableAsset tableAsset)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (_initialized == false)
            {
                LogIfDatabaseIsNotInitialized();
                tableAsset = null;
                return false;
            }

            if (_assetMap.TryGetValue(name, out var asset))
            {
                if (asset.GetType() == type)
                {
                    tableAsset = asset;
                    return true;
                }
                else
                {
                    LogIfFoundAssetIsNotValidType(type, asset);
                }
            }
            else
            {
                LogIfCannotFindAsset(name);
            }

            tableAsset = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetDataTableAsset<T>(out T tableAsset)
            where T : DataTableAsset
            => TryGetDataTableAsset<T>(typeof(T).Name, out tableAsset);

        public bool TryGetDataTableAsset<T>(string name, out T tableAsset)
            where T : DataTableAsset
        {
            if (_initialized == false)
            {
                LogIfDatabaseIsNotInitialized();
                tableAsset = null;
                return false;
            }

            if (_assetMap.TryGetValue(name, out var asset))
            {
                if (asset is T assetT)
                {
                    tableAsset = assetT;
                    return true;
                }
                else
                {
                    LogIfFoundAssetIsNotValidType<T>(asset);
                }
            }
            else
            {
                LogIfCannotFindAsset(name);
            }

            tableAsset = null;
            return false;
        }

        internal void Clear()
        {
            _assetRefs = Array.Empty<TableAssetRef>();
            _redundantAssetRefs = Array.Empty<TableAssetRef>();
        }

        internal void AddRange(
              IEnumerable<DataTableAsset> assets
            , IEnumerable<DataTableAsset> redundantAssets
        )
        {
            if (assets == null)
            {
                throw new NullReferenceException(nameof(assets));
            }

            var list = new List<TableAssetRef>();

            foreach (var asset in assets)
            {
                list.Add(new TableAssetRef {
                    name = asset.GetType().Name,
                    reference = asset,
                });
            }

            _assetRefs = list.ToArray();
            list.Clear();

            if (redundantAssets != null)
            {
                foreach (var asset in redundantAssets)
                {
                    list.Add(new TableAssetRef {
                        name = asset.GetType().Name,
                        reference = asset,
                    });
                }

                _redundantAssetRefs = list.ToArray();
                list.Clear();
            }
        }

        private void LogIfDatabaseIsNotInitialized()
        {
            Debug.LogError($"The database is not initialized yet. Please invoke {nameof(Initialize)} method beofre using.", this);
        }

        private void LogIfCannotFindAsset(string name)
        {
            Debug.LogError($"Cannot find any data table asset named {name}.", this);
        }

        private void LogIfFoundAssetIsNotValidType<T>(DataTableAsset asset)
        {
            Debug.LogError($"The data table asset is not an instance of {typeof(T)}", asset);
        }

        private void LogIfFoundAssetIsNotValidType(Type type, DataTableAsset asset)
        {
            Debug.LogError($"The data table asset is not an instance of {type}", asset);
        }

        [Serializable]
        internal struct TableAssetRef
        {
            [HideInInspector]
            public string name;

            [HideInInspector]
            public LazyLoadReference<DataTableAsset> reference;
        }
    }
}
