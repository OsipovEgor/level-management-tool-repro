#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Game.Levels.EditorTool
{
	public static class LevelAssetIndex
	{
		public static List<LevelConfig> FindAllLevels()
		{
			string[] guids = AssetDatabase.FindAssets("t:Game.Levels.LevelConfig");
			List<LevelConfig> result = new(guids.Length);

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				LevelConfig asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
				if (asset != null) result.Add(asset);
			}

			result.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
			return result;
		}

		public static HashSet<string> CollectAllLevelAssetNames()
		{
			HashSet<string> set = new(StringComparer.OrdinalIgnoreCase);

			List<LevelConfig> levels = FindAllLevels();
			foreach (LevelConfig lvl in levels)
			{
				if (lvl == null) continue;
				set.Add(lvl.name.Trim());
			}

			return set;
		}
	}
}
#endif