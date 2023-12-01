using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cathei.BakingSheet.Unity;
using UnityEditor;
using UnityEngine;

#if USE_CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif

#if USE_UNITY_EDITORCOROUTINES
using Unity.EditorCoroutines.Editor;
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    public abstract partial class DatabaseGoogleSheetConfig<TSheetContainer> : DatabaseGoogleSheetConfig<DatabaseAsset, TSheetContainer>
        where TSheetContainer : DataSheetContainerBase
    {
    }

    public abstract partial class DatabaseGoogleSheetConfig<TDatabaseAsset, TSheetContainer> : DatabaseGoogleSheetConfigBase
        where TDatabaseAsset : DatabaseAsset
        where TSheetContainer : DataSheetContainerBase
    {
        protected abstract TSheetContainer CreateSheetContainer();

        public override void ExportDataTableAssets(Action<bool> resultCallback)
        {
            if (ServiceAccountFileExist == false)
            {
                Debug.LogError($"Credential file does not exists");
                resultCallback?.Invoke(false);
                return;
            }

            if (SpreadSheetIdFilePathExist == false)
            {
                Debug.LogError($"Spreadsheet ID file does not exists");
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
                CredentialFilePath = ServiceAccountFilePath,
                SpreadSheetIdFilePath = SpreadSheetIdFilePath,
                AssetOutputFolderPath = AssetOutputFolderPath,
                ShowProgress = false,
                ResultCallback = resultCallback,
            };

            EditorCoroutineUtility.StartCoroutine(Export(args), this);
        }

        protected virtual IEnumerator Export(ExportArgs args)
        {
#if USE_CYSHARP_UNITASK && USE_UNITY_EDITORCOROUTINES

            ShowProgress(args, "Retrieving service account credential...");

            var credentialJsonTask = File.ReadAllTextAsync(args.CredentialFilePath).AsUniTask();

            while (credentialJsonTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Retrieving service account credential...");
                yield return null;
            }

            var credentialJson = credentialJsonTask.AsTask().Result;

            if (string.IsNullOrWhiteSpace(credentialJson))
            {
                Debug.LogError($"Service account credential is undefined.");
                StopReport(args);
                args.ResultCallback?.Invoke(false);
                yield break;
            }

            ShowProgress(args, "Retrieving id of the spreadsheet which contains a table of file meta...");

            var spreadSheetIdTask = File.ReadAllTextAsync(args.SpreadSheetIdFilePath).AsUniTask();

            while (spreadSheetIdTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Retrieving id of the spreadsheet which contains a table of file meta...");
                yield return null;
            }

            var spreadSheetId = spreadSheetIdTask.AsTask().Result;

            if (string.IsNullOrWhiteSpace(spreadSheetId))
            {
                Debug.LogError($"Spreadsheet Id is undefined.");
                StopReport(args);
                args.ResultCallback?.Invoke(false);
                yield break;
            }

            ShowProgress(args, "Reading the table of file meta...");

            var fileSheetContainer = new FileDatabaseDefinition.SheetContainer(UnityLogger.Default);
            var fileSheetConverter = new DatabaseGoogleSheetConverter(
                  spreadSheetId
                , credentialJson
                , TimeZoneInfo.Utc
            );

            var fileSheetBakeTask = fileSheetContainer.Bake(fileSheetConverter).AsUniTask();

            while (fileSheetBakeTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Reading the table of file meta...");
                yield return null;
            }

            var converters = new List<DatabaseGoogleSheetConverter>();
            var fileDataTableAsset = fileSheetContainer.FileDataTableAsset_FileDataSheet;

            if (fileDataTableAsset == null || fileDataTableAsset.Count < 1)
            {
                converters.Add(fileSheetConverter);
            }
            else
            {
                foreach (var row in fileDataTableAsset)
                {
                    if (row.Type != "application/vnd.google-apps.spreadsheet"
                        || row.FileName.StartsWith('$')
                        || row.FileName.StartsWith('<')
                        || row.FileName.EndsWith('>')
                    )
                    {
                        continue;
                    }

                    ShowProgress(args, $"Add file `{row.FileName}` with id `{row.FileId}`");

                    converters.Add(new DatabaseGoogleSheetConverter(
                          row.FileId
                        , credentialJson
                        , TimeZoneInfo.Utc
                    ));
                }
            }

            if (converters.Count < 1)
            {
                Debug.LogWarning($"No spreadsheet file found.");
                StopReport(args);
                args.ResultCallback?.Invoke(false);
                yield break;
            }

            ShowProgress(args, "Importing all spreadsheet files...");

            var sheetContainer = args.SheetContainer;
            var sheetBakeTask = sheetContainer.Bake(converters.ToArray()).AsUniTask();

            while (sheetBakeTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Importing all spreadsheet files...");
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

            public string CredentialFilePath { get; set; }

            public string SpreadSheetIdFilePath { get; set; }

            public string AssetOutputFolderPath { get; set; }

            public bool ShowProgress { get; set; }

            public Action<bool> ResultCallback { get; set; }
        }
    }

    public abstract class DatabaseGoogleSheetConfigBase : ScriptableObject
    {
        [SerializeField]
        internal string _relativeServiceAccountFilePath;

        [SerializeField]
        internal string _relativeSpreadSheetIdFilePath;

        [SerializeField]
        internal string _relativeOutputFolderPath;

        public string ServiceAccountFilePath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeServiceAccountFilePath ?? ""));

        public bool ServiceAccountFileExist
            => File.Exists(ServiceAccountFilePath);

        public string SpreadSheetIdFilePath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeSpreadSheetIdFilePath ?? ""));

        public bool SpreadSheetIdFilePathExist
            => File.Exists(SpreadSheetIdFilePath);

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
