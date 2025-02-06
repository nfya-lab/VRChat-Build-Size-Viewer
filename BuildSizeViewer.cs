/**
 * VRC Build Size Viewer (Multi-OS & Multi-Language)
 * Created by MunifiSense
 * Updated for multi-OS & multi-language support by nfya
 * https://github.com/MunifiSense/VRChat-Build-Size-Viewer
 * 
 * Licensed under the MIT License.
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildSizeViewer : EditorWindow
{
    public enum Language { English, Japanese }
    private static Language currentLanguage;

    public class BuildObject
    {
        public string size;
        public string percent;
        public string path;
    }

    List<BuildObject> buildObjectList;
    List<string> uncompressedList;
    string buildLogPath;
    private char[] delimiterChars = { ' ', '\t' };
    float win;
    float w1;
    float w2;
    float w3;
    string totalSize;
    bool buildLogFound = false;
    Vector2 scrollPos;

    private static Dictionary<string, string> translations = new Dictionary<string, string>();

    [MenuItem("nfya/VRC Build Size Viewer")]
    public static void ShowWindow()
    {
        GetWindow<BuildSizeViewer>("VRC Build Size Viewer");
    }

    private void OnEnable()
    {
        buildLogPath = getBuildLogPath();
        currentLanguage = (Language)EditorPrefs.GetInt("BuildSizeViewer_Language", 0);
        SetLanguage(currentLanguage);
    }

    private void OnGUI()
    {
        win = position.width * 0.6f;
        w1 = win * 0.15f;
        w2 = win * 0.15f;
        w3 = win * 0.35f;

        // 言語選択ドロップダウン
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Language:", GUILayout.Width(70));
        Language newLanguage = (Language)EditorGUILayout.EnumPopup(currentLanguage);
        EditorGUILayout.EndHorizontal();

        if (newLanguage != currentLanguage)
        {
            currentLanguage = newLanguage;
            EditorPrefs.SetInt("BuildSizeViewer_Language", (int)currentLanguage);
            SetLanguage(currentLanguage);
        }

        EditorGUILayout.LabelField(translations["title"], EditorStyles.boldLabel);
        EditorGUILayout.LabelField(translations["instruction"], EditorStyles.label);

        if (GUILayout.Button(translations["read_log"]))
        {
            buildLogFound = false;
            buildLogFound = getBuildSize();
        }

        if (buildLogFound)
        {
            if (uncompressedList != null && uncompressedList.Count != 0)
            {
                EditorGUILayout.LabelField(translations["total_size"] + ": " + totalSize);
                foreach (string s in uncompressedList)
                {
                    EditorGUILayout.LabelField(s);
                }
            }

            if (buildObjectList != null && buildObjectList.Count != 0)
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(translations["size_percent"], GUILayout.Width(w1));
                EditorGUILayout.LabelField(translations["size"], GUILayout.Width(w2));
                EditorGUILayout.LabelField(translations["path"], GUILayout.Width(w3));
                EditorGUILayout.EndHorizontal();

                foreach (BuildObject buildObject in buildObjectList)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(buildObject.percent, GUILayout.Width(w1));
                    EditorGUILayout.LabelField(buildObject.size, GUILayout.Width(w2));
                    EditorGUILayout.LabelField(buildObject.path);

                    if (buildObject.path != "Resources/unity_builtin_extra")
                    {
                        if (GUILayout.Button(translations["go"], GUILayout.Width(w1)))
                        {
                            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(buildObject.path, typeof(UnityEngine.Object));
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }

    private bool getBuildSize()
    {
        if (!File.Exists(buildLogPath))
        {
            Debug.LogWarning(translations["log_not_found"] + ": " + buildLogPath);
            return false;
        }

        FileUtil.ReplaceFile(buildLogPath, buildLogPath + "copy");
        StreamReader reader = new StreamReader(buildLogPath + "copy");

        if (reader == null)
        {
            Debug.LogWarning(translations["log_read_error"]);
            FileUtil.DeleteFileOrDirectory(buildLogPath + "copy");
            return false;
        }

        string line = reader.ReadLine();
        while (line != null)
        {
            if ((line.Contains("scene-") && line.Contains(".vrcw"))
                || (line.Contains("avtr") && line.Contains(".prefab.unity3d")))
            {
                buildObjectList = new List<BuildObject>();
                uncompressedList = new List<string>();

                line = reader.ReadLine();
                while (!line.Contains("Compressed Size"))
                {
                    line = reader.ReadLine();
                }

                totalSize = line.Split(':')[1];
                line = reader.ReadLine();

                while (line != "Used Assets and files from the Resources folder, sorted by uncompressed size:")
                {
                    uncompressedList.Add(line);
                    line = reader.ReadLine();
                }

                line = reader.ReadLine();
                while (line != "-------------------------------------------------------------------------------")
                {
                    string[] splitLine = line.Split(delimiterChars);
                    BuildObject temp = new BuildObject();
                    temp.size = splitLine[1] + splitLine[2];
                    temp.percent = splitLine[4];
                    temp.path = splitLine[5];

                    for (int i = 6; i < splitLine.Length; i++)
                    {
                        temp.path += (" " + splitLine[i]);
                    }

                    buildObjectList.Add(temp);
                    line = reader.ReadLine();
                }
            }
            line = reader.ReadLine();
        }

        FileUtil.DeleteFileOrDirectory(buildLogPath + "copy");
        reader.Close();
        return true;
    }

    private string getBuildLogPath()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
                return System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + "/Unity/Editor/Editor.log";
            case RuntimePlatform.OSXEditor:
                return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Library/Logs/Unity/Editor.log";
            case RuntimePlatform.LinuxEditor:
                return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/.config/unity3d/Editor.log";
            default:
                Debug.LogError(translations["unsupported_os"]);
                return "";
        }
    }

    private void SetLanguage(Language lang)
    {
        translations.Clear();
        if (lang == Language.English)
        {
            translations["title"] = "VRC Build Size Viewer";
            translations["instruction"] = "Create a build of your world/avatar and click the button!";
            translations["read_log"] = "Read Build Log";
            translations["total_size"] = "Total Compressed Build Size";
            translations["size_percent"] = "Size%";
            translations["size"] = "Size";
            translations["path"] = "Path";
            translations["go"] = "Go";
            translations["log_not_found"] = "Could not find build log file";
            translations["log_read_error"] = "Could not read build file";
            translations["unsupported_os"] = "Unsupported OS for Build Log Viewer.";
        }
        else
        {
            translations["title"] = "VRC ビルドサイズビューア";
            translations["instruction"] = "ワールドまたはアバターをビルドし、ボタンを押してください！";
            translations["read_log"] = "ビルドログを読み取る";
            translations["total_size"] = "圧縮後のビルドサイズ";
            translations["size_percent"] = "サイズ%";
            translations["size"] = "サイズ";
            translations["path"] = "パス";
            translations["go"] = "移動";
            translations["log_not_found"] = "ビルドログファイルが見つかりません";
            translations["log_read_error"] = "ビルドファイルを読み取れません";
            translations["unsupported_os"] = "このOSは対応していません";
        }
    }
}
#endif
