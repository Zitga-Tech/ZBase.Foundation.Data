using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZBase.Foundation.Data
{
    using TableAssetRef = LazyLoadReference<DataTableAsset>;

    public class DatabaseAsset : ScriptableObject
    {
        [SerializeField]
        internal TableAssetRef[] _assetRefs = Array.Empty<TableAssetRef>();

        [SerializeField]
        internal TableAssetRef[] _redundantAssetRefs = Array.Empty<TableAssetRef>();

        protected readonly Dictionary<string, DataTableAsset> AssetMap = new();

        protected ReadOnlyMemory<TableAssetRef> AssetRefs => _assetRefs;

        protected ReadOnlyMemory<TableAssetRef> RedundantAssetRefs => _redundantAssetRefs;

        public bool Initialized { get; protected set; }

        public virtual void Initialize()
        {
            if (Initialized)
            {
                return;
            }

            var assetRefs = AssetRefs.Span;
            var assetMap = AssetMap;

            assetMap.Clear();
            assetMap.EnsureCapacity(assetRefs.Length);

            for (var i = 0; i < assetRefs.Length; i++)
            {
                var assetRef = assetRefs[i];

                if (assetRef.isSet == false || assetRef.isBroken)
                {
                    LogIfReferenceIsInvalid(i, this);
                    continue;
                }

                var asset = assetRef.asset;

                if (asset == false)
                {
                    LogIfAssetIsInvalid(i, this);
                    continue;
                }

                var type = asset.GetType();
                assetMap[type.Name] = asset;
                asset.Initialize();
            }

            Initialized = true;
        }

        public virtual void Deinitialize()
        {
            if (Initialized == false)
            {
                return;
            }

            foreach (var asset in AssetMap.Values)
            {
                asset.Deinitialize();
            }

            Initialized = false;
        }

        public bool TryGetDataTableAsset(string name, out DataTableAsset tableAsset)
        {
            if (Initialized == false)
            {
                LogIfDatabaseIsNotInitialized(this);
                tableAsset = null;
                return false;
            }

            if (AssetMap.TryGetValue(name, out var asset))
            {
                tableAsset = asset;
                return true;
            }
            else
            {
                LogIfCannotFindAsset(name, this);
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

            if (Initialized == false)
            {
                LogIfDatabaseIsNotInitialized(this);
                tableAsset = null;
                return false;
            }

            if (AssetMap.TryGetValue(name, out var asset))
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
                LogIfCannotFindAsset(name, this);
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
            if (Initialized == false)
            {
                LogIfDatabaseIsNotInitialized(this);
                tableAsset = null;
                return false;
            }

            if (AssetMap.TryGetValue(name, out var asset))
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
                LogIfCannotFindAsset(name, this);
            }

            tableAsset = null;
            return false;
        }

        [HideInCallstack]
        private static void LogIfReferenceIsInvalid(int index, DatabaseAsset context)
        {
            Debug.LogError($"Table Asset reference at index {index} is invalid.", context);
        }

        [HideInCallstack]
        private static void LogIfAssetIsInvalid(int index, DatabaseAsset context)
        {
            Debug.LogError($"Table Asset at index {index} is invalid.", context);
        }

        [HideInCallstack]
        private static void LogIfDatabaseIsNotInitialized(DatabaseAsset context)
        {
            Debug.LogError($"The database is not initialized yet. Please invoke {nameof(Initialize)} method beofre using.", context);
        }

        [HideInCallstack]
        private static void LogIfCannotFindAsset(string name, DatabaseAsset context)
        {
            Debug.LogError($"Cannot find any data table asset named {name}.", context);
        }

        [HideInCallstack]
        private static void LogIfFoundAssetIsNotValidType<T>(DataTableAsset context)
        {
            Debug.LogError($"The data table asset is not an instance of {typeof(T)}", context);
        }

        [HideInCallstack]
        private static void LogIfFoundAssetIsNotValidType(Type type, DataTableAsset context)
        {
            Debug.LogError($"The data table asset is not an instance of {type}", context);
        }
    }
}
