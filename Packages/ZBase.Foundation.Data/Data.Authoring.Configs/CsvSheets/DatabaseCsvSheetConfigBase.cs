﻿using System;
using System.IO;
using UnityEngine;

#if USE_CYSHARP_UNITASK
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.CsvSheets
{
    public abstract class DatabaseCsvSheetConfigBase : ScriptableObject
    {
        [SerializeField]
        internal string _relativeCsvFolderPath;

        [SerializeField]
        internal bool _includeSubFolders = true;

        [SerializeField]
        internal string _relativeOutputFolderPath;

        public string FullCsvFolderPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeCsvFolderPath ?? ""));

        public bool CsvFolderPathExist
            => Directory.Exists(FullCsvFolderPath);

        public bool IncludeSubFolders
            => _includeSubFolders;

        public string FullOutputFolderPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeOutputFolderPath ?? ""));

        public string AssetOutputFolderPath
            => Path.Combine("Assets", _relativeOutputFolderPath ?? "");

        public bool OutputFolderExist
            => Directory.Exists(FullOutputFolderPath);

        public string DatabaseFileName
            => $"{GetDatabaseAssetName()}.asset";

        public string FullDatabaseFilePath
            => Path.Combine(FullOutputFolderPath, DatabaseFileName);

        public bool DatabaseFileExist
            => string.IsNullOrWhiteSpace(FullDatabaseFilePath) == false
            && File.Exists(FullDatabaseFilePath);

        public string DatabaseFilePath
            => Path.Combine(AssetOutputFolderPath, DatabaseFileName);

        protected abstract string GetDatabaseAssetName();

        public abstract void ExportDataTableAssets(Action<bool> resultCallback);

        public abstract void ExportDataTableAssets();

        public abstract void LocateDatabaseAsset();
    }
}
