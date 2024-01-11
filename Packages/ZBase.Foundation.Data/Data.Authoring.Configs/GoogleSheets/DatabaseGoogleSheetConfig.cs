using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Cathei.BakingSheet.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

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

        protected virtual IEnumerator Export(ExportAllAssetsArgs args)
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

            var spreadSheetIdTask = File.ReadAllTextAsync(args.SpreadsheetIdFilePath).AsUniTask();

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

            var converters = new List<DatabaseGoogleSheetConverter>();

            if (args.ListOfSpreadsheets)
            {
                ShowProgress(args, "Reading the list of Spreadsheets...");

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

                var fileDataTableAsset = fileSheetContainer.FileDataTableAsset_FileDataSheet;

                if (fileDataTableAsset != null && fileDataTableAsset.Count >= 1)
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
            }
            
            if (converters.Count < 1)
            {
                converters.Add(new DatabaseGoogleSheetConverter(
                      spreadSheetId
                    , credentialJson
                    , TimeZoneInfo.Utc
                ));
            }

            ShowProgress(args, "Importing all Spreadsheets...");

            var sheetContainer = args.SheetContainer;
            var sheetBakeTask = sheetContainer.Bake(converters.ToArray()).AsUniTask();

            while (sheetBakeTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Importing all Spreadsheets...");
                yield return null;
            }

            ShowProgress(args, "Converting to data table assets...");

            var exporter = new DatabaseAssetExporter<TDatabaseAsset>(
                  args.OutputFolderPath
                , args.DatabaseAssetName
            );

            var sheetStoreTask = sheetContainer.Store(exporter).AsUniTask();

            while (sheetStoreTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Converting to data table assets...");
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

        protected virtual IEnumerator Export(ExportCsvFilesArgs args)
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

            var spreadSheetIdTask = File.ReadAllTextAsync(args.SpreadsheetIdFilePath).AsUniTask();

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

            var exporters = new List<DatabaseGoogleSheetExporter>();
            var fileSystem = new DatabaseFileSystem();

            if (args.ListOfSpreadsheets)
            {
                ShowProgress(args, "Reading the list of Spreadsheets...");

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

                var fileDataTableAsset = fileSheetContainer.FileDataTableAsset_FileDataSheet;

                if (fileDataTableAsset != null && fileDataTableAsset.Count >= 1)
                {
                    foreach (var row in fileDataTableAsset)
                    {
                        if (row.Type != "application/vnd.google-apps.spreadsheet")
                        {
                            continue;
                        }

                        ShowProgress(args, $"Add file `{row.FileName}` with id `{row.FileId}`");

                        exporters.Add(new DatabaseGoogleSheetExporter(
                              row.FileId
                            , credentialJson
                            , fileSystem
                        ));
                    }
                }
            }

            if (exporters.Count < 1)
            {
                exporters.Add(new DatabaseGoogleSheetExporter(
                      spreadSheetId
                    , credentialJson
                    , fileSystem
                ));
            }

            ShowProgress(args, "Exporting all Spreadsheets...");

            var exportTask = Export(args, exporters).AsUniTask();

            while (exportTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Exporting all Spreadsheets...");
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

        private static async Task Export(
              [NotNull] ExportCsvFilesArgs args
            , [NotNull] IEnumerable<DatabaseGoogleSheetExporter> exporters
        )
        {
            foreach (var exporter in exporters)
            {
                await exporter.Export(
                      args.OutputFolderPath
                    , args.FolderPerSpreadsheet
                    , args.CleanOutputFolder
                );
            }
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

        protected abstract class ExportArgs
        {
            public bool ShowProgress { get; set; }

            public string CredentialFilePath { get; set; }

            public string SpreadsheetIdFilePath { get; set; }

            public bool ListOfSpreadsheets { get; set; }

            public string OutputFolderPath { get; set; }

            public Action<bool> ResultCallback { get; set; }
        }

        protected class ExportAllAssetsArgs : ExportArgs
        {
            public TSheetContainer SheetContainer { get; set; }

            public string DatabaseAssetName { get; set; }
        }

        protected class ExportCsvFilesArgs : ExportArgs
        {
            public bool FolderPerSpreadsheet { get; set; }

            public bool CleanOutputFolder { get; set; }
        }
    }

    public abstract class DatabaseGoogleSheetConfigBase : ScriptableObject
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

        protected abstract string GetDatabaseAssetName();

        public abstract void ExportDataTableAssets(Action<bool> resultCallback);

        public abstract void ExportCsvFiles(Action<bool> resultCallback);

        public abstract void ExportDataTableAssets();

        public abstract void ExportCsvFiles();

        public abstract void LocateDatabaseAsset();
    }
}
