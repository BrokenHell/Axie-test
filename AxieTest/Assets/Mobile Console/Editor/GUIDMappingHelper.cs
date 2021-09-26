using System.IO;
using System.Linq;

namespace MobileConsole.Editor
{
	public static class GUIDMappingHelper
	{
		static string[] extensionWhitelist = new string[] { ".unity", ".asset", ".prefab" };

		public static void Map(GUIDMappingSet guidMappingSet, string rootPath, bool isLog = false)
		{
			if (isLog)
				UnityEngine.Debug.LogFormat("Start mapping GUID: {0}", rootPath);
			string[] filePaths = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
			foreach (var path in filePaths)
			{
				if (!extensionWhitelist.Contains(Path.GetExtension(path)))
					continue;

				string fileContent = File.ReadAllText(path);
				bool hasChanged = false;
				foreach (var guidMapping in guidMappingSet.guidMappings)
				{
					if (!fileContent.Contains(guidMapping.oldGUID))
						continue;

					if (isLog)
						UnityEngine.Debug.LogFormat("{0}: {1} -> {2}", Path.GetFileName(path), guidMapping.oldGUID, guidMapping.newGUID);

					fileContent = fileContent.Replace(guidMapping.oldGUID, guidMapping.newGUID);
					hasChanged = true;
				}

				if (hasChanged)
				{
					File.WriteAllText(path, fileContent);
				}
			}
		}
	}
}