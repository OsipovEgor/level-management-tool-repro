using UnityEditor;

namespace ProjectName.Editor.ToolsUpdates
{
	[InitializeOnLoad]
	public static class AutoOpenEditorWindow
	{
		private const string AutoOpenKey = "LevelManagementTool_AutoOpenOnStartup";
		private const string OpenedThisSessionKey = "LevelManagementTool_OpenedThisSession";

		static AutoOpenEditorWindow()
		{
			EditorApplication.delayCall += TryOpenWindowOncePerSession;
		}

		private static void TryOpenWindowOncePerSession()
		{
			if (SessionState.GetBool(OpenedThisSessionKey, false))
				return;

			SessionState.SetBool(OpenedThisSessionKey, true);

			if (EditorPrefs.GetBool(AutoOpenKey, true))
			{
				WhatsNewWindow.ShowWindow();
			}
		}

		public static bool AutoOpenEnabled
		{
			get => EditorPrefs.GetBool(AutoOpenKey, true);
			set => EditorPrefs.SetBool(AutoOpenKey, value);
		}
	}
}