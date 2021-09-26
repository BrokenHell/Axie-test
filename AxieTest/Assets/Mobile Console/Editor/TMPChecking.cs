using UnityEditor;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
#endif

namespace MobileConsole.Editor
{
	public static class TMPChecking
	{
		public enum InstallSource
		{
			None,
			AssetStore,
			PackageManager
		}

		public delegate void Result(InstallSource source);
		static Result OnCheckingResult;

		const string TMP_PackageName = "com.unity.textmeshpro";

#if UNITY_2018_1_OR_NEWER
		static ListRequest _packageRequest;
#endif

		public static void CheckTMPAsync(Result result)
		{
			OnCheckingResult = result;

			if (HasTMPInAsset())
			{
				OnCheckingResult(InstallSource.AssetStore);
			}
			else
			{
#if UNITY_2018_1_OR_NEWER
				CheckTMPInPackageManagerAsync();
#else
				OnCheckingResult(InstallSource.None);
#endif
			}

		}


#if UNITY_2018_1_OR_NEWER
		public static void CheckTMPInPackageManagerAsync()
		{
			_packageRequest = Client.List();
			EditorApplication.update += OnFetchingListPackages;
		}


		static void OnFetchingListPackages()
		{
			if (_packageRequest != null)
			{
				if (_packageRequest.IsCompleted)
				{
					bool isFound = false;
					foreach (var result in _packageRequest.Result)
					{
						if (result.name == TMP_PackageName)
						{
							isFound = true;
							break;
						}
					}

					EditorApplication.update -= OnFetchingListPackages;

					OnCheckingResult(isFound ? InstallSource.PackageManager : InstallSource.None);
				}
			}
		}
#endif

		static bool HasTMPInAsset()
		{
			string[] sceneAssetGUIDs = AssetDatabase.FindAssets("TextMeshPro-5.5-Runtime");
			return sceneAssetGUIDs.Length > 0;
		}
	}
}