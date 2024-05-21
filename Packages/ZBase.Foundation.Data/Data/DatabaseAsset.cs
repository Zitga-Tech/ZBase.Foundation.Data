﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        private readonly Dictionary<string, DataTableAsset> _nameToAsset = new();
        private readonly Dictionary<Type, DataTableAsset> _typeToAsset = new();

        protected IReadOnlyDictionary<string, DataTableAsset> NameToAsset => _nameToAsset;

        protected IReadOnlyDictionary<Type, DataTableAsset> TypeToAsset => _typeToAsset;

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
            var assetsLength = assetRefs.Length;
            var nameToAsset = _nameToAsset;
            var typeToAsset = _typeToAsset;

            nameToAsset.Clear();
            nameToAsset.EnsureCapacity(assetsLength);
            
            typeToAsset.Clear();
            typeToAsset.EnsureCapacity(assetsLength);

            for (var i = 0; i < assetsLength; i++)
            {
                var assetRef = assetRefs[i];

                if (assetRef.isSet == false || assetRef.isBroken)
                {
                    LogErrorReferenceIsInvalid(i, this);
                    continue;
                }

                var asset = assetRef.asset;

                if (asset == false)
                {
                    LogErrorAssetIsInvalid(i, this);
                    continue;
                }

                var type = asset.GetType();
                nameToAsset[type.Name] = asset;
                typeToAsset[type] = asset;

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

            Initialized = false;

            foreach (var asset in _nameToAsset.Values)
            {
                asset.Deinitialize();
            }

            _nameToAsset.Clear();
            _typeToAsset.Clear();
        }

        public bool TryGetDataTableAsset(string name, out DataTableAsset tableAsset)
        {
            ThrowIfNull(name);

            if (Initialized == false)
            {
                LogErrorDatabaseIsNotInitialized(this);
                tableAsset = null;
                return false;
            }

            if (_nameToAsset.TryGetValue(name, out var asset))
            {
                tableAsset = asset;
                return true;
            }
            else
            {
                LogErrorCannotFindAsset(name, this);
            }

            tableAsset = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetDataTableAsset(Type type, out DataTableAsset tableAsset)
        {
            ThrowIfNull(type);

            if (Initialized == false)
            {
                LogErrorDatabaseIsNotInitialized(this);
                tableAsset = null;
                return false;
            }

            if (_typeToAsset.TryGetValue(type, out var asset))
            {
                tableAsset = asset;
                return true;
            }
            else
            {
                LogErrorCannotFindAsset(type, this);
            }

            tableAsset = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetDataTableAsset<T>(out T tableAsset)
            where T : DataTableAsset
        {
            if (Initialized == false)
            {
                LogErrorDatabaseIsNotInitialized(this);
                tableAsset = null;
                return false;
            }

            var type = typeof(T);

            if (_typeToAsset.TryGetValue(type, out var asset))
            {
                if (asset is T assetT)
                {
                    tableAsset = assetT;
                    return true;
                }
                else
                {
                    LogErrorFoundAssetIsNotValidType<T>(asset);
                }
            }
            else
            {
                LogErrorCannotFindAsset(type, this);
            }

            tableAsset = null;
            return false;
        }

        public bool TryGetDataTableAsset<T>(string name, out T tableAsset)
            where T : DataTableAsset
        {
            ThrowIfNull(name);

            if (Initialized == false)
            {
                LogErrorDatabaseIsNotInitialized(this);
                tableAsset = null;
                return false;
            }

            if (_nameToAsset.TryGetValue(name, out var asset))
            {
                if (asset is T assetT)
                {
                    tableAsset = assetT;
                    return true;
                }
                else
                {
                    LogErrorFoundAssetIsNotValidType<T>(asset);
                }
            }
            else
            {
                LogErrorCannotFindAsset(name, this);
            }

            tableAsset = null;
            return false;
        }

        [Obsolete("Use other TryGetDataTableAsset overloads instead.", false)]
        public bool TryGetDataTableAsset(Type type, string name, out DataTableAsset tableAsset)
        {
            ThrowIfNull(name);
            ThrowIfNull(type);

            if (Initialized == false)
            {
                LogErrorDatabaseIsNotInitialized(this);
                tableAsset = null;
                return false;
            }

            if (_nameToAsset.TryGetValue(name, out var asset))
            {
                if (asset.GetType() == type)
                {
                    tableAsset = asset;
                    return true;
                }
                else
                {
                    LogErrorFoundAssetIsNotValidType(type, asset);
                }
            }
            else
            {
                LogErrorCannotFindAsset(name, this);
            }

            tableAsset = null;
            return false;
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ThrowIfNull(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
        }
        
        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ThrowIfNull(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
        }
        
        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogErrorReferenceIsInvalid(int index, DatabaseAsset context)
        {
            UnityEngine.Debug.LogError($"Table Asset reference at index {index} is invalid.", context);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogErrorAssetIsInvalid(int index, DatabaseAsset context)
        {
            UnityEngine.Debug.LogError($"Table Asset at index {index} is invalid.", context);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogErrorDatabaseIsNotInitialized(DatabaseAsset context)
        {
            UnityEngine.Debug.LogError($"The database is not initialized yet. Please invoke {nameof(Initialize)} method beofre using.", context);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogErrorCannotFindAsset(string name, DatabaseAsset context)
        {
            UnityEngine.Debug.LogError($"Cannot find any data table asset named {name}.", context);
        }
        
        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogErrorCannotFindAsset(Type type, DatabaseAsset context)
        {
            UnityEngine.Debug.LogError($"Cannot find any data table asset of type {type}.", context);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogErrorFoundAssetIsNotValidType<T>(DataTableAsset context)
        {
            UnityEngine.Debug.LogError($"The data table asset is not an instance of {typeof(T)}", context);
        }

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogErrorFoundAssetIsNotValidType(Type type, DataTableAsset context)
        {
            UnityEngine.Debug.LogError($"The data table asset is not an instance of {type}", context);
        }
    }
}
