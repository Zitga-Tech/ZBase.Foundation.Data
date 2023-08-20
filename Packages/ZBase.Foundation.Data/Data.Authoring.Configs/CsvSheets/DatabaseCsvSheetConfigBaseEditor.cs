using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZBase.Foundation.Data.Authoring.Configs.CsvSheets
{
    [CustomEditor(typeof(DatabaseCsvSheetConfigBase), true)]
    internal class DatabaseCsvSheetConfigBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _relativeCsvFolderPath;
        private SerializedProperty _relativeOutputFolderPath;

        private GUIContent _labelCsvFolderPath;
        private GUIContent _labelOutputFolderPath;

        private void OnEnable()
        {
            var so = this.serializedObject;
            
            _relativeCsvFolderPath = so.FindProperty(nameof(DatabaseCsvSheetConfigBase._relativeCsvFolderPath));
            _relativeOutputFolderPath = so.FindProperty(nameof(DatabaseCsvSheetConfigBase._relativeOutputFolderPath));

            _labelCsvFolderPath = new GUIContent(
                  "Csv Folder Path"
                , "Path to the folder contains CSV files to export. The folder path is relative to the Assets folder."
            );

            _labelOutputFolderPath = new GUIContent(
                  "Relative Output Folder"
                , "Path to the folder contains the exported assets. The folder path is relative to the Assets folder."
            );
        }

        public override void OnInspectorGUI()
        {
            if (this.target is not DatabaseCsvSheetConfigBase config)
            {
                OnInspectorGUI();
                return;
            }

            EditorGUILayout.LabelField("CSV Sheets", EditorStyles.boldLabel);
            {
                EditorGUILayout.Space();
                {
                    EditorGUILayout.BeginVertical();

                    if (config.CsvFolderPathExist)
                    {
                        EditorGUILayout.HelpBox(config.FullCsvFolderPath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Folder must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawRelativePath(config, _relativeCsvFolderPath, _labelCsvFolderPath);

                    var openFilePanel = false;

                    if (GUILayout.Button("Browse", GUILayout.Width(65)))
                    {
                        openFilePanel = true;
                        BrowseFolder("Choose the CSV folder", _relativeCsvFolderPath, config);
                    }

                    if (openFilePanel == false)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);
            {
                EditorGUILayout.Space();
                {
                    EditorGUILayout.BeginVertical();

                    if (config.OutputFolderExist)
                    {
                        EditorGUILayout.HelpBox(config.FullOutputFolderPath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Folder must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawRelativePath(config, _relativeOutputFolderPath, _labelOutputFolderPath);

                    var openFolderPanel = false;

                    if (GUILayout.Button("Browse", GUILayout.Width(65)))
                    {
                        openFolderPanel = true;
                        BrowseFolder("Choose a folder", _relativeOutputFolderPath, config);
                    }

                    if (openFolderPanel == false)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUILayout.Space();
            {
                var color = GUI.color;
                GUI.color = Color.green;

                if (GUILayout.Button("Export All Assets", GUILayout.Height(30)))
                {
                    config.ExportAllAssets();
                }

                GUI.color = color;
            }

            EditorGUILayout.Space();
            {
                if (GUILayout.Button("Locate Database Asset", GUILayout.Height(30)))
                {
                    config.LocateDatabaseAsset();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void BrowseFolder(string title, SerializedProperty property, Object config)
        {
            var path = EditorUtility.OpenFolderPanel(title, "Assets", "Assets");

            if (string.IsNullOrWhiteSpace(path) == false)
            {
                path = Path.GetRelativePath(Application.dataPath, path).Replace('\\', '/');

                property.stringValue = path;
                Undo.RecordObject(config, property.propertyPath);
            }
        }

        private void DrawRelativePath(Object obj, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, label);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(obj, property.propertyPath);
            }
        }
    }
}
