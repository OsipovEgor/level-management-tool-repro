#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelCreateOptionsView
	{
		public static void Draw(LevelManagementContext ctx)
		{
			ctx.ShowCreateOptions = EditorGUILayout.Foldout(ctx.ShowCreateOptions, "Create Options", true);
			if (!ctx.ShowCreateOptions)
				return;

			using (new EditorGUI.IndentLevelScope())
			{
				EditorGUI.BeginChangeCheck();

				ctx.Settings.autoNamingEnabled =
					EditorGUILayout.ToggleLeft("Auto naming enabled", ctx.Settings.autoNamingEnabled);

				using (new EditorGUI.DisabledScope(!ctx.Settings.autoNamingEnabled))
				{
					ctx.Settings.levelNameTemplate =
						EditorGUILayout.TextField("Name template", ctx.Settings.levelNameTemplate);

					EditorGUILayout.HelpBox(
						"Template supports {index} or {index:000}. Example: Level_{index:000}",
						MessageType.None);
				}

				EditorGUILayout.Space(6);

				ctx.CreateCount = EditorGUILayout.IntSlider("Create count", ctx.CreateCount, 1, 50);

				ctx.PresetTimeLimitSeconds =
					Mathf.Max(1, EditorGUILayout.IntField("Time Limit Seconds", ctx.PresetTimeLimitSeconds));

				ctx.PresetDifficulty =
					Mathf.Max(0, EditorGUILayout.IntField("Difficulty", ctx.PresetDifficulty));

				ctx.PresetGoalsList?.DoLayoutList();

				if (EditorGUI.EndChangeCheck())
				{
					EditorUtility.SetDirty(ctx.Settings);
					AssetDatabase.SaveAssets();
				}

				if (ctx.CreateCount > 1 && (ctx.Settings == null || !ctx.Settings.autoNamingEnabled))
				{
					EditorGUILayout.HelpBox(
						"Batch create (count > 1) requires Auto naming enabled." +
						"\nOtherwise you will be asked to manually configure naming template for New Levels.",
						MessageType.Warning);
				}
			}
		}
	}
}
#endif