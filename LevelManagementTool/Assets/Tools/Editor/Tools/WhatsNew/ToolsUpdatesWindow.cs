using UnityEditor;
using UnityEngine;

namespace ProjectName.Editor.ToolsUpdates
{
	public class ToolsUpdatesWindow : EditorWindow
	{
		public static void ShowWindow()
		{
			GetWindow<ToolsUpdatesWindow>("What's New");
		}

		private void OnGUI()
		{
			GUILayout.Label("What's New", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			bool autoOpen = AutoOpenEditorWindow.AutoOpenEnabled;
			bool newValue = EditorGUILayout.Toggle("Open on Unity start", autoOpen);

			if (newValue != autoOpen)
				AutoOpenEditorWindow.AutoOpenEnabled = newValue;

			EditorGUILayout.Space();
		}
	}
}