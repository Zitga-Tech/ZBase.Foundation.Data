using UnityEditor;
using UnityEngine;

namespace ZBase.Foundation.Data.Authoring.GoogleSheets
{
    partial class GoogleSheetConfig<TDatabaseAsset, TSheetContainer>
    {
        public override void LocateDatabaseAsset()
        {
            if (DatabaseFileExist == false)
            {
                Debug.LogError($"Database file does not exists");
                return;
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath(
                  DatabaseFilePath
                , typeof(ScriptableObject)
            );
        }
    }
}
