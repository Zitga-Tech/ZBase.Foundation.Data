using System;
using UnityEngine;

#if USE_UNITY_EDITORCOROUTINES
using Unity.EditorCoroutines.Editor;
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    partial class DatabaseGoogleSheetConfig<TDatabaseAsset, TSheetContainer>
    {
        public override void ExportDataTableAssets(Action<bool> resultCallback)
        {
            if (ServiceAccountFileExist == false)
            {
                Debug.LogError($"Credential file does not exists");
                resultCallback?.Invoke(false);
                return;
            }

            if (SpreadsheetIdFilePathExist == false)
            {
                Debug.LogError($"Spreadsheet ID file does not exists");
                resultCallback?.Invoke(false);
                return;
            }

            if (AssetOutputFolderExist == false)
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

#if USE_UNITY_EDITORCOROUTINES
            var args = new ExportAllAssetsArgs {
                SheetContainer = CreateSheetContainer(),
                DatabaseAssetName = databaseAssetName,
                CredentialFilePath = ServiceAccountFilePath,
                SpreadsheetIdFilePath = SpreadsheetIdFilePath,
                ListOfSpreadsheets = ListOfSpreadsheets,
                OutputFolderPath = AssetOutputFolderPath,
                ShowProgress = false,
                ResultCallback = resultCallback,
            };

            EditorCoroutineUtility.StartCoroutine(Export(args), this);
#else
            Debug.LogError("Requires \"Editor Coroutines\" package");
            resultCallback?.Invoke(false);
#endif
        }

        public override void ExportDataTableAssets()
        {
#if USE_CYSHARP_UNITASK && USE_UNITY_EDITORCOROUTINES

            if (ServiceAccountFileExist == false)
            {
                Debug.LogError($"Credential file does not exists");
                return;
            }

            if (SpreadsheetIdFilePathExist == false)
            {
                Debug.LogError($"Spreadsheet ID file does not exists");
                return;
            }

            if (AssetOutputFolderExist == false)
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

            var args = new ExportAllAssetsArgs {
                SheetContainer = CreateSheetContainer(),
                DatabaseAssetName = databaseAssetName,
                CredentialFilePath = ServiceAccountFilePath,
                SpreadsheetIdFilePath = SpreadsheetIdFilePath,
                ListOfSpreadsheets = ListOfSpreadsheets,
                OutputFolderPath = AssetOutputFolderPath,
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
