using UnityEditor;
using UnityEngine;

namespace ZBase.Foundation.Data.Editor
{
    [UnityEditor.CustomEditor(typeof(DatabaseAsset))]
    public sealed class DatabaseAssetEditor : UnityEditor.Editor
    {
        private Vector2 _assetRefsScrollPos;
        private Vector2 _redundantAssetRefsScrollPos;

        public override void OnInspectorGUI()
        {
            if (this.target is not DatabaseAsset databaseAsset)
            {
                base.OnInspectorGUI();
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Table Assets", EditorStyles.boldLabel);

            DrawAssets(
                  databaseAsset._assetRefs
                , nameof(DatabaseAsset._assetRefs)
                , ref _assetRefsScrollPos
            );

            if (databaseAsset._redundantAssetRefs.Length < 1)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Redundant Table Assets", EditorStyles.boldLabel);

            DrawAssets(
                  databaseAsset._redundantAssetRefs
                , nameof(DatabaseAsset._redundantAssetRefs)
                , ref _redundantAssetRefsScrollPos
            );
        }

        private void DrawAssets(
              DatabaseAsset.TableAssetRef[] assetRefs
            , string serializedPropertyName
            , ref Vector2 scrollPos
        )
        {
            if (assetRefs == null || assetRefs.Length < 1)
            {
                return;
            }

            var assetsSP = this.serializedObject.FindProperty(serializedPropertyName);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            for (var i = 0; i < assetRefs.Length; i++)
            {
                var asset = assetRefs[i];
                var referenceSP = assetsSP.GetArrayElementAtIndex(i)
                    .FindPropertyRelative(nameof(DatabaseAsset.TableAssetRef.reference));

                EditorGUILayout.BeginHorizontal(GUI.skin.box);

                EditorGUILayout.PrefixLabel(asset.name);
                EditorGUILayout.PropertyField(referenceSP, GUIContent.none);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
