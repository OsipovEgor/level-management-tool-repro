#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Levels;

namespace Game.Levels.EditorTool
{
	[InitializeOnLoad]
	public static class LevelAssetUndoManager
	{
		private const string BridgeAssetPath = "Assets/Tools/Levels/Editor/LevelAssetUndoBridge.asset";
		private const string SessionClearedKey = "Game.Levels.UndoBridge.ClearedThisSession";

		private static LevelAssetUndoBridge _bridge;

		private static HashSet<string> _prevCreated = new(StringComparer.OrdinalIgnoreCase);
		private static HashSet<string> _prevDeleted = new(StringComparer.OrdinalIgnoreCase);

		public static event Action AssetsChanged;
		private static readonly List<LevelAssetRecord> _pendingRelinks = new();

		static LevelAssetUndoManager()
		{
			EditorApplication.delayCall += EnsureInitialized;
		}

		public static LevelAssetUndoBridge Bridge => EnsureBridge();

		private static void EnsureInitialized()
		{
			EnsureBridge();

			ClearBridgeOnEditorStartIfNeeded(); // <-- ДО CacheCurrentSets()
			CacheCurrentSets();

			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			Undo.undoRedoPerformed += OnUndoRedoPerformed;
		}

		private static void ClearBridgeOnEditorStartIfNeeded()
		{
			// SessionState сбрасывается при перезапуске Unity Editor (но сохраняется при domain reload)
			if (SessionState.GetBool(SessionClearedKey, false))
				return;

			SessionState.SetBool(SessionClearedKey, true);

			LevelAssetUndoBridge bridge = EnsureBridge();
			if (bridge == null) return;

			// Без Undo: это "служебное" состояние, не пользовательская правка.
			bridge.created.Clear();
			bridge.deleted.Clear();

			EditorUtility.SetDirty(bridge);
			AssetDatabase.SaveAssets();
		}

		private static LevelAssetUndoBridge EnsureBridge()
		{
			if (_bridge != null) return _bridge;

			EnsureFolderExists(Path.GetDirectoryName(BridgeAssetPath)?.Replace('\\', '/'));

			_bridge = AssetDatabase.LoadAssetAtPath<LevelAssetUndoBridge>(BridgeAssetPath);
			if (_bridge == null)
			{
				_bridge = ScriptableObject.CreateInstance<LevelAssetUndoBridge>();
				AssetDatabase.CreateAsset(_bridge, BridgeAssetPath);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			_bridge.hideFlags = HideFlags.NotEditable;
			return _bridge;
		}

		private static void OnUndoRedoPerformed()
		{
			LevelAssetUndoBridge bridge = EnsureBridge();

			HashSet<string> createdNow = new(bridge.created.Select(x => x.assetPath), StringComparer.OrdinalIgnoreCase);
			HashSet<string> deletedNow = new(bridge.deleted.Select(x => x.assetPath), StringComparer.OrdinalIgnoreCase);

			List<string> createdAdded = createdNow.Except(_prevCreated).ToList();
			List<string> createdRemoved = _prevCreated.Except(createdNow).ToList();

			List<string> deletedAdded = deletedNow.Except(_prevDeleted).ToList();
			List<string> deletedRemoved = _prevDeleted.Except(deletedNow).ToList();

			foreach (string path in createdAdded)
			{
				LevelAssetRecord rec = bridge.created.FirstOrDefault(r =>
					string.Equals(r.assetPath, path, StringComparison.OrdinalIgnoreCase));

				if (rec == null) continue;

				EnsureAssetExistsFromJson(rec.assetPath, rec.json);

				if (!string.IsNullOrEmpty(rec.databasePath))
					_pendingRelinks.Add(rec);
			}

			foreach (string path in createdRemoved)
			{
				EnsureAssetDeleted(path);
			}

			foreach (string path in deletedAdded)
			{
				EnsureAssetDeleted(path);
			}

			foreach (string path in deletedRemoved)
			{
				LevelAssetRecord rec = bridge.deleted.FirstOrDefault(r =>
					string.Equals(r.assetPath, path, StringComparison.OrdinalIgnoreCase));
			}

			RestoreAssetsFromPrevDeleted(deletedRemoved);

			CacheCurrentSets();

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			if (_pendingRelinks.Count > 0)
			{
				LevelAssetRecord[] copy = _pendingRelinks.ToArray();
				_pendingRelinks.Clear();

				EditorApplication.delayCall += () =>
				{
					ApplyRelinks(copy);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
					AssetsChanged?.Invoke();
				};
			}
			else
			{
				AssetsChanged?.Invoke();
			}
		}

		private static void ApplyRelinks(LevelAssetRecord[] records)
		{
			foreach (LevelAssetRecord rec in records)
			{
				if (rec == null) continue;
				if (string.IsNullOrEmpty(rec.databasePath)) continue;

				LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(rec.databasePath);
				if (db == null) continue;

				LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>(rec.assetPath);
				if (level == null) continue;

				if (db.orderedLevels == null) continue;
				if (db.orderedLevels.Contains(level)) continue;

				if (rec.databaseIndex >= 0 && rec.databaseIndex < db.orderedLevels.Count)
				{
					if (db.orderedLevels[rec.databaseIndex] == null)
					{
						db.orderedLevels[rec.databaseIndex] = level;
						EditorUtility.SetDirty(db);
						continue;
					}
				}

				for (int i = 0; i < db.orderedLevels.Count; i++)
				{
					if (db.orderedLevels[i] == null)
					{
						db.orderedLevels[i] = level;
						EditorUtility.SetDirty(db);
						break;
					}
				}
			}
		}

		private static Dictionary<string, LevelAssetRecord> _prevDeletedRecordByPath =
			new(StringComparer.OrdinalIgnoreCase);

		public static void RefreshSnapshotsNow()
		{
			EnsureBridge();
			CacheCurrentSets();
		}

		private static void CacheCurrentSets()
		{
			LevelAssetUndoBridge bridge = EnsureBridge();

			_prevCreated =
				new HashSet<string>(bridge.created.Select(x => x.assetPath), StringComparer.OrdinalIgnoreCase);
			_prevDeleted =
				new HashSet<string>(bridge.deleted.Select(x => x.assetPath), StringComparer.OrdinalIgnoreCase);

			_prevDeletedRecordByPath.Clear();
			foreach (LevelAssetRecord rec in bridge.deleted)
			{
				if (!string.IsNullOrEmpty(rec.assetPath))
					_prevDeletedRecordByPath[rec.assetPath] = rec;
			}
		}

		private static void RestoreAssetsFromPrevDeleted(List<string> deletedRemovedPaths)
		{
			foreach (string path in deletedRemovedPaths)
			{
				if (_prevDeletedRecordByPath.TryGetValue(path, out LevelAssetRecord rec) && rec != null)
				{
					EnsureAssetExistsFromJson(rec.assetPath, rec.json);
					_pendingRelinks.Add(rec);
				}
			}
		}

		private static void EnsureAssetExistsFromJson(string assetPath, string json)
		{
			if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(json))
				return;

			LevelConfig existing = AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath);
			if (existing != null)
				return;

			EnsureFolderExists(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));

			LevelConfig asset = ScriptableObject.CreateInstance<LevelConfig>();
			EditorJsonUtility.FromJsonOverwrite(json, asset);
			AssetDatabase.CreateAsset(asset, assetPath);

			EditorUtility.SetDirty(asset);
		}

		private static void EnsureAssetDeleted(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath))
				return;

			if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
				return;

			AssetDatabase.DeleteAsset(assetPath);
		}

		private static void EnsureFolderExists(string folderPath)
		{
			if (string.IsNullOrEmpty(folderPath)) return;
			if (AssetDatabase.IsValidFolder(folderPath)) return;

			string parent = "Assets";
			string relative = folderPath.StartsWith("Assets/") ? folderPath.Substring("Assets/".Length) : folderPath;
			string[] parts = relative.Split('/');

			foreach (string part in parts)
			{
				if (string.IsNullOrEmpty(part)) continue;
				string current = $"{parent}/{part}";
				if (!AssetDatabase.IsValidFolder(current))
					AssetDatabase.CreateFolder(parent, part);
				parent = current;
			}
		}
	}
}
#endif