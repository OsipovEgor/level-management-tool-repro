#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
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

		private static void DrawInspectorForSelected(LevelManagementContext ctx, LevelManagementController controller, List<LevelConfig> targets)
		{
			if (targets == null || targets.Count == 0)
			{
				EditorGUILayout.HelpBox("Select one or more levels to edit.", MessageType.Info);
				return;
			}

			bool multi = targets.Count > 1;
			EditorGUILayout.LabelField(
				multi ? $"Editing {targets.Count} levels" : "Editing 1 level",
				EditorStyles.boldLabel);

			EditorGUILayout.Space(6);

			SerializedObject so = new(targets.ToArray());
			so.Update();

			DrawAutoProperties(so);
			DrawGoalsForTargets(controller, targets);

			if (!so.ApplyModifiedProperties())
				return;

			foreach (LevelConfig t in targets)
				EditorUtility.SetDirty(t);

			controller.Validate();
		}

		private static void DrawAutoProperties(SerializedObject so)
		{
			if (so == null) return;

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

		private static void DrawGoalsForTargets(LevelManagementController controller, List<LevelConfig> targets)
		{
			if (targets == null || targets.Count == 0) return;

			if (targets.Count == 1)
			{
				DrawLevelGoalsInspector(targets[0]);
				return;
			}

			if (AllGoalsSame(targets))
			{
				List<LevelGoal> temp = new(targets[0].goals ?? new List<LevelGoal>());
				DrawGoalsEditorInline(temp);

				if (GUILayout.Button("Apply Goals To All", GUILayout.Width(160)))
					controller.ApplyGoalsToLevels(targets, temp);
			}
			else
			{
				EditorGUILayout.HelpBox("Multiple Values", MessageType.None);

				if (!GUILayout.Button("Edit Goals & Apply To All", GUILayout.Width(220)))
					return;

				List<LevelGoal> baseGoals = targets[0].goals != null
					? new List<LevelGoal>(targets[0].goals)
					: new List<LevelGoal>(3);

				LevelGoalsPromptWindow.Show(
					title: "Edit Goals",
					initial: baseGoals,
					onOk: newGoals => controller.ApplyGoalsToLevels(targets, newGoals),
					onCancel: () => { }
				);
			}
		}

		private static void DrawLevelGoalsInspector(LevelConfig lvl)
		{
			EditorGUILayout.Space(8);

			SerializedObject so = new(lvl);
			so.Update();

			EditorGUILayout.PropertyField(so.FindProperty("goals"), includeChildren: true);
			so.ApplyModifiedProperties();
		}

		private static bool GoalsEqual(LevelConfig a, LevelConfig b)
		{
			if (a.goals == null && b.goals == null) return true;
			if (a.goals == null || b.goals == null) return false;
			if (a.goals.Count != b.goals.Count) return false;

			for (int i = 0; i < a.goals.Count; i++)
			{
				LevelGoal ga = a.goals[i];
				LevelGoal gb = b.goals[i];

				if (ga.Type != gb.Type) return false;
				if (ga.Target != gb.Target) return false;
				if (!string.Equals(ga.Tag ?? "", gb.Tag ?? "", StringComparison.Ordinal)) return false;
			}

			return true;
		}

		private static bool AllGoalsSame(List<LevelConfig> levels)
		{
			LevelConfig first = levels[0];
			for (int i = 1; i < levels.Count; i++)
				if (!GoalsEqual(first, levels[i]))
					return false;
			return true;
		}

		private static void DrawGoalsEditorInline(List<LevelGoal> goals)
		{
			if (goals == null) return;

			for (int i = 0; i < goals.Count; i++)
			{
				LevelGoal g = goals[i];
				using (new EditorGUILayout.HorizontalScope())
				{
					g.Type = (GoalType)EditorGUILayout.EnumPopup(g.Type, GUILayout.Width(160));
					g.Target = Mathf.Max(1, EditorGUILayout.IntField(g.Target, GUILayout.Width(80)));
					g.Tag = EditorGUILayout.TextField(g.Tag ?? "", GUILayout.ExpandWidth(true));

					if (GUILayout.Button("X", GUILayout.Width(24)))
					{
						goals.RemoveAt(i);
						GUIUtility.ExitGUI();
					}
				}

				goals[i] = g;
			}

			using (new EditorGUI.DisabledScope(goals.Count >= 3))
			{
				if (GUILayout.Button("+ Add Goal", GUILayout.Width(110)))
					goals.Add(new LevelGoal { Type = GoalType.Collect, Target = 1, Tag = "" });
			}
		}
	}
}
#endif
