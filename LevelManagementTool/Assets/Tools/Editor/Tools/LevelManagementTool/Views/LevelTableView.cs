#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelTableView
	{
		private const float ColSel = 22f;
		private const float ColName = 150f;
		private const float ColTime = 70f;
		private const float ColDiff = 70f;
		private const float ColGoals = 300f;
		private const float ColIssues = 60f;

		private const int GoalsSummaryMaxChars = 68;

		public static void Draw(LevelManagementContext ctx, LevelManagementController controller, Action repaint)
		{
			bool canReorder =
				string.IsNullOrWhiteSpace(ctx.Search) &&
				ctx.IssuesFilter == LevelManagementContext.IssuesFilterMode.All &&
				ctx.DbOrderList != null &&
				ctx.Database != null &&
				ctx.Database.orderedLevels != null;

			EditorGUILayout.BeginVertical(GUILayout.Width(420));

			List<LevelConfig> visible;

			if (canReorder)
			{
				visible = ctx.Database.orderedLevels.Where(levelConfig => levelConfig).ToList();
			}
			else
			{
				visible = ctx.FilteredLevels
					.Where(levelConfig => levelConfig)
					.Where(levelConfig => PassesIssueFilter(ctx, levelConfig))
					.ToList();
			}

			DrawHeader(ctx, controller, visible, repaint);
			DrawTableHeader();

			ctx.LeftScroll = EditorGUILayout.BeginScrollView(ctx.LeftScroll);

			if (canReorder)
			{
				DrawDbReorderable(ctx);
			}
			else
			{
				DrawRows(ctx, controller, repaint, visible);
			}

			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(6);
			DrawBottomActions(ctx, controller, repaint);

			EditorGUILayout.Space(6);
			LevelIssuesView.DrawSummary(ctx);

			EditorGUILayout.EndVertical();
		}

		private static void DrawHeader(LevelManagementContext ctx,
			LevelManagementController controller,
			List<LevelConfig> visible,
			Action repaint)
		{
			bool any = visible.Count > 0;
			int selectedCount = visible.Count(l => ctx.IsSelected(l));

			bool allSelected = any && selectedCount == visible.Count;
			bool noneSelected = selectedCount == 0;

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				bool newAll = GUILayout.Toggle(allSelected, GUIContent.none, EditorStyles.toolbarButton,
					GUILayout.Width(24));

				if (any && newAll != allSelected)
				{
					foreach (LevelConfig lvl in visible)
					{
						controller.SetSelected(lvl, newAll);
					}
				}

				GUILayout.FlexibleSpace();

				EditorGUI.BeginChangeCheck();
				ctx.IssuesFilter = (LevelManagementContext.IssuesFilterMode)EditorGUILayout.EnumPopup(
					ctx.IssuesFilter, EditorStyles.toolbarPopup, GUILayout.Width(120));
				if (EditorGUI.EndChangeCheck())
					repaint?.Invoke();

				GUILayout.Space(6);

				using (new EditorGUI.DisabledScope(noneSelected))
				{
					string text = noneSelected ? "Delete" : $"Delete ({selectedCount})";

					if (!GUILayout.Button(text, EditorStyles.toolbarButton, GUILayout.Width(100)))
						return;

					List<LevelConfig> selected = controller.GetSelectedLevels();
					EditorApplication.delayCall += () => controller.DeleteLevelsBatch(selected);
					GUIUtility.ExitGUI();
				}
			}
		}

		private static void DrawTableHeader()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
			{
				GUILayout.Label("", GUILayout.Width(ColSel));
				GUILayout.Label("Name", EditorStyles.miniBoldLabel, GUILayout.Width(ColName));
				GUILayout.Label("Time", EditorStyles.miniBoldLabel, GUILayout.Width(ColTime));
				GUILayout.Label("Difficulty", EditorStyles.miniBoldLabel, GUILayout.Width(ColDiff));
				GUILayout.Label("Goals", EditorStyles.miniBoldLabel, GUILayout.Width(ColGoals));
				GUILayout.Label("Issues", EditorStyles.miniBoldLabel, GUILayout.Width(ColIssues));
			}
		}

		private static void DrawRows(LevelManagementContext ctx, LevelManagementController controller, Action repaint,
			List<LevelConfig> visible)
		{
			Event e = Event.current;

			for (int i = 0; i < visible.Count; i++)
			{
				LevelConfig lvl = visible[i];

				if (!lvl)
					continue;

				bool isSel = ctx.IsSelected(lvl);

				bool hasError =
					ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Error);
				bool hasWarn =
					ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Warning);

				string fullGoalsSummary = BuildGoalsSummary(lvl.goals);
				string shortGoalsSummary = Ellipsize(fullGoalsSummary, GoalsSummaryMaxChars);

				EditorGUILayout.BeginHorizontal();

				Rect rowRect =
					GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
				rowRect.height = EditorGUIUtility.singleLineHeight + 4;
				rowRect.y -= 2;

				if (isSel)
					EditorGUI.DrawRect(rowRect, new Color(0.22f, 0.45f, 0.85f, 0.12f));

				GUILayout.Space(0);

				bool newSel = GUILayout.Toggle(isSel, GUIContent.none, GUILayout.Width(ColSel));
				if (newSel != isSel)
				{
					controller.SetSelected(lvl, newSel);
					ctx.SelectedIndex = i;
					repaint?.Invoke();
				}

				GUILayout.Label(lvl.name, GUILayout.Width(ColName));
				GUILayout.Label(lvl.timeLimitSeconds.ToString(), GUILayout.Width(ColTime));
				GUILayout.Label(lvl.difficulty.ToString(), GUILayout.Width(ColDiff));

				GUIContent goalsContent = new(shortGoalsSummary, fullGoalsSummary);
				GUILayout.Label(goalsContent, EditorStyles.miniLabel, GUILayout.Width(ColGoals));

				string issuesTxt = hasError ? "ERROR" : hasWarn ? "WARNING" : "";
				GUILayout.Label(issuesTxt, GUILayout.Width(ColIssues));

				EditorGUILayout.EndHorizontal();

				Rect last = GUILayoutUtility.GetLastRect();
				Rect clickRect = last;
				clickRect.xMin += ColSel;

				if (e.type != EventType.MouseDown || e.button != 0 || !clickRect.Contains(e.mousePosition))
					continue;

				bool ctrl = e.control || e.command;
				bool shift = e.shift;

				if (!ctrl && !shift)
				{
					ClearSelection(ctx, controller);
					controller.SetSelected(lvl, true);
					ctx.SelectedIndex = i;
				}
				else if (shift)
				{
					int anchor = ctx.SelectedIndex >= 0 ? ctx.SelectedIndex : i;
					int min = Mathf.Min(anchor, i);
					int max = Mathf.Max(anchor, i);

					for (int k = min; k <= max; k++)
					{
						LevelConfig lv2 = visible[k];
						if (lv2 != null) controller.SetSelected(lv2, true);
					}
				}
				else // ctrl/cmd
				{
					controller.SetSelected(lvl, !ctx.IsSelected(lvl));
					ctx.SelectedIndex = i;
				}

				repaint?.Invoke();

				e.Use();
			}
		}

		private static bool IssueMatchesLevel(ValidationIssue issue, LevelConfig lvl)
		{
			if (!issue.Level || !lvl)
				return false;

			if (string.IsNullOrEmpty(issue.Level.StableId) || string.IsNullOrEmpty(lvl.StableId))
				return false;

			return string.Equals(issue.Level.StableId, lvl.StableId, StringComparison.OrdinalIgnoreCase);
		}

		private static void DrawBottomActions(LevelManagementContext ctx, LevelManagementController controller,
			Action repaint)
		{
			List<LevelConfig> selected = controller.GetSelectedLevels();
			int count = selected.Count;

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUI.DisabledScope(count == 0))
				{
					if (GUILayout.Button($"Edit Levels… ({count})", GUILayout.Height(26)))
					{
						EditorApplication.delayCall += () =>
						{
							LevelEditPromptWindow.Show("Edit Levels", selected, onApplied: controller.Validate);
						};
						GUIUtility.ExitGUI();
					}

					if (GUILayout.Button($"Edit Goals… ({count})", GUILayout.Height(26)))
					{
						EditorApplication.delayCall += () =>
						{
							LevelGoalsPromptWindow.ShowMulti(
								title: "Edit Goals",
								targets: selected,
								onOk: map => controller.ApplyGoalsPerLevel(map),
								onCancel: () => { },
								defaultEditSameForAll: true,
								defaultShowPerLevelGoals: true
							);
						};
						GUIUtility.ExitGUI();
					}
				}

				GUILayout.FlexibleSpace();

				if (!GUILayout.Button("Clear Selection", GUILayout.Width(120), GUILayout.Height(26)))
					return;

				ClearSelection(ctx, controller);
				repaint?.Invoke();
			}
		}

		private static void ClearSelection(LevelManagementContext ctx, LevelManagementController controller)
		{
			List<LevelConfig> selected = controller.GetSelectedLevels();

			foreach (LevelConfig lvl in selected)
			{
				controller.SetSelected(lvl, false);
			}

			ctx.SelectedIndex = -1;
		}

		private static bool PassesIssueFilter(LevelManagementContext ctx, LevelConfig lvl)
		{
			if (ctx.IssuesFilter == LevelManagementContext.IssuesFilterMode.All)
				return true;

			bool hasError = ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Error);
			bool hasWarn = ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Warning);

			return ctx.IssuesFilter switch
			{
				LevelManagementContext.IssuesFilterMode.WithIssues => hasError || hasWarn,
				LevelManagementContext.IssuesFilterMode.ErrorsOnly => hasError,
				LevelManagementContext.IssuesFilterMode.WarningsOnly => hasWarn,
				_ => true
			};
		}

		private static string BuildGoalsSummary(List<LevelGoal> goals)
		{
			if (goals == null || goals.Count == 0)
				return "(empty)";

			return string.Join("; ", goals.Select(g =>
			{
				string tag = string.IsNullOrWhiteSpace(g.Tag) ? "" : $" {g.Tag}";
				return $"{g.Type} {g.Target}{tag}";
			}));
		}

		private static string Ellipsize(string s, int maxChars)
		{
			if (string.IsNullOrEmpty(s) || maxChars <= 0)
				return "";

			if (s.Length <= maxChars)
				return s;

			if (maxChars <= 1)
				return "…";

			return s.Substring(0, maxChars - 1) + "…";
		}

		private static void DrawDbReorderable(LevelManagementContext ctx)
		{
			ctx.DbOrderList.DoLayoutList();

			EditorGUILayout.Space(4);
			EditorGUILayout.HelpBox(
				"Reorder is available only when Search is empty and Issues Filter = All.",
				MessageType.None);
		}

		public static void EnsureDbReorderableList(
			LevelManagementContext ctx,
			LevelManagementController controller,
			Action repaint)
		{
			if (!ctx.Database)
			{
				ctx.DbOrderList = null;
				ctx.DbOrderListFor = null;
				return;
			}

			if (ctx.DbOrderList != null && ctx.DbOrderListFor == ctx.Database)
				return;

			ctx.Database.orderedLevels ??= new List<LevelConfig>();
			ctx.DbOrderListFor = ctx.Database;

			ctx.DbOrderList = new ReorderableList(
				ctx.Database.orderedLevels,
				typeof(LevelConfig),
				draggable: true,
				displayHeader: false,
				displayAddButton: false,
				displayRemoveButton: false
			)
			{
				elementHeight = EditorGUIUtility.singleLineHeight + 6,
				onReorderCallback = _ =>
				{
					Undo.RecordObject(ctx.Database, "Reorder Levels");
					EditorUtility.SetDirty(ctx.Database);
					AssetDatabase.SaveAssets();

					controller.ApplyFilter();
					controller.Validate();
					repaint?.Invoke();
				},
				drawElementCallback = (rect, index, _, _) =>
				{
					if (index < 0 || index >= ctx.Database.orderedLevels.Count)
						return;

					LevelConfig lvl = ctx.Database.orderedLevels[index];
					DrawDbRow(ctx, controller, repaint, rect, lvl, index);
				}
			};
		}

		private static void DrawDbRow(
			LevelManagementContext ctx,
			LevelManagementController controller,
			Action repaint,
			Rect rect,
			LevelConfig lvl,
			int index)
		{
			rect.y += 2;
			rect.height = EditorGUIUtility.singleLineHeight;

			bool isSel = (lvl != null) && ctx.IsSelected(lvl);

			// selection bg
			if (isSel)
			{
				EditorGUI.DrawRect(
					new Rect(rect.x, rect.y - 1, rect.width, rect.height + 2),
					new Color(0.22f, 0.45f, 0.85f, 0.12f));
			}

			// checkbox
			Rect rSel = new(rect.x, rect.y, ColSel, rect.height);
			bool newSel = GUI.Toggle(rSel, isSel, GUIContent.none);
			if (lvl != null && newSel != isSel)
			{
				controller.SetSelected(lvl, newSel);
				ctx.SelectedIndex = index;
				repaint?.Invoke();
			}

			// columns
			Rect rName = new(rSel.xMax, rect.y, ColName, rect.height);
			Rect rTime = new(rName.xMax, rect.y, ColTime, rect.height);
			Rect rDiff = new(rTime.xMax, rect.y, ColDiff, rect.height);
			Rect rGoals = new(rDiff.xMax, rect.y, ColGoals, rect.height);
			Rect rIssues = new(rGoals.xMax, rect.y, ColIssues, rect.height);

			EditorGUI.LabelField(rName, lvl ? lvl.name : "(missing)");
			EditorGUI.LabelField(rTime, lvl ? lvl.timeLimitSeconds.ToString() : "-");
			EditorGUI.LabelField(rDiff, lvl ? lvl.difficulty.ToString() : "-");

			string full = lvl
				? BuildGoalsSummary(lvl.goals)
				: "";

			string shortTxt = Ellipsize(full, GoalsSummaryMaxChars);
			EditorGUI.LabelField(rGoals, new GUIContent(shortTxt, full), EditorStyles.miniLabel);

			bool hasError = lvl &&
							ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Error);
			bool hasWarn = lvl &&
						   ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Warning);

			GUIContent ic = GUIContent.none;

			if (hasError)
			{
				ic = EditorGUIUtility.IconContent("console.erroricon");
			}
			else if (hasWarn)
			{
				ic = EditorGUIUtility.IconContent("console.warnicon");
			}

			EditorGUI.LabelField(rIssues, ic);

			Rect clickRect = new(rect.x + ColSel, rect.y - 2, rect.width - ColSel, rect.height + 4);
			Event e = Event.current;

			if (lvl == null || e.type != EventType.MouseDown || e.button != 0 ||
				!clickRect.Contains(e.mousePosition))
				return;

			controller.SetSelected(lvl, !ctx.IsSelected(lvl));
			ctx.SelectedIndex = index;
			repaint?.Invoke();

			e.Use();
		}
	}
}
#endif