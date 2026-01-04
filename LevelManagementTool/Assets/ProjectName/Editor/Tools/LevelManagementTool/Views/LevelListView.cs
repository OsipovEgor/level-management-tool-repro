#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelListView
	{
		public static void Draw(LevelManagementContext ctx, LevelManagementController controller, System.Action repaint)
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(420));

			DrawHeader(ctx, controller);
			ctx.LeftScroll = EditorGUILayout.BeginScrollView(ctx.LeftScroll);
			DrawRows(ctx, controller);
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(6);
			LevelIssuesView.DrawSummary(ctx);

			EditorGUILayout.EndVertical();
		}

		private static void DrawHeader(LevelManagementContext ctx, LevelManagementController controller)
		{
			bool any = ctx.FilteredLevels.Count > 0;
			int selectedCount = ctx.FilteredLevels.Count(l => ctx.IsSelected(l));

			bool allSelected = any && selectedCount == ctx.FilteredLevels.Count;
			bool noneSelected = selectedCount == 0;

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				bool newAll = GUILayout.Toggle(allSelected, GUIContent.none, EditorStyles.toolbarButton, GUILayout.Width(24));
				if (any && newAll != allSelected)
				{
					foreach (LevelConfig lvl in ctx.FilteredLevels)
						controller.SetSelected(lvl, newAll);
				}

				GUILayout.Label("Level Name", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
				GUILayout.Label("Duplicate", EditorStyles.miniLabel, GUILayout.Width(80));

				using (new EditorGUI.DisabledScope(noneSelected))
				{
					string text = noneSelected ? "Delete" : $"Delete All ({selectedCount})";
					if (GUILayout.Button(text, EditorStyles.toolbarButton, GUILayout.Width(110)))
					{
						List<LevelConfig> selected = controller.GetSelectedLevels();
						EditorApplication.delayCall += () => controller.DeleteLevelsBatch(selected);
						GUIUtility.ExitGUI();
					}
				}
			}
		}

		private static void DrawRows(LevelManagementContext ctx, LevelManagementController controller)
		{
			foreach (LevelConfig lvl in ctx.FilteredLevels)
			{
				if (lvl == null) continue;

				bool isSel = ctx.IsSelected(lvl);

				bool hasError = ctx.Issues.Any(x => x.Level == lvl && x.Severity == ValidationSeverity.Error);
				bool hasWarn = ctx.Issues.Any(x => x.Level == lvl && x.Severity == ValidationSeverity.Warning);

				string label = lvl.name + (hasError ? "  ❌" : hasWarn ? "  ⚠️" : "");

				using (new EditorGUILayout.HorizontalScope())
				{
					bool newSel = GUILayout.Toggle(isSel, GUIContent.none, GUILayout.Width(18));
					if (newSel != isSel) controller.SetSelected(lvl, newSel);

					if (GUILayout.Button(label, "Button", GUILayout.ExpandWidth(true)))
						controller.SetSelected(lvl, true);

					if (GUILayout.Button("Duplicate", GUILayout.Width(80)))
					{
						LevelConfig src = lvl;
						EditorApplication.delayCall += () => controller.DuplicateLevel(src);
						GUIUtility.ExitGUI();
					}

					if (GUILayout.Button("Delete", GUILayout.Width(60)))
					{
						LevelConfig toDelete = lvl;
						EditorApplication.delayCall += () => controller.DeleteLevelsBatch(new List<LevelConfig> { toDelete });
						GUIUtility.ExitGUI();
					}
				}
			}
		}
	}
}
#endif
