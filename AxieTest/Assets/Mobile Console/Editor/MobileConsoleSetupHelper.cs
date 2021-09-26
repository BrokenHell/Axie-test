using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MobileConsole.Editor
{
	public static class MobileconsoleSetupHelper
	{
		const string ToolVersion = "2.0.4";
		const string DebugLogDefineSymbol = "DebugLog";
        static MobileConsoleInfo mobileConsoleInfo;

		[DidReloadScripts]
		static void CheckingNewVersion()
		{
            mobileConsoleInfo = AssetHelper.LoadUniqueAsset<MobileConsoleInfo>();
            if (mobileConsoleInfo == null || mobileConsoleInfo.version != ToolVersion)
            {
                // First install or update to new version, check for TMP install source
                TMPChecking.CheckTMPAsync(OnCheckingTMPDone);
            }
		}
		
		public static void OnCheckingTMPDone(TMPChecking.InstallSource installSource)
		{
			string scenePath = LogConsoleScenePath();
			string assetDirectory = Path.GetDirectoryName(scenePath);

			if (installSource == TMPChecking.InstallSource.PackageManager)
			{
				Debug.Log("Found Package Manager version of <b>TextMeshPro</b> in project");
				Debug.Log("Start update new GUIDs for LogConsole scene and prefab");
				GUIDMappingSet guidMappingSet = AssetHelper.LoadUniqueAsset<GUIDMappingSet>("TMPGUIDMapping");
				if (guidMappingSet == null)
				{
					throw new System.Exception("Could not found TMPGUIDMapping.asset");
				}

				GUIDMappingHelper.Map(guidMappingSet, assetDirectory);
				Debug.Log("Update new GUIDs completed!");
			}
			else if (installSource == TMPChecking.InstallSource.None)
			{
				Debug.LogError("No <b>TextMeshPro</b> found in project. Please import <b>TextMeshPro</b> either from Asset Store or Package Manager. If you're using Text Mesh Pro Source version, MCK doesn't support that, please contact me via pixeption@gmail so I can support you.");
			}

			if (mobileConsoleInfo == null)
			{
                mobileConsoleInfo = ScriptableObject.CreateInstance<MobileConsoleInfo>();
                mobileConsoleInfo.version = ToolVersion;
                AssetDatabase.CreateAsset(mobileConsoleInfo, Path.Combine(assetDirectory, "MobileConsoleInfo.asset"));
				EditorApplication.delayCall += SaveAssets;
			}
			else
			{
                mobileConsoleInfo.version = ToolVersion;
				EditorUtility.SetDirty(mobileConsoleInfo);
				EditorApplication.delayCall += SaveAssets;
			}
		}

		static void SaveAssets()
		{
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

        [MenuItem("Tools/Mobile Console/Enable", false, 1)]
		public static void EnableMobileConsole()
		{
			AddDebugLogDefineSymbolForGroup(BuildTargetGroup.iOS);
			AddDebugLogDefineSymbolForGroup(BuildTargetGroup.Android);
			AddDebugLogDefineSymbolForGroup(BuildTargetGroup.Standalone);
			AddLogConsoleToBuildSettings();
			EnableDevelopmentBuild();
			Debug.Log("Enable Mobile Console Completed");
		}

		[MenuItem("Tools/Mobile Console/Disable", false, 2)]
		public static void DisableMobileConsole()
		{
			RemoveDebugLogDefineSymbolForGroup(BuildTargetGroup.iOS);
			RemoveDebugLogDefineSymbolForGroup(BuildTargetGroup.Android);
			RemoveDebugLogDefineSymbolForGroup(BuildTargetGroup.Standalone);
			RemoveLogConsoleFromBuildSettings();
			Debug.Log("Disable Mobile Console Completed");
		}

		[MenuItem("Tools/Mobile Console/Open Log Folder", false, 21)]
		public static void OpenLogFolder()
		{
			string folderPath = Path.Combine(Application.persistentDataPath, "MCK_Logs");

			// Make sure the folder is created, just in case there is no log
			Directory.CreateDirectory(folderPath);

			// Open the folder
			EditorUtility.RevealInFinder(folderPath);
		}

		[MenuItem("Tools/Mobile Console/Online Manual", false, 41)]
		public static void OpenOnlineManual()
		{
			Application.OpenURL("https://github.com/pixeption/MobileConsoleKit");
		}

		[MenuItem("Tools/Mobile Console/About", false, 42)]
		public static void About()
		{
			string title = string.Format("Mobile Console Kit ({0})", ToolVersion);
			string message = "Bugs, feedbacks, feature requests ... Please contact me via pixeption@gmail.com";
			EditorUtility.DisplayDialog(title, message, "Close");
		}

		static void AddDebugLogDefineSymbolForGroup(BuildTargetGroup group)
		{
			string defineSymbolsString = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
			List<string> defineSymbols = defineSymbolsString.Split(';').ToList();
			if (!defineSymbols.Contains(DebugLogDefineSymbol))
			{
				defineSymbols.Add(DebugLogDefineSymbol);
				string newDefineSymbolsString = string.Join(";", defineSymbols.ToArray());
				PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newDefineSymbolsString);
			}
		}

		static void RemoveDebugLogDefineSymbolForGroup(BuildTargetGroup group)
		{
			string defineSymbolsString = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
			List<string> defineSymbols = defineSymbolsString.Split(';').ToList();
			if (defineSymbols.Contains(DebugLogDefineSymbol))
			{
				defineSymbols.Remove(DebugLogDefineSymbol);
				string newDefineSymbolsString = string.Join(";", defineSymbols.ToArray());
				PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newDefineSymbolsString);
			}
		}

		static void AddLogConsoleToBuildSettings()
		{
			string logConsoleScenePath = LogConsoleScenePath();
			if (string.IsNullOrEmpty(logConsoleScenePath))
			{
				throw new System.Exception("Could not found LogConsole scene");
			}

			List<EditorBuildSettingsScene> buildSettingScenes = EditorBuildSettings.scenes.ToList();
			EditorBuildSettingsScene logConsoleScene = buildSettingScenes.Find(scene => scene.path == logConsoleScenePath);

			// Always remove if scene is already added just in case it's disabled
			if (logConsoleScene != null)
			{
				buildSettingScenes.Remove(logConsoleScene);
			}

			buildSettingScenes.Add(new EditorBuildSettingsScene(logConsoleScenePath, true));
			EditorBuildSettings.scenes = buildSettingScenes.ToArray();
		}

		static void EnableDevelopmentBuild()
		{
			EditorUserBuildSettings.development = true;
		}

		static void RemoveLogConsoleFromBuildSettings()
		{
			string logConsoleScenePath = LogConsoleScenePath();
			if (string.IsNullOrEmpty(logConsoleScenePath))
			{
				throw new System.Exception("Could not found LogConsole scene");
			}

			List<EditorBuildSettingsScene> buildSettingScenes = EditorBuildSettings.scenes.ToList();
			EditorBuildSettingsScene logConsoleScene = buildSettingScenes.Find(scene => scene.path == logConsoleScenePath);
			if (logConsoleScene != null)
			{
				buildSettingScenes.Remove(logConsoleScene);
				EditorBuildSettings.scenes = buildSettingScenes.ToArray();
			}
		}

		public static string LogConsoleScenePath()
		{
			string[] sceneAssetGUIDs = AssetDatabase.FindAssets("t:SceneAsset");
			foreach (var guid in sceneAssetGUIDs)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.EndsWith("LogConsole.unity"))
				{
					return path;
				}
			}

			return null;
		}
	}
}