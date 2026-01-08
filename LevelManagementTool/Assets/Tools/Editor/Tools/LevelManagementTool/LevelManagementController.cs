#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Levels.EditorTool
{
	public class LevelManagementController
	{
		private readonly LevelManagementContext _ctx;

		public LevelManagementController(LevelManagementContext ctx)
		{
			_ctx = ctx;
		}

		public void LoadSettings()
		{
			_ctx.ShowWelcome = LevelToolSettings.ShowWelcomeOnOpen;
		}

		public void RefreshLevels()
		{
			_ctx.AllLevels = LevelAssetIndex.FindAllLevels();

			foreach (LevelConfig lvl in _ctx.AllLevels)
			{
				LevelStableIdUtility.EnsureStableId(lvl);
			}

			ApplyFilter();
		}

		public void ApplyFilter()
		{
			List<LevelConfig> ordered = BuildOrderedLevels();

			if (string.IsNullOrWhiteSpace(_ctx.Search))
			{
				_ctx.FilteredLevels = ordered;
				return;
			}

			string s = _ctx.Search.ToLowerInvariant();
			_ctx.FilteredLevels = ordered
				.Where(levelConfig => levelConfig != null && levelConfig.name.ToLowerInvariant().Contains(s))
				.ToList();
		}

		private List<LevelConfig> BuildOrderedLevels()
		{
			List<LevelConfig> all = _ctx.AllLevels.Where(l => l != null).Distinct().ToList();

			if (_ctx.Database == null || _ctx.Database.orderedLevels == null || _ctx.Database.orderedLevels.Count == 0)
				return all;

			List<LevelConfig> result = new(_ctx.Database.orderedLevels.Count + 32);
			HashSet<LevelConfig> inDb = new();

			foreach (LevelConfig lvl in _ctx.Database.orderedLevels)
			{
				if (!lvl)
					continue;

				if (inDb.Add(lvl))
					result.Add(lvl);
			}

			// 2) Orphans (assets not in DB) — добавляем в конец
			// Можно сортировать по имени, чтобы было стабильно.
			IOrderedEnumerable<LevelConfig> orphans = all.Where(l => l != null && !inDb.Contains(l))
				.OrderBy(l => l.name, StringComparer.OrdinalIgnoreCase);

			result.AddRange(orphans);

			return result;
		}

		public void Validate()
		{
			_ctx.Issues = LevelValidation.ValidateAll(_ctx.Database, _ctx.AllLevels);
		}

		public void SetSelected(LevelConfig lvl, bool selected)
		{
			if (!lvl)
				return;

			if (selected)
			{
				_ctx.SelectedStableIds.Add(lvl.StableId);
			}
			else
			{
				_ctx.SelectedStableIds.Remove(lvl.StableId);
			}
		}

		public List<LevelConfig> GetSelectedLevels()
		{
			List<LevelConfig> list = new();

			foreach (LevelConfig lvl in _ctx.AllLevels)
			{
				if (lvl != null && _ctx.SelectedStableIds.Contains(lvl.StableId))
					list.Add(lvl);
			}

			return list;
		}

		private LevelConfig GetDatabaseAnchorLevel()
		{
			if (_ctx.SelectedIndex < 0)
				return null;

			List<LevelConfig> filtered = string.IsNullOrWhiteSpace(_ctx.Search)
				? _ctx.AllLevels
				: _ctx.AllLevels.Where(levelConfig =>
						levelConfig != null &&
						levelConfig.name.ToLowerInvariant().Contains(_ctx.Search.ToLowerInvariant()))
					.ToList();

			if (_ctx.SelectedIndex < 0 || _ctx.SelectedIndex >= filtered.Count)
				return null;

			return filtered[_ctx.SelectedIndex];
		}

		// -------------------------
		// Top bar actions
		// -------------------------

		public void SyncDb()
		{
			if (_ctx.Database == null) return;

			SyncReport report = LevelDatabaseSync.Sync(_ctx.Database, _ctx.AllLevels);
			Debug.Log(
				$"[LevelTool] Sync: +{report.Added}, removed nulls {report.RemovedNulls}, total {report.TotalAfter}");
			Validate();
		}

		public void ExportIdMap()
		{
			if (!_ctx.Database)
				return;

			string path = EditorUtility.SaveFilePanelInProject(
				"Save LevelIdMap",
				"LevelIdMap",
				"asset",
				"Choose location for LevelIdMap asset");

			if (string.IsNullOrEmpty(path))
				return;

			LevelDatabaseSync.BuildIdMap(_ctx.Database, path);
			AssetDatabase.Refresh();
		}

		public void HideWelcome()
		{
			_ctx.ShowWelcome = false;

			LevelToolSettings.ShowWelcomeOnOpen = false;
			AssetDatabase.SaveAssets();
		}

		// -------------------------
		// Create / Duplicate / Delete
		// -------------------------

		public void CreateLevelsClicked()
		{
			string folder = LevelAssetCreationUtility.DefaultLevelsFolder;
			_ = LevelAssetCreationUtility.GetUniquePath(folder, "Temp");

			if (LevelToolSettings.AutoNamingEnabled)
			{
				CreateMultipleLevelsAutoNamed(_ctx.CreateCount);
				return;
			}

			HashSet<string> existingNames = LevelAssetIndex.CollectAllLevelAssetNames();
			if (_ctx.CreateCount > 1)
			{
				bool canInsert = _ctx.Database != null;
				string afterLabel = "(end)";

				LevelConfig anchor = GetDatabaseAnchorLevel();
				if (anchor != null) afterLabel = anchor.name;

				LevelBatchNamePromptWindow.Show(
					title: "Batch Create Levels",
					folder: folder,
					count: _ctx.CreateCount,
					existingNames: existingNames,
					canInsertIntoDatabase: canInsert,
					insertAfterLabel: afterLabel,
					onOk: CreateMultipleLevelsManual,
					onCancel: () => { }
				);
				return;
			}

			LevelNamePromptWindow.Show(
				title: "Create Level",
				folder: folder,
				initialName: "Level_New",
				existingNames: existingNames,
				onOk: (levelName) => CreateSingleLevelWithName(folder, levelName),
				onCancel: () => { }
			);
		}

		public void DeleteLevelsBatch(List<LevelConfig> levels)
		{
			if (levels == null)
				return;

			levels = levels.Where(l => l != null).Distinct().ToList();
			
			if (levels.Count == 0)
				return;

			if (!EditorUtility.DisplayDialog(
					"Delete Levels",
					$"Delete {levels.Count} level(s)?\n\nUndo (Ctrl+Z) will restore assets and relink them in the database.",
					"Delete",
					"Cancel"))
				return;

			List<LevelAssetRecord> records = new(levels.Count);

			string dbPath = null;
			Dictionary<LevelConfig, int> dbIndexByLevel = null;

			if (_ctx.Database != null && _ctx.Database.orderedLevels != null)
			{
				dbPath = AssetDatabase.GetAssetPath(_ctx.Database);
				dbIndexByLevel = new Dictionary<LevelConfig, int>(levels.Count);

				foreach (LevelConfig lvl in levels)
				{
					dbIndexByLevel[lvl] = _ctx.Database.orderedLevels.IndexOf(lvl);
				}
			}

			foreach (LevelConfig lvl in levels)
			{
				string path = AssetDatabase.GetAssetPath(lvl);
				if (string.IsNullOrEmpty(path)) continue;

				string json = EditorJsonUtility.ToJson(lvl);

				int idx = -1;
				if (dbIndexByLevel != null && dbIndexByLevel.TryGetValue(lvl, out int storedIdx))
					idx = storedIdx;

				records.Add(new LevelAssetRecord
				{
					assetPath = path,
					json = json,
					databasePath = dbPath,
					databaseIndex = idx
				});
			}

			if (records.Count == 0)
				return;

			int group = Undo.GetCurrentGroup();
			Undo.IncrementCurrentGroup();
			Undo.SetCurrentGroupName($"Delete {records.Count} Levels");

			LevelAssetUndoBridge bridge = LevelAssetUndoManager.Bridge;

			if (_ctx.Database != null && _ctx.Database.orderedLevels != null)
				Undo.RegisterCompleteObjectUndo(_ctx.Database, "Delete Levels (Database)");

			LevelUndoBridgeSerializedOps.AddDeletedRecords(bridge, records, "Delete Levels (Bridge)");

			if (_ctx.Database != null && _ctx.Database.orderedLevels != null)
			{
				List<LevelAssetRecord> ordered = records
					.Where(r => r.databaseIndex >= 0)
					.OrderByDescending(r => r.databaseIndex)
					.ToList();

				foreach (LevelAssetRecord rec in ordered)
				{
					if (rec.databaseIndex < 0 || rec.databaseIndex >= _ctx.Database.orderedLevels.Count)
						continue;
					
					LevelConfig obj = AssetDatabase.LoadAssetAtPath<LevelConfig>(rec.assetPath);
					if (obj != null)
					{
						_ctx.Database.orderedLevels.Remove(obj);
					}
					else
					{
						_ctx.Database.orderedLevels.RemoveAt(rec.databaseIndex);
					}
				}

				foreach (LevelAssetRecord rec in records.Where(r => r.databaseIndex < 0))
				{
					LevelConfig obj = AssetDatabase.LoadAssetAtPath<LevelConfig>(rec.assetPath);
					if (obj != null)
						_ctx.Database.orderedLevels.Remove(obj);
				}

				EditorUtility.SetDirty(_ctx.Database);
			}

			AssetDatabase.StartAssetEditing();
			try
			{
				foreach (LevelAssetRecord rec in records)
				{
					if (!string.IsNullOrEmpty(rec.assetPath))
						AssetDatabase.DeleteAsset(rec.assetPath);
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			LevelAssetUndoManager.RefreshSnapshotsNow();

			Undo.CollapseUndoOperations(group);

			foreach (LevelConfig lvl in levels)
			{
				SetSelected(lvl, false);
			}

			RefreshLevels();
			Validate();
		}

		// -------------------------
		// Internals (create)
		// -------------------------

		private void CreateMultipleLevelsManual(List<string> names, bool insertIntoDatabaseAfterSelected)
		{
			if (names == null || names.Count == 0)
				return;

			string folder = LevelAssetCreationUtility.DefaultLevelsFolder;

			LevelAssetUndoBridge bridge = LevelAssetUndoManager.Bridge;
			Undo.RecordObject(bridge, "Batch Create Levels");

			List<LevelConfig> created = new(names.Count);
			LevelConfig last = null;

			foreach (string n in names)
			{
				last = CreateLevelAssetWithPreset(folder, n);
				
				if (last)
					created.Add(last);
			}

			EditorUtility.SetDirty(bridge);
			LevelAssetUndoManager.RefreshSnapshotsNow();

			if (_ctx.Database && insertIntoDatabaseAfterSelected && created.Count > 0)
			{
				InsertCreatedLevelsAfterAnchor(created);
			}
			else if (_ctx.Database != null)
			{
				AppendCreatedLevelsToDatabase(created);
			}

			FinalizeAfterCreate(last);
		}

		private void InsertCreatedLevelsAfterAnchor(List<LevelConfig> created)
		{
			if (_ctx.Database == null || created == null || created.Count == 0)
				return;
			
			_ctx.Database.orderedLevels ??= new List<LevelConfig>();

			LevelConfig anchor = GetDatabaseAnchorLevel();

			Undo.RecordObject(_ctx.Database, "Add Created Levels Into Database");

			int insertIndex = _ctx.Database.orderedLevels.Count;
			if (anchor != null)
			{
				int anchorIndex = _ctx.Database.orderedLevels.IndexOf(anchor);
				
				if (anchorIndex >= 0)
					insertIndex = anchorIndex + 1;
			}

			foreach (LevelConfig lvl in created)
			{
				if (lvl == null) 
					continue;
				
				if (_ctx.Database.orderedLevels.Contains(lvl))
					continue;

				insertIndex = Mathf.Clamp(insertIndex, 0, _ctx.Database.orderedLevels.Count);
				_ctx.Database.orderedLevels.Insert(insertIndex, lvl);
				insertIndex++;
			}

			EditorUtility.SetDirty(_ctx.Database);
			AssetDatabase.SaveAssets();
		}

		private void AppendCreatedLevelsToDatabase(List<LevelConfig> created)
		{
			if (_ctx.Database == null || created == null || created.Count == 0)
				return;
			
			_ctx.Database.orderedLevels ??= new List<LevelConfig>();

			Undo.RecordObject(_ctx.Database, "Append Created Levels To Database");

			foreach (LevelConfig lvl in created)
			{
				if (lvl == null)
					continue;
				
				if (_ctx.Database.orderedLevels.Contains(lvl))
					continue;
				
				_ctx.Database.orderedLevels.Add(lvl);
			}

			EditorUtility.SetDirty(_ctx.Database);
			AssetDatabase.SaveAssets();
		}

		private void CreateMultipleLevelsAutoNamed(int count)
		{
			string folder = LevelAssetCreationUtility.DefaultLevelsFolder;

			LevelAssetUndoBridge bridge = LevelAssetUndoManager.Bridge;
			Undo.RecordObject(bridge, "Create Levels");

			List<LevelConfig> created = new(count);

			for (int i = 0; i < count; i++)
			{
				string levelName = LevelAssetCreationUtility.GenerateNextLevelName();
				LevelConfig lvl = CreateLevelAssetWithPreset(folder, levelName);
				if (lvl != null) created.Add(lvl);
			}

			switch (created.Count)
			{
				case > 0 when _ctx.Database != null:
				{
					AppendCreatedLevelsToDatabase(created);

					foreach (LevelConfig lvl in created)
					{
						RecordCreatedLevelInBridge(bridge, lvl);
					}

					break;
				}
				case > 0:
				{
					foreach (LevelConfig lvl in created)
					{
						RecordCreatedLevelInBridge(bridge, lvl);
					}

					break;
				}
			}

			if (created.Count > 0)
			{
				EditorUtility.SetDirty(bridge);
				LevelAssetUndoManager.RefreshSnapshotsNow();
			}

			FinalizeAfterCreate(created.Count > 0 ? created[^1] : null);
		}

		private void CreateSingleLevelWithName(string folder, string levelName)
		{
			LevelAssetUndoBridge bridge = LevelAssetUndoManager.Bridge;
			Undo.RecordObject(bridge, "Create Level");

			LevelConfig created = CreateLevelAssetWithPreset(folder, levelName);

			if (created && _ctx.Database)
			{
				AppendCreatedLevelsToDatabase(new List<LevelConfig> { created });
			}

			if (created)
			{
				RecordCreatedLevelInBridge(bridge, created);

				EditorUtility.SetDirty(bridge);
				LevelAssetUndoManager.RefreshSnapshotsNow();
			}

			FinalizeAfterCreate(created);
		}

		private LevelConfig CreateLevelAssetWithPreset(string folder, string assetName)
		{
			string path = LevelAssetCreationUtility.GetUniquePath(folder, assetName);

			LevelConfig asset = ScriptableObject.CreateInstance<LevelConfig>();
			AssetDatabase.CreateAsset(asset, path);

			ApplyCreatePreset(asset);

			LevelStableIdUtility.EnsureStableId(asset);

			EditorUtility.SetDirty(asset);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			return asset;
		}

		private void RecordCreatedLevelInBridge(LevelAssetUndoBridge bridge, LevelConfig asset)
		{
			if (!bridge || !asset)
				return;

			string path = AssetDatabase.GetAssetPath(asset);
			
			if (string.IsNullOrEmpty(path))
				return;

			string json = EditorJsonUtility.ToJson(asset);

			string dbPath = null;
			int dbIndex = -1;

			if (_ctx.Database && _ctx.Database.orderedLevels != null)
			{
				dbPath = AssetDatabase.GetAssetPath(_ctx.Database);
				dbIndex = _ctx.Database.orderedLevels.IndexOf(asset);
			}

			LevelUndoBridgeSerializedOps.AddCreatedRecord(
				bridge,
				new LevelAssetRecord
				{
					assetPath = path,
					json = json,
					databasePath = dbPath,
					databaseIndex = dbIndex
				},
				"Create Level(s) (Bridge)"
			);
		}

		private void ApplyCreatePreset(LevelConfig lvl)
		{
			Undo.RecordObject(lvl, "Apply Level Preset");

			lvl.timeLimitSeconds = Mathf.Max(1, _ctx.PresetTimeLimitSeconds);
			lvl.difficulty = Mathf.Max(0, _ctx.PresetDifficulty);

			lvl.goals ??= new List<LevelGoal>(3);
			lvl.goals.Clear();

			if (_ctx.PresetGoals != null)
			{
				for (int i = 0; i < _ctx.PresetGoals.Count && i < 3; i++)
				{
					lvl.goals.Add(_ctx.PresetGoals[i]);
				}
			}

			EditorUtility.SetDirty(lvl);
		}

		private void FinalizeAfterCreate(LevelConfig lastCreated)
		{
			RefreshLevels();

			if (_ctx.Database)
				LevelDatabaseSync.Sync(_ctx.Database, _ctx.AllLevels);

			Validate();

			if (!lastCreated)
				return;
			
			Selection.activeObject = lastCreated;
			EditorGUIUtility.PingObject(lastCreated);
			_ctx.SelectedIndex = _ctx.AllLevels.IndexOf(lastCreated);
		}

		// -------------------------
		// Goals apply (used by UI)
		// -------------------------

		public void ApplyGoalsPerLevel(Dictionary<LevelConfig, List<LevelGoal>> map)
		{
			if (map == null || map.Count == 0)
				return;

			List<LevelConfig> levels = map.Keys.Where(x => x).ToList();
			
			if (levels.Count == 0)
				return;

			Undo.RecordObjects(levels.Cast<Object>().ToArray(), "Apply Goals (Per Level)");

			foreach (KeyValuePair<LevelConfig, List<LevelGoal>> kv in map)
			{
				LevelConfig lvl = kv.Key;
				if (!lvl) continue;

				List<LevelGoal> goals = kv.Value ?? new List<LevelGoal>();
				lvl.goals ??= new List<LevelGoal>(3);
				lvl.goals.Clear();

				for (int i = 0; i < goals.Count && i < 3; i++)
					lvl.goals.Add(goals[i]);

				EditorUtility.SetDirty(lvl);
			}

			AssetDatabase.SaveAssets();
			Validate();
		}
	}
}
#endif