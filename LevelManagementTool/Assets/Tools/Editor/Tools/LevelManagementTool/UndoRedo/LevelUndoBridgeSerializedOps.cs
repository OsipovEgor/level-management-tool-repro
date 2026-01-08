#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace Game.Levels.EditorTool
{
	public static class LevelUndoBridgeSerializedOps
	{
		public static void AddDeletedRecords(LevelAssetUndoBridge bridge, List<LevelAssetRecord> records,
			string undoName)
		{
			if (bridge == null || records == null || records.Count == 0) return;

			Undo.RegisterCompleteObjectUndo(bridge, undoName);

			SerializedObject so = new(bridge);
			SerializedProperty listProp = so.FindProperty("deleted");

			int start = listProp.arraySize;
			listProp.arraySize = start + records.Count;

			for (int i = 0; i < records.Count; i++)
			{
				LevelAssetRecord rec = records[i];
				SerializedProperty elem = listProp.GetArrayElementAtIndex(start + i);

				elem.FindPropertyRelative("assetPath").stringValue = rec.assetPath;
				elem.FindPropertyRelative("json").stringValue = rec.json;
				elem.FindPropertyRelative("databasePath").stringValue = rec.databasePath;
				elem.FindPropertyRelative("databaseIndex").intValue = rec.databaseIndex;
			}

			so.ApplyModifiedProperties();
			EditorUtility.SetDirty(bridge);
		}

		public static void AddCreatedRecord(LevelAssetUndoBridge bridge, LevelAssetRecord record, string undoName)
		{
			if (!bridge || record == null) return;

			Undo.RegisterCompleteObjectUndo(bridge, undoName);

			SerializedObject so = new(bridge);
			SerializedProperty listProp = so.FindProperty("created");

			int idx = listProp.arraySize;
			listProp.arraySize = idx + 1;

			SerializedProperty elem = listProp.GetArrayElementAtIndex(idx);
			elem.FindPropertyRelative("assetPath").stringValue = record.assetPath;
			elem.FindPropertyRelative("json").stringValue = record.json;
			elem.FindPropertyRelative("databasePath").stringValue = record.databasePath;
			elem.FindPropertyRelative("databaseIndex").intValue = record.databaseIndex;

			so.ApplyModifiedProperties();
			EditorUtility.SetDirty(bridge);
		}
	}
}
#endif