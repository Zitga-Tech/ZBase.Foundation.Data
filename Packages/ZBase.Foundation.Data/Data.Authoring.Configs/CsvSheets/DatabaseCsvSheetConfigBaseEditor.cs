using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZBase.Foundation.Data.Authoring.Configs.CsvSheets
{
    [CustomEditor(typeof(DatabaseCsvSheetConfigBase), true)]
    internal class DatabaseCsvSheetConfigBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _relativeCsvFolderPath;
        private SerializedProperty _includeSubFolders;
        private SerializedProperty _includeCommentedFiles;
        private SerializedProperty _relativeOutputFolderPath;

        private GUIContent _labelCsvFolderPath;
        private GUIContent _labelIncludeSubFolders;
        private GUIContent _labelIncludeCommentedFiles;
        private GUIContent _labelOutputFolderPath;

        private void OnEnable()
        {
            var so = this.serializedObject;
            
            _relativeCsvFolderPath = so.FindProperty(nameof(DatabaseCsvSheetConfigBase._relativeCsvFolderPath));
            _includeSubFolders = so.FindProperty(nameof(DatabaseCsvSheetConfigBase._includeSubFolders));
            _includeCommentedFiles = so.FindProperty(nameof(DatabaseCsvSheetConfigBase._includeCommentedFiles));
            _relativeOutputFolderPath = so.FindProperty(nameof(DatabaseCsvSheetConfigBase._relativeOutputFolderPath));

            _labelCsvFolderPath = new GUIContent(
                  "Csv Folder Path"
                , "Path to the folder contains CSV files to export. The folder path is relative to the Assets folder."
            );

            _labelIncludeSubFolders = new GUIContent(
                  "Include Sub-Folders"
                , "Include all CSV files in all sub-folders."
            );

            _labelIncludeCommentedFiles = new GUIContent(
                  "Include Commented Files"
                , "Include all CSV files whose name starts with a $."
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
                EditorGUILayout.BeginVertical();
                {
                    if (config.CsvFolderPathExist)
                    {
                        EditorGUILayout.HelpBox(config.FullCsvFolderPath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Folder must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawProperty(config, _relativeCsvFolderPath, _labelCsvFolderPath);

                    var openFilePanel = false;

                    if (GUILayout.Button("Browse", GUILayout.Width(65)))
                    {
                        openFilePanel = true;
                        BrowseFolder("Choose the CSV folder", _relativeCsvFolderPath, config);
                    }

                    if (openFilePanel == false)
                    {
                        EditorGUILayout.EndHorizontal();

                        DrawProperty(config, _includeSubFolders, _labelIncludeSubFolders);


                        DrawProperty(config, _includeCommentedFiles, _labelIncludeCommentedFiles);

                        if (config.IncludeCommentedFiles)
                        {
                            EditorGUILayout.HelpBox(
                                "Be careful when enable 'Include Commeted Files' option!\n" +
                                "Files whose name starts with a $ will be included in the exporting process. " +
                                "For examples: '$hero.csv', '$enemy.csv'."
                                , MessageType.Warning
                            );
                        }

                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                {
                    if (config.OutputFolderExist)
                    {
                        EditorGUILayout.HelpBox(config.FullOutputFolderPath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Folder must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawProperty(config, _relativeOutputFolderPath, _labelOutputFolderPath);

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

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    var color = GUI.color;
                    GUI.color = Color.green;

                    var enabled = GUI.enabled;
                    GUI.enabled = enabled
                        && config.CsvFolderPathExist
                        && config.OutputFolderExist
                        ;

                    if (GUILayout.Button("Export All Assets", GUILayout.Height(25)))
                    {
                        config.ExportDataTableAssets();
                    }

                    GUI.enabled = enabled;
                    GUI.color = color;
                }

                {
                    var enabled = GUI.enabled;
                    GUI.enabled = enabled && config.DatabaseFileExist;

                    if (GUILayout.Button("Locate Database Asset", GUILayout.Height(25)))
                    {
                        config.LocateDatabaseAsset();
                    }

                    GUI.enabled = enabled;
                }
                EditorGUILayout.EndHorizontal();
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

        private void DrawProperty(Object obj, SerializedProperty property, GUIContent label)
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
