#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelTopBarView
	{
		public static void Draw(LevelManagementContext ctx, LevelManagementController controller)
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			ctx.Database = (LevelDatabase)EditorGUILayout.ObjectField(
				ctx.Database, typeof(LevelDatabase), false, GUILayout.Width(320));

			GUILayout.Space(8);

			using (new EditorGUI.DisabledScope(ctx.Database == null))
			{
				string newSearch = EditorGUILayout.TextField(
					ctx.Search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(200));

				if (!string.Equals(newSearch, ctx.Search, StringComparison.Ordinal))
				{
					ctx.Search = newSearch;
					controller.ApplyFilter();
				}

				if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
				{
					controller.RefreshLevels();
					controller.Validate();
				}

				if (GUILayout.Button("Sync DB", EditorStyles.toolbarButton, GUILayout.Width(70)))
					controller.SyncDb();

				if (GUILayout.Button("Export ID Map", EditorStyles.toolbarButton, GUILayout.Width(100)))
					controller.ExportIdMap();

				if (GUILayout.Button("Create Level", EditorStyles.toolbarButton, GUILayout.Width(90)))
				{
					EditorApplication.delayCall += controller.CreateLevelsClicked;
					GUIUtility.ExitGUI();
				}

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70)))
					controller.Validate();
			}

			EditorGUILayout.EndHorizontal();
		}
	}
}
#endif