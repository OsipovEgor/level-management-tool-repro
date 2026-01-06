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

			// Use visible rows based on current search + issue filter
			List<LevelConfig> visible;

			if (canReorder)
			{
				// В reorder-режиме мы показываем ТОЛЬКО DB-список (как "истина"),
				// без сирот и без фильтрации — иначе reorder становится непредсказуемым.
				visible = ctx.Database.orderedLevels.Where(l => l != null).ToList();
			}
			else
			{
				// В фильтр-режиме используем уже подготовленный список (DB order + orphans, плюс search),
				// и добавляем issue filter поверх.
				visible = ctx.FilteredLevels
					.Where(l => l != null)
					.Where(l => PassesIssueFilter(ctx, l))
					.ToList();
			}

			DrawHeader(ctx, controller, visible, repaint);
			DrawTableHeader(ctx);

			ctx.LeftScroll = EditorGUILayout.BeginScrollView(ctx.LeftScroll);

			if (canReorder)
				DrawDbReorderable(ctx);
			else
				DrawRows(ctx, controller, repaint, visible);

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
						controller.SetSelected(lvl, newAll);
				}

				GUILayout.FlexibleSpace();

				// Issue filter dropdown (readable & compact)
				EditorGUI.BeginChangeCheck();
				ctx.IssuesFilter = (LevelManagementContext.IssuesFilterMode)EditorGUILayout.EnumPopup(
					ctx.IssuesFilter, EditorStyles.toolbarPopup, GUILayout.Width(120));
				if (EditorGUI.EndChangeCheck())
					repaint?.Invoke();

				GUILayout.Space(6);

				using (new EditorGUI.DisabledScope(noneSelected))
				{
					string text = noneSelected ? "Delete" : $"Delete ({selectedCount})";
					if (GUILayout.Button(text, EditorStyles.toolbarButton, GUILayout.Width(100)))
					{
						var selected = controller.GetSelectedLevels();
						EditorApplication.delayCall += () => controller.DeleteLevelsBatch(selected);
						GUIUtility.ExitGUI();
					}
				}
			}
		}

		private static void DrawTableHeader(LevelManagementContext ctx)
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
				if (lvl == null) continue;

				bool isSel = ctx.IsSelected(lvl);

				bool hasError =
					ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Error);
				bool hasWarn =
					ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Warning);


				string fullGoalsSummary = BuildGoalsSummary(lvl.goals);
				string shortGoalsSummary = Ellipsize(fullGoalsSummary, GoalsSummaryMaxChars);

				EditorGUILayout.BeginHorizontal();

				// Selected row background (subtle)
				Rect rowRect =
					GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
				rowRect.height = EditorGUIUtility.singleLineHeight + 4;
				rowRect.y -= 2;

				if (isSel)
					EditorGUI.DrawRect(rowRect, new Color(0.22f, 0.45f, 0.85f, 0.12f));

				// Now actually draw row content over that rect:
				GUILayout.Space(0); // keep layout stable

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

				// Goals summary with tooltip
				GUIContent goalsContent = new GUIContent(shortGoalsSummary, fullGoalsSummary);
				GUILayout.Label(goalsContent, EditorStyles.miniLabel, GUILayout.Width(ColGoals));

				string issuesTxt = hasError ? "ERROR" : hasWarn ? "WARNING" : "";
				GUILayout.Label(issuesTxt, GUILayout.Width(ColIssues));

				EditorGUILayout.EndHorizontal();

				// Click behavior: plain click selects single; Ctrl/Cmd toggles; Shift range select
				// NO modal opening on click.
				Rect last = GUILayoutUtility.GetLastRect();
				Rect clickRect = last;
				clickRect.xMin += ColSel;

				if (e.type == EventType.MouseDown && e.button == 0 && clickRect.Contains(e.mousePosition))
				{
					bool ctrl = e.control || e.command;
					bool shift = e.shift;

					if (!ctrl && !shift)
					{
						ClearSelection(ctx, controller);
						controller.SetSelected(lvl, true);
						ctx.SelectedIndex = i;
						repaint?.Invoke();
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

						repaint?.Invoke();
					}
					else // ctrl/cmd
					{
						controller.SetSelected(lvl, !ctx.IsSelected(lvl));
						ctx.SelectedIndex = i;
						repaint?.Invoke();
					}

					e.Use();
				}
			}
		}

		private static bool IssueMatchesLevel(ValidationIssue issue, LevelConfig lvl)
		{
			if (issue.Level == null || lvl == null) return false;
			if (string.IsNullOrEmpty(issue.Level.StableId) || string.IsNullOrEmpty(lvl.StableId)) return false;
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

				if (GUILayout.Button("Clear Selection", GUILayout.Width(120), GUILayout.Height(26)))
				{
					ClearSelection(ctx, controller);
					repaint?.Invoke();
				}
			}
		}

		private static void ClearSelection(LevelManagementContext ctx, LevelManagementController controller)
		{
			List<LevelConfig> selected = controller.GetSelectedLevels();
			foreach (LevelConfig lvl in selected)
				controller.SetSelected(lvl, false);

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
			if (goals == null || goals.Count == 0) return "(empty)";

			return string.Join("; ", goals.Select(g =>
			{
				string tag = string.IsNullOrWhiteSpace(g.Tag) ? "" : $" {g.Tag}";
				return $"{g.Type} {g.Target}{tag}";
			}));
		}

		private static string Ellipsize(string s, int maxChars)
		{
			if (string.IsNullOrEmpty(s) || maxChars <= 0) return "";
			if (s.Length <= maxChars) return s;
			if (maxChars <= 1) return "…";
			return s.Substring(0, maxChars - 1) + "…";
		}

		private static void DrawDbReorderable(LevelManagementContext ctx)
		{
			// У ReorderableList внутри свой layout. Мы отключили header/add/remove при создании.
			// Важно: таблиц-хедер у нас уже нарисован сверху.
			ctx.DbOrderList.DoLayoutList();

			// Подсказка пользователю, почему сейчас "перетаскивается"
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
			if (ctx.Database == null)
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
			);

			ctx.DbOrderList.elementHeight = EditorGUIUtility.singleLineHeight + 6;

			ctx.DbOrderList.onReorderCallback = _ =>
			{
				Undo.RecordObject(ctx.Database, "Reorder Levels");
				EditorUtility.SetDirty(ctx.Database);
				AssetDatabase.SaveAssets();

				controller.ApplyFilter();
				controller.Validate();
				repaint?.Invoke();
			};

			ctx.DbOrderList.drawElementCallback = (rect, index, isActive, isFocused) =>
			{
				if (index < 0 || index >= ctx.Database.orderedLevels.Count) return;
				var lvl = ctx.Database.orderedLevels[index];
				DrawDbRow(ctx, controller, repaint, rect, lvl, index);
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

			// фон выделения
			if (isSel)
				EditorGUI.DrawRect(
					new Rect(rect.x, rect.y - 1, rect.width, rect.height + 2),
					new Color(0.22f, 0.45f, 0.85f, 0.12f));

			// checkbox
			var rSel = new Rect(rect.x, rect.y, ColSel, rect.height);
			bool newSel = GUI.Toggle(rSel, isSel, GUIContent.none);
			if (lvl != null && newSel != isSel)
			{
				controller.SetSelected(lvl, newSel);
				ctx.SelectedIndex = index;
				repaint?.Invoke();
			}

			// columns
			var rName = new Rect(rSel.xMax, rect.y, ColName, rect.height);
			var rTime = new Rect(rName.xMax, rect.y, ColTime, rect.height);
			var rDiff = new Rect(rTime.xMax, rect.y, ColDiff, rect.height);
			var rGoals = new Rect(rDiff.xMax, rect.y, ColGoals, rect.height);
			var rIssues = new Rect(rGoals.xMax, rect.y, ColIssues, rect.height);

			EditorGUI.LabelField(rName, lvl ? lvl.name : "(missing)");
			EditorGUI.LabelField(rTime, lvl ? lvl.timeLimitSeconds.ToString() : "-");
			EditorGUI.LabelField(rDiff, lvl ? lvl.difficulty.ToString() : "-");

			string full = lvl ? BuildGoalsSummary(lvl.goals) : "";
			string shortTxt = Ellipsize(full, GoalsSummaryMaxChars);
			EditorGUI.LabelField(rGoals, new GUIContent(shortTxt, full), EditorStyles.miniLabel);

			bool hasError = lvl &&
							ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Error);
			bool hasWarn = lvl &&
						   ctx.Issues.Any(x => IssueMatchesLevel(x, lvl) && x.Severity == ValidationSeverity.Warning);

			GUIContent ic = GUIContent.none;
			if (hasError) ic = EditorGUIUtility.IconContent("console.erroricon");
			else if (hasWarn) ic = EditorGUIUtility.IconContent("console.warnicon");

			EditorGUI.LabelField(rIssues, ic);

			// Клик по строке (кроме чекбокса): только selection, без модалки
			var clickRect = new Rect(rect.x + ColSel, rect.y - 2, rect.width - ColSel, rect.height + 4);
			var e = Event.current;
			if (lvl != null && e.type == EventType.MouseDown && e.button == 0 && clickRect.Contains(e.mousePosition))
			{
				bool ctrl = e.control || e.command;
				bool shift = e.shift;

				if (!ctrl && !shift)
				{
					ClearSelection(ctx, controller);
					controller.SetSelected(lvl, true);
					ctx.SelectedIndex = index;
					repaint?.Invoke();
				}
				else if (ctrl)
				{
					controller.SetSelected(lvl, !ctx.IsSelected(lvl));
					ctx.SelectedIndex = index;
					repaint?.Invoke();
				}

				e.Use();
			}
		}
	}
}
#endif