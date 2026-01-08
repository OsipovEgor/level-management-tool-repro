#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
			_ctx.ShowWelcome = LevelToolSettings.showWelcomeOnOpen;
		}

		public void RefreshLevels()
		{
			_ctx.AllLevels = LevelAssetIndex.FindAllLevels();

			foreach (var lvl in _ctx.AllLevels)
				LevelStableIdUtility.EnsureStableId(lvl);

			ApplyFilter();
		}

		public void ApplyFilter()
		{
			// 1) Берём базовый список в правильном порядке (DB -> потом сироты)
			var ordered = BuildOrderedLevels();

			// 2) Применяем поиск
			if (string.IsNullOrWhiteSpace(_ctx.Search))
			{
				_ctx.FilteredLevels = ordered;
				return;
			}

			string s = _ctx.Search.ToLowerInvariant();
			_ctx.FilteredLevels = ordered
				.Where(l => l != null && l.name.ToLowerInvariant().Contains(s))
				.ToList();
		}

		private List<LevelConfig> BuildOrderedLevels()
		{
			// если DB не выбран — оставляем как есть (то, что вернул индексатор)
			var all = _ctx.AllLevels.Where(l => l != null).Distinct().ToList();

			if (_ctx.Database == null || _ctx.Database.orderedLevels == null || _ctx.Database.orderedLevels.Count == 0)
				return all;

			// 1) DB order
			var result = new List<LevelConfig>(_ctx.Database.orderedLevels.Count + 32);
			var inDb = new HashSet<LevelConfig>();

			foreach (var lvl in _ctx.Database.orderedLevels)
			{
				if (lvl == null) continue;
				if (inDb.Add(lvl))
					result.Add(lvl);
			}

			// 2) Orphans (assets not in DB) — добавляем в конец
			// Можно сортировать по имени, чтобы было стабильно.
			var orphans = all.Where(l => l != null && !inDb.Contains(l))
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
			if (lvl == null) return;

			if (selected) _ctx.SelectedStableIds.Add(lvl.StableId);
			else _ctx.SelectedStableIds.Remove(lvl.StableId);
		}

		public List<LevelConfig> GetSelectedLevels()
		{
			List<LevelConfig> list = new();
			foreach (LevelConfig lvl in _ctx.AllLevels)
				if (lvl != null && _ctx.SelectedStableIds.Contains(lvl.StableId))
					list.Add(lvl);
			return list;
		}

		public LevelConfig GetDatabaseAnchorLevel()
		{
			if (_ctx.SelectedIndex < 0) return null;

			List<LevelConfig> filtered = string.IsNullOrWhiteSpace(_ctx.Search)
				? _ctx.AllLevels
				: _ctx.AllLevels.Where(l =>
						l != null && l.name.ToLowerInvariant().Contains(_ctx.Search.ToLowerInvariant()))
					.ToList();

			if (_ctx.SelectedIndex < 0 || _ctx.SelectedIndex >= filtered.Count) return null;
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
			if (_ctx.Database == null) return;

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

			LevelToolSettings.showWelcomeOnOpen = false;
			AssetDatabase.SaveAssets();
		}

		// -------------------------
		// Create / Duplicate / Delete
		// -------------------------

		public void CreateLevelsClicked()
		{
			string folder = LevelAssetCreationUtility.DefaultLevelsFolder;
			_ = LevelAssetCreationUtility.GetUniquePath(folder, "Temp"); // ensure folder exists

			if (LevelToolSettings.autoNamingEnabled)
			{
				CreateMultipleLevelsAutoNamed(_ctx.CreateCount);
				return;
			}

			// If not auto naming -> prompt(s)
			if (_ctx.CreateCount > 1)
			{
				HashSet<string> existingNames = LevelAssetIndex.CollectAllLevelAssetNames();

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

			{
				HashSet<string> existingNames = LevelAssetIndex.CollectAllLevelAssetNames();
				LevelNamePromptWindow.Show(
					title: "Create Level",
					folder: folder,
					initialName: "Level_New",
					existingNames: existingNames,
					onOk: (levelName) => CreateSingleLevelWithName(folder, levelName),
					onCancel: () => { }
				);
			}
		}

		public void DeleteLevelsBatch(List<LevelConfig> levels)
		{
			if (levels == null) return;

			levels = levels.Where(l => l != null).Distinct().ToList();
			if (levels.Count == 0) return;

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
					dbIndexByLevel[lvl] = _ctx.Database.orderedLevels.IndexOf(lvl);
			}

			foreach (LevelConfig lvl in levels)
			{
				string path = AssetDatabase.GetAssetPath(lvl);
				if (string.IsNullOrEmpty(path)) continue;

				string json = EditorJsonUtility.ToJson(lvl);

				int idx = -1;
				if (dbIndexByLevel != null && dbIndexByLevel.TryGetValue(lvl, out var storedIdx))
					idx = storedIdx;

				records.Add(new LevelAssetRecord
				{
					assetPath = path,
					json = json,
					databasePath = dbPath,
					databaseIndex = idx
				});
			}

			if (records.Count == 0) return;

			int group = Undo.GetCurrentGroup();
			Undo.IncrementCurrentGroup();
			Undo.SetCurrentGroupName($"Delete {records.Count} Levels");

			LevelAssetUndoBridge bridge = LevelAssetUndoManager.Bridge;

// Undo for database (before we mutate orderedLevels)
			if (_ctx.Database != null && _ctx.Database.orderedLevels != null)
				Undo.RegisterCompleteObjectUndo(_ctx.Database, "Delete Levels (Database)");

// IMPORTANT: do NOT register bridge undo here – AddDeletedRecords already does it.
			LevelUndoBridgeSerializedOps.AddDeletedRecords(bridge, records, "Delete Levels (Bridge)");

// Remove from database list (still undoable because we registered DB above)
			if (_ctx.Database != null && _ctx.Database.orderedLevels != null)
			{
				List<LevelAssetRecord> ordered = records
					.Where(r => r.databaseIndex >= 0)
					.OrderByDescending(r => r.databaseIndex)
					.ToList();

				foreach (LevelAssetRecord rec in ordered)
				{
					if (rec.databaseIndex >= 0 && rec.databaseIndex < _ctx.Database.orderedLevels.Count)
					{
						LevelConfig obj = AssetDatabase.LoadAssetAtPath<LevelConfig>(rec.assetPath);
						if (obj != null) _ctx.Database.orderedLevels.Remove(obj);
						else _ctx.Database.orderedLevels.RemoveAt(rec.databaseIndex);
					}
				}

				foreach (LevelAssetRecord rec in records.Where(r => r.databaseIndex < 0))
				{
					LevelConfig obj = AssetDatabase.LoadAssetAtPath<LevelConfig>(rec.assetPath);
					if (obj != null) _ctx.Database.orderedLevels.Remove(obj);
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

// IMPORTANT: refresh snapshots AFTER files are actually gone
			LevelAssetUndoManager.RefreshSnapshotsNow();

			Undo.CollapseUndoOperations(group);

			foreach (LevelConfig lvl in levels)
				SetSelected(lvl, false);

			RefreshLevels();
			Validate();
		}

		// -------------------------
		// Internals (create)
		// -------------------------

		private void CreateMultipleLevelsManual(List<string> names, bool insertIntoDatabaseAfterSelected)
		{
			if (names == null || names.Count == 0) return;

			string folder = LevelAssetCreationUtility.DefaultLevelsFolder;

			LevelAssetUndoBridge bridge = LevelAssetUndoManager.Bridge;
			Undo.RecordObject(bridge, "Batch Create Levels");

			List<LevelConfig> created = new List<LevelConfig>(names.Count);
			LevelConfig last = null;

			foreach (string n in names)
			{
				last = CreateLevelAssetWithPreset(folder, n);
				if (last != null) created.Add(last);
			}

			EditorUtility.SetDirty(bridge);
			LevelAssetUndoManager.RefreshSnapshotsNow();

			if (_ctx.Database != null && insertIntoDatabaseAfterSelected && created.Count > 0)
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
			if (_ctx.Database == null || created == null || created.Count == 0) return;
			_ctx.Database.orderedLevels ??= new List<LevelConfig>();

			LevelConfig anchor = GetDatabaseAnchorLevel();

			Undo.RecordObject(_ctx.Database, "Add Created Levels Into Database");

			int insertIndex = _ctx.Database.orderedLevels.Count;
			if (anchor != null)
			{
				int anchorIndex = _ctx.Database.orderedLevels.IndexOf(anchor);
				if (anchorIndex >= 0) insertIndex = anchorIndex + 1;
			}

			foreach (LevelConfig lvl in created)
			{
				if (lvl == null) continue;
				if (_ctx.Database.orderedLevels.Contains(lvl)) continue;

				insertIndex = Mathf.Clamp(insertIndex, 0, _ctx.Database.orderedLevels.Count);
				_ctx.Database.orderedLevels.Insert(insertIndex, lvl);
				insertIndex++;
			}

			EditorUtility.SetDirty(_ctx.Database);
			AssetDatabase.SaveAssets();
		}

		private void AppendCreatedLevelsToDatabase(List<LevelConfig> created)
		{
			if (_ctx.Database == null || created == null || created.Count == 0) return;
			_ctx.Database.orderedLevels ??= new List<LevelConfig>();

			Undo.RecordObject(_ctx.Database, "Append Created Levels To Database");

			foreach (LevelConfig lvl in created)
			{
				if (lvl == null) continue;
				if (_ctx.Database.orderedLevels.Contains(lvl)) continue;
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

			var created = new List<LevelConfig>(count);

			for (int i = 0; i < count; i++)
			{
				string levelName = LevelAssetCreationUtility.GenerateNextLevelName();
				var lvl = CreateLevelAssetWithPreset(folder, levelName);
				if (lvl != null) created.Add(lvl);
			}

			if (created.Count > 0 && _ctx.Database != null)
			{
				// ✅ ВАЖНО: явное добавление в DB (undoable),
				// вместо "надеемся на Sync" в FinalizeAfterCreate
				AppendCreatedLevelsToDatabase(created);

				// ✅ Пишем created records в bridge уже ПОСЛЕ того,
				// как уровни реально попали в DB (теперь index можно получить точно)
				foreach (var lvl in created)
					RecordCreatedLevelInBridge(bridge, lvl);
			}
			else if (created.Count > 0)
			{
				// DB не выбрана — просто записываем created records без databasePath/index
				foreach (var lvl in created)
					RecordCreatedLevelInBridge(bridge, lvl);
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

			if (created != null && _ctx.Database != null)
			{
				AppendCreatedLevelsToDatabase(new List<LevelConfig> { created });
			}

			if (created != null)
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

			// recordInBridge больше не используется здесь намеренно:
			// теперь запись в bridge делается в RecordCreatedLevelInBridge(...)
			// после того, как ассет добавлен в DB и индекс известен.

			return asset;
		}

		private void RecordCreatedLevelInBridge(LevelAssetUndoBridge bridge, LevelConfig asset)
		{
			if (bridge == null || asset == null) return;

			string path = AssetDatabase.GetAssetPath(asset);
			if (string.IsNullOrEmpty(path)) return;

			string json = EditorJsonUtility.ToJson(asset);

			string dbPath = null;
			int dbIndex = -1;

			if (_ctx.Database != null && _ctx.Database.orderedLevels != null)
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
					lvl.goals.Add(_ctx.PresetGoals[i]);
			}

			EditorUtility.SetDirty(lvl);
		}

		private void FinalizeAfterCreate(LevelConfig lastCreated)
		{
			RefreshLevels();

			if (_ctx.Database != null)
				LevelDatabaseSync.Sync(_ctx.Database, _ctx.AllLevels);

			Validate();

			if (lastCreated != null)
			{
				Selection.activeObject = lastCreated;
				EditorGUIUtility.PingObject(lastCreated);
				_ctx.SelectedIndex = _ctx.AllLevels.IndexOf(lastCreated);
			}
		}

		// -------------------------
		// Goals apply (used by UI)
		// -------------------------

		public void ApplyGoalsPerLevel(Dictionary<LevelConfig, List<LevelGoal>> map)
		{
			if (map == null || map.Count == 0) return;

			var levels = map.Keys.Where(x => x != null).ToList();
			if (levels.Count == 0) return;

			Undo.RecordObjects(levels.Cast<Object>().ToArray(), "Apply Goals (Per Level)");

			foreach (var kv in map)
			{
				var lvl = kv.Key;
				if (lvl == null) continue;

				var goals = kv.Value ?? new List<LevelGoal>();
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