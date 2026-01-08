#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelIssuesView
	{
		public static void DrawSummary(LevelManagementContext ctx)
		{
			int err = ctx.Issues.Count(x => x.Severity == ValidationSeverity.Error);
			int warn = ctx.Issues.Count(x => x.Severity == ValidationSeverity.Warning);

			EditorGUILayout.HelpBox(
				$"Validation: {err} errors, {warn} warnings",
				err > 0 ? MessageType.Error : warn > 0 ? MessageType.Warning : MessageType.Info);
		}

		public static void DrawSingleLevelIssues(LevelManagementContext ctx, LevelConfig lvl)
		{
			if (!lvl) return;

			List<ValidationIssue> lvlIssues = ctx.Issues.Where(x => x.Level == lvl).ToList();

			if (lvlIssues.Count == 0)
				return;

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);

			foreach (ValidationIssue issue in lvlIssues)
			{
				MessageType type =
					issue.Severity == ValidationSeverity.Error ? MessageType.Error :
					issue.Severity == ValidationSeverity.Warning ? MessageType.Warning :
					MessageType.Info;

				EditorGUILayout.HelpBox(issue.Message, type);
			}
		}

		public static void DrawMultiIssues(LevelManagementContext ctx, List<LevelConfig> selected,
			LevelManagementController controller)
		{
			if (selected == null || selected.Count == 0)
				return;

			HashSet<LevelConfig> set = new(selected);
			List<ValidationIssue> selectedIssues = ctx.Issues
				.Where(i => i.Level && set.Contains(i.Level))
				.ToList();

			int err = selectedIssues.Count(i => i.Severity == ValidationSeverity.Error);
			int warn = selectedIssues.Count(i => i.Severity == ValidationSeverity.Warning);

			if (err == 0 && warn == 0)
			{
				EditorGUILayout.HelpBox("No validation issues for the selected levels.", MessageType.Info);
				return;
			}

			EditorGUILayout.HelpBox(
				$"Selected validation: {err} error(s), {warn} warning(s).",
				err > 0 ? MessageType.Error : MessageType.Warning);

			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);

			IOrderedEnumerable<IGrouping<LevelConfig, ValidationIssue>> byLevel = selectedIssues
				.GroupBy(i => i.Level)
				.OrderBy(g => g.Key != null ? g.Key.name : "");

			foreach (IGrouping<LevelConfig, ValidationIssue> g in byLevel)
			{
				LevelConfig lvl = g.Key;
				if (!lvl) continue;

				using (new EditorGUILayout.VerticalScope("box"))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.LabelField(lvl.name, EditorStyles.boldLabel);

						if (GUILayout.Button("Select", GUILayout.Width(70)))
						{
							Selection.activeObject = lvl;
							EditorGUIUtility.PingObject(lvl);
							controller.SetSelected(lvl, true);
						}
					}

					foreach (ValidationIssue issue in g)
					{
						MessageType type =
							issue.Severity == ValidationSeverity.Error ? MessageType.Error :
							issue.Severity == ValidationSeverity.Warning ? MessageType.Warning :
							MessageType.Info;

						EditorGUILayout.HelpBox(issue.Message, type);
					}
				}
			}
		}
	}
}
#endif