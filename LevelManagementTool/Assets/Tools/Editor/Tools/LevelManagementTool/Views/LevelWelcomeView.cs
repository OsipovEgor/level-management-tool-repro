#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelWelcomeView
	{
		public static void Draw(LevelManagementContext ctx, LevelManagementController controller)
		{
			if (!ctx.ShowWelcome)
				return;

			EditorGUILayout.HelpBox(
				"Welcome to Level Management Tool.\n" +
				"Use Sync DB to auto-register levels, edit goals without JSON, and keep Stable IDs save-safe.",
				MessageType.Info);

			if (GUILayout.Button("Hide Welcome"))
				controller.HideWelcome();
		}
	}
}
#endif