#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelInspectorView
	{
		private static readonly HashSet<string> ExcludedAutoProps = new()
		{
			"m_Script",
			"goals",
			"stableId"
		};

		public static void Draw(LevelManagementContext ctx, LevelManagementController controller)
		{
			EditorGUILayout.BeginVertical();

			if (GUILayout.Button(LevelToolSettings.ShowLevelInspector
					? "Hide Level Inspector"
					: "Show Level Inspector"))
			{
				LevelToolSettings.ShowLevelInspector = !LevelToolSettings.ShowLevelInspector;
			}

			if (!LevelToolSettings.ShowLevelInspector)
			{
				EditorGUILayout.EndVertical();
				return;
			}

			ctx.RightScroll = EditorGUILayout.BeginScrollView(ctx.RightScroll);

			List<LevelConfig> selected = controller.GetSelectedLevels();

			DrawInspectorForSelected(ctx, controller, selected);

			if (selected.Count == 1)
				LevelIssuesView.DrawSingleLevelIssues(ctx, selected[0]);
			else
				LevelIssuesView.DrawMultiIssues(ctx, selected, controller);

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		private static void DrawInspectorForSelected(
			LevelManagementContext ctx,
			LevelManagementController controller,
			List<LevelConfig> targets)
		{
			if (targets == null || targets.Count == 0)
			{
				EditorGUILayout.HelpBox("Select one or more levels to edit.", MessageType.Info);
				return;
			}

			bool multi = targets.Count > 1;
			EditorGUILayout.LabelField(
				multi ? $"Editing {targets.Count} levels" : $"Editing {targets[0].name}",
				EditorStyles.boldLabel);

			EditorGUILayout.Space(6);

			SerializedObject so = new(targets.ToArray());
			so.Update();

			if (targets.Count > 1)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button($"Edit Levels… ({targets.Count})", GUILayout.Height(26)))
					{
						EditorApplication.delayCall += () =>
						{
							LevelEditPromptWindow.Show("Edit Levels", targets, onApplied: controller.Validate);
						};
						GUIUtility.ExitGUI();
					}

					if (GUILayout.Button($"Edit Goals… ({targets.Count})", GUILayout.Height(26)))
					{
						EditorApplication.delayCall += () =>
						{
							LevelGoalsPromptWindow.ShowMulti(
								title: "Edit Goals",
								targets: targets,
								onOk: map => controller.ApplyGoalsPerLevel(map),
								onCancel: () => { },
								defaultEditSameForAll: true,
								defaultShowPerLevelGoals: true
							);
						};
						GUIUtility.ExitGUI();
					}

					GUILayout.FlexibleSpace();
				}
			}

			DrawAutoProperties(so);
			DrawGoalsForTargets(so, targets);

			bool changed = so.ApplyModifiedProperties();

			if (!changed)
				return;

			foreach (LevelConfig t in targets)
			{
				EditorUtility.SetDirty(t);
			}

			controller.Validate();
		}

		private static void DrawAutoProperties(SerializedObject so)
		{
			if (so == null)
				return;

			SerializedProperty prop = so.GetIterator();
			bool enterChildren = true;

			while (prop.NextVisible(enterChildren))
			{
				enterChildren = false;

				if (ExcludedAutoProps.Contains(prop.name))
					continue;

				EditorGUILayout.PropertyField(prop, includeChildren: true);
			}
		}

		private static void DrawGoalsForTargets(SerializedObject so, List<LevelConfig> targets)
		{
			if (targets == null || targets.Count == 0 || so == null)
				return;

			SerializedProperty goalsProp = so.FindProperty("goals");

			EditorGUILayout.Space(8);
			EditorGUILayout.LabelField("Goals", EditorStyles.boldLabel);

			if (goalsProp != null)
			{
				EditorGUILayout.PropertyField(goalsProp, includeChildren: true);
			}
			else
			{
				EditorGUILayout.HelpBox("Property 'goals' not found on LevelConfig.", MessageType.Warning);
			}
		}
	}
}
#endif