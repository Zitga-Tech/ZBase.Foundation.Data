using System;
using UnityEngine;

#if USE_UNITY_EDITORCOROUTINES
using Unity.EditorCoroutines.Editor;
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.CsvSheets
{
    partial class DatabaseCsvSheetConfig<TDatabaseAsset, TSheetContainer>
    {
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
                IncludeSubFolders = IncludeSubFolders,
                IncludeCommentedFiles = IncludeCommentedFiles,
                AssetOutputFolderPath = AssetOutputFolderPath,
                ShowProgress = false,
                ResultCallback = resultCallback,
            };

            EditorCoroutineUtility.StartCoroutine(Export(args), this);
        }

        public override void ExportDataTableAssets()
        {
#if USE_CYSHARP_UNITASK && USE_UNITY_EDITORCOROUTINES

            if (CsvFolderPathExist == false)
            {
                Debug.LogError($"CSV folder does not exists");
                return;
            }

            if (OutputFolderExist == false)
            {
                Debug.LogError($"Output folder does not exists");
                return;
            }

            var databaseAssetName = GetDatabaseAssetName();

            if (string.IsNullOrWhiteSpace(databaseAssetName))
            {
                Debug.LogError("The name of Database Asset must not be null or contain only white spaces.");
                return;
            }

            var args = new ExportArgs {
                SheetContainer = CreateSheetContainer(),
                DatabaseAssetName = databaseAssetName,
                CsvFolderPath = FullCsvFolderPath,
                IncludeSubFolders = IncludeSubFolders,
                IncludeCommentedFiles = IncludeCommentedFiles,
                AssetOutputFolderPath = AssetOutputFolderPath,
                ShowProgress = true,
            };

            EditorCoroutineUtility.StartCoroutine(Export(args), this);

#else
            UnityEditor.EditorUtility
                .DisplayDialog("Missing packages", "Requires \"UniTask\" and \"Editor Coroutines\" packages", "OK");
#endif
        }
    }
}
