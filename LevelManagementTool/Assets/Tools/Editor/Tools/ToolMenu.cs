#if UNITY_EDITOR
using ProjectName.Editor.ToolsUpdates;
using UnityEditor;

namespace Game.Levels.EditorTool
{
	public static class ToolMenu
	{
		[MenuItem("Tools/Level Management Tool")]
		public static void Open()
		{
			LevelManagementWindow.ShowWindow();
		}

		[MenuItem("Tools/What's New")]
		public static void OpenWhatsNewWindow()
		{
			WhatsNewWindow.ShowWindow();
		}
	}
}
#endif