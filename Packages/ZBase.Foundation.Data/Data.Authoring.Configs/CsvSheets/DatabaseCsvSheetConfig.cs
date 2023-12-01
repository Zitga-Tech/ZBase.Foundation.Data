using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

#if USE_CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif

#if USE_UNITY_EDITORCOROUTINES
using Unity.EditorCoroutines.Editor;
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.CsvSheets
{
    public abstract partial class DatabaseCsvSheetConfig<TSheetContainer> : DatabaseCsvSheetConfig<DatabaseAsset, TSheetContainer>
        where TSheetContainer : DataSheetContainerBase
    {
    }

    public abstract partial class DatabaseCsvSheetConfig<TDatabaseAsset, TSheetContainer> : DatabaseCsvSheetConfigBase
        where TDatabaseAsset : DatabaseAsset
        where TSheetContainer : DataSheetContainerBase
    {
        protected abstract TSheetContainer CreateSheetContainer();

        public override void ExportDataTableAssets(Action<bool> resultCallback)
        {
            if (CsvFolderPathExist == false)
            {
                Debug.LogError($"CSV folder does not exists");
                resultCallback?.Invoke(false);
                return;
            }

            if (OutputFolderExist == false)
            {
                Debug.LogError($"Output folder does not exists");
                resultCallback?.Invoke(false);
                return;
            }

            var databaseAssetName = GetDatabaseAssetName();

            if (string.IsNullOrWhiteSpace(databaseAssetName))
            {
                Debug.LogError($"The name of Master Database Asset must not be empty or contain only white spaces.");
                resultCallback?.Invoke(false);
                return;
            }

            var args = new ExportArgs {
                SheetContainer = CreateSheetContainer(),
                DatabaseAssetName = databaseAssetName,
                CsvFolderPath = FullCsvFolderPath,
                AssetOutputFolderPath = AssetOutputFolderPath,
                ShowProgress = false,
                ResultCallback = resultCallback,
            };

            EditorCoroutineUtility.StartCoroutine(Export(args), this);
        }

        protected virtual IEnumerator Export(ExportArgs args)
        {
#if USE_CYSHARP_UNITASK && USE_UNITY_EDITORCOROUTINES

            var converter = new DatabaseCsvSheetConverter(
                  args.CsvFolderPath
                , TimeZoneInfo.Utc
            );

            ShowProgress(args, "Importing all CSV files...");

            var sheetContainer = args.SheetContainer;
            var sheetBakeTask = sheetContainer.Bake(converter).AsUniTask();

            while (sheetBakeTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Importing all CSV files...");
                yield return null;
            }

            ShowProgress(args, "Converting to data table asset files...");

            var exporter = new DatabaseAssetExporter<TDatabaseAsset>(
                  args.AssetOutputFolderPath
                , args.DatabaseAssetName
            );

            var sheetStoreTask = sheetContainer.Store(exporter).AsUniTask();

            while (sheetStoreTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Converting to data table asset files...");
                yield return null;
            }

            StopReport(args);
            AssetDatabase.Refresh();

            args.ResultCallback?.Invoke(true);
#else
            UnityEditor.EditorUtility
                .DisplayDialog("Missing packages", "Requires \"UniTask\" and \"Editor Coroutines\" packages", "OK");
            yield break;
#endif
        }

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

        protected class ExportArgs
        {
            public TSheetContainer SheetContainer { get; set; }

            public string DatabaseAssetName { get; set; }

            public string CsvFolderPath { get; set; }

            public string AssetOutputFolderPath { get; set; }

            public bool ShowProgress { get; set; }

            public Action<bool> ResultCallback { get; set; }
        }
    }

    public abstract class DatabaseCsvSheetConfigBase : ScriptableObject
    {
        [SerializeField]
        internal string _relativeCsvFolderPath;

        [SerializeField]
        internal string _relativeOutputFolderPath;

        public string FullCsvFolderPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeCsvFolderPath ?? ""));

        public bool CsvFolderPathExist
            => Directory.Exists(FullCsvFolderPath);

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

        public abstract void ExportAllAssets();

        public abstract void LocateDatabaseAsset();
    }
}
