using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    public abstract partial class DatabaseGoogleSheetConfigBase : ScriptableObject
    {
        [SerializeField]
        internal string _relativeServiceAccountFilePath;

        [SerializeField, FormerlySerializedAs("_relativeSpreadSheetIdFilePath")]
        internal string _relativeSpreadsheetIdFilePath;

        [SerializeField]
        internal bool _listOfSpreadsheets;

        [SerializeField, FormerlySerializedAs("_relativeOutputFolderPath")]
        internal string _relativeAssetOutputFolderPath;

        [SerializeField]
        internal string _relativeCsvOutputFolderPath;

        [SerializeField]
        internal bool _csvFolderPerSpreadsheet = true;

        [SerializeField]
        internal bool _cleanCsvOutputFolder = true;

        [SerializeField]
        internal bool _cleanCsvOutputSubFolders = true;

        [SerializeField]
        internal bool _commentOutFileNameIfPossible = true;

        public string ServiceAccountFilePath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeServiceAccountFilePath ?? ""));

        public bool ServiceAccountFileExist
            => File.Exists(ServiceAccountFilePath);

        public string SpreadsheetIdFilePath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeSpreadsheetIdFilePath ?? ""));

        public bool SpreadsheetIdFilePathExist
            => File.Exists(SpreadsheetIdFilePath);

        public bool ListOfSpreadsheets
            => _listOfSpreadsheets;

        public string FullAssetOutputFolderPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeAssetOutputFolderPath ?? ""));

        public string AssetOutputFolderPath
            => Path.Combine("Assets", _relativeAssetOutputFolderPath ?? "");

        public bool AssetOutputFolderExist
            => Directory.Exists(FullAssetOutputFolderPath);

        public string DatabaseFileName
            => $"{GetDatabaseAssetName()}.asset";

        public string FullDatabaseFilePath
            => Path.Combine(FullAssetOutputFolderPath, DatabaseFileName);

        public bool DatabaseFileExist
            => string.IsNullOrWhiteSpace(FullDatabaseFilePath) == false
            && File.Exists(FullDatabaseFilePath);

        public string DatabaseFilePath
            => Path.Combine(AssetOutputFolderPath, DatabaseFileName);

        public string FullCsvOutputFolderPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeCsvOutputFolderPath ?? ""));

        public string CsvOutputFolderPath
            => Path.Combine("Assets", _relativeCsvOutputFolderPath ?? "");

        public bool CsvOutputFolderExist
            => Directory.Exists(FullCsvOutputFolderPath);

        public bool CsvFolderPerSpreadsheet
            => _csvFolderPerSpreadsheet;

        public bool CleanCsvOutputFolder
            => _cleanCsvOutputFolder;

        public bool CleanCsvOutputSubFolders
            => _cleanCsvOutputSubFolders;

        public bool CommentOutFileNameIfPossible
            => _commentOutFileNameIfPossible;

        protected abstract string GetDatabaseAssetName();

        public abstract void ExportDataTableAssets(Action<bool> resultCallback);

        public abstract void ExportDataTableAssets();

        public abstract void LocateDatabaseAsset();

        protected static void ShowProgress(ExportArgs args, string message)
        {
            if (args.ShowProgress)
            {
                EditorUtility.DisplayProgressBar("Master Database Exporter", message, 0);
            }
        }

        protected static void StopReport(ExportArgs args)
        {
            if (args.ShowProgress)
            {
                EditorUtility.ClearProgressBar();
            }
        }

        protected abstract class ExportArgs
        {
            public bool ShowProgress { get; set; }

            public string CredentialFilePath { get; set; }

            public string SpreadsheetIdFilePath { get; set; }

            public bool ListOfSpreadsheets { get; set; }

            public string OutputFolderPath { get; set; }

            public Action<bool> ResultCallback { get; set; }
        }
    }
}
