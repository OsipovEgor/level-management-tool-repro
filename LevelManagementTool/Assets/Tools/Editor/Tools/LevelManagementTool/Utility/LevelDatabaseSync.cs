#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public struct SyncReport
	{
		public int Added;
		public int RemovedNulls;
		public int TotalAfter;
	}

	public static class LevelDatabaseSync
	{
		public static SyncReport Sync(LevelDatabase db, IReadOnlyList<LevelConfig> allLevels)
		{
			if (!db)
				throw new ArgumentNullException(nameof(db));

			if (allLevels == null)
				throw new ArgumentNullException(nameof(allLevels));

			Undo.RecordObject(db, "Sync Level Database");

			int removedNulls = 0;
			for (int i = db.orderedLevels.Count - 1; i >= 0; i--)
			{
				if (db.orderedLevels[i])
					continue;

				db.orderedLevels.RemoveAt(i);
				removedNulls++;
			}

			HashSet<LevelConfig> set = new(db.orderedLevels);
			int added = 0;
			foreach (LevelConfig lvl in allLevels)
			{
				if (lvl == null)
					continue;

				if (!set.Add(lvl))
					continue;

				db.orderedLevels.Add(lvl);
				added++;
			}

			EditorUtility.SetDirty(db);

			return new SyncReport
			{
				Added = added,
				RemovedNulls = removedNulls,
				TotalAfter = db.orderedLevels.Count
			};
		}

		public static LevelIdMap BuildIdMap(LevelDatabase db, string assetPath)
		{
			LevelIdMap map = ScriptableObject.CreateInstance<LevelIdMap>();
			map.entries = new List<LevelIdMapEntry>(db.Count);

			for (int i = 0; i < db.orderedLevels.Count; i++)
			{
				LevelConfig lvl = db.orderedLevels[i];

				if (!lvl)
					continue;

				map.entries.Add(new LevelIdMapEntry
				{
					Index = i,
					StableId = lvl.StableId,
					Level = lvl
				});
			}

			AssetDatabase.CreateAsset(map, assetPath);
			AssetDatabase.SaveAssets();
			return map;
		}
	}
}
#endif