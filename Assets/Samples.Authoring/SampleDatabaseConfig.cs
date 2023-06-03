using System;
using System.IO;
using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using TriInspector;
using UnityEditor;
using UnityEngine;
using ZBase.Foundation.Data.Authoring;

namespace Samples.Authoring
{
    [CreateAssetMenu(fileName = "SampleDatabaseConfig", menuName = "Sample Database Config", order = 0)]
    [Database]
    public partial class SampleDatabaseConfig : ScriptableObject
    {
        [Title("Google Credentials")]
        [LabelText("Service Account File Path")]
        [Tooltip("Path to the Google Service Account credential JSON file. The file path is relative to the Assets folder.")]
        [InfoBox("$" + nameof(CredentialFilePath))]
        [InfoBox("File does not exist.", TriMessageType.Error, visibleIf: nameof(CredentialFileNotExist))]
        [SerializeField]
        private string _relativeCredentialFilePath;

        [PropertySpace]
        [LabelText("Spreadsheet Id File Path")]
        [Tooltip("Path to the file contains the Spreadsheet ID to export. The file path is relative to the Assets folder.")]
        [InfoBox("$" + nameof(SpreadSheetIdFilePath))]
        [InfoBox("ID must not be empty.", TriMessageType.Error, visibleIf: nameof(SpreadSheetIdFilePathNotExist))]
        [SerializeField]
        private string _spreadSheetIdFilePath;

        [Title("Data Table Assets")]
        [LabelText("Relative Output Folder")]
        [Tooltip("Path to the folder contains the exported Data Table Assets. The folder path is relative to the Assets folder.")]
        [InfoBox("$" + nameof(FullOutputFolderPath))]
        [InfoBox("File does not exist.", TriMessageType.Error, visibleIf: nameof(OutputFolderNotExist))]
        [SerializeField]
        private string _relativeOutputFolderPath;

        private string CredentialFilePath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeCredentialFilePath));

        private bool CredentialFileNotExist
            => File.Exists(CredentialFilePath) == false;

        private string FullOutputFolderPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _relativeOutputFolderPath));

        private string AssetOutputFolderPath
            => Path.Combine("Assets", _relativeOutputFolderPath);

        private bool OutputFolderNotExist
            => Directory.Exists(FullOutputFolderPath) == false;

        private string SpreadSheetIdFilePath
            => Path.GetFullPath(Path.Combine(Application.dataPath, _spreadSheetIdFilePath));

        private bool SpreadSheetIdFilePathNotExist
            => File.Exists(SpreadSheetIdFilePath) == false;

        [PropertySpace]
        [Button]
        private void ExportDataTableAssets()
        {
            if (CredentialFileNotExist)
            {
                Debug.LogError($"Credential file does not exists");
                return;
            }

            if (SpreadSheetIdFilePathNotExist)
            {
                Debug.LogError($"Spreadsheet ID file does not exists");
                return;
            }

            if (OutputFolderNotExist)
            {
                Debug.LogError($"Output folder does not exists");
                return;
            }

            Export().Forget();
        }

        private async UniTaskVoid Export()
        {
            var credentialJson = await File.ReadAllTextAsync(CredentialFilePath);

            if (string.IsNullOrWhiteSpace(credentialJson))
            {
                Debug.LogError($"Credential file is empty");
                return;
            }

            var spreadSheetId = await File.ReadAllTextAsync(SpreadSheetIdFilePath);

            if (string.IsNullOrWhiteSpace(spreadSheetId))
            {
                Debug.LogError($"Spreadsheet ID is empty");
                return;
            }

            var googleSheetConverter = new DatabaseGoogleSheetConverter(
                  spreadSheetId
                , credentialJson
                , TimeZoneInfo.Utc
            );

            var sheetContainer = new SheetContainer(UnityLogger.Default);

            await sheetContainer.Bake(googleSheetConverter);

            var exporter = new DatabaseAssetExporter(
                  AssetOutputFolderPath
                , "SampleDatabaseAsset"
            );

            await sheetContainer.Store(exporter);

            AssetDatabase.Refresh();
        }
    }

    [Table(typeof(HeroDataTableAsset), "Heroes", NamingStrategy.SnakeCase)]
    [VerticalList(typeof(HeroData), nameof(HeroData.Multipliers))]
    partial class SampleDatabaseConfig
    {
        partial class HeroDataSheet { }
    }

    [Table(typeof(EnemyDataTableAsset), "Enemies", NamingStrategy.SnakeCase)]
    partial class SampleDatabaseConfig
    {
        partial class EnemyDataSheet { }
    }
}
