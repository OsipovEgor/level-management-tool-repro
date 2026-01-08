#if UNITY_EDITOR
using System;
using UnityEditor;

namespace Game.Levels.EditorTool
{
	public static class LevelStableIdUtility
	{
		public static bool EnsureStableId(LevelConfig level)
		{
			if (!level)
				return false;

			if (!string.IsNullOrEmpty(level.StableId))
				return false;

			Undo.RecordObject(level, "Generate Level Stable ID");

			SerializedObject so = new(level);
			SerializedProperty prop = so.FindProperty("stableId");
			prop.stringValue = Guid.NewGuid().ToString("N");
			so.ApplyModifiedPropertiesWithoutUndo();

			EditorUtility.SetDirty(level);
			return true;
		}
	}
}
#endif