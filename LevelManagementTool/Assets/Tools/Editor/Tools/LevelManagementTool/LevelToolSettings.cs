#if UNITY_EDITOR
using UnityEditor;

namespace Game.Levels.EditorTool
{
	public static class LevelToolSettings
	{
		private const string KeyPrefix = "Game.Levels.EditorTool.LevelToolSettings.";

		private const string KeyShowWelcomeOnOpen = KeyPrefix + "showWelcomeOnOpen";
		private const string KeyAutoSyncOnOpen = KeyPrefix + "autoSyncOnOpen";
		private const string KeyAutoNamingEnabled = KeyPrefix + "autoNamingEnabled";
		private const string KeyLevelNameTemplate = KeyPrefix + "levelNameTemplate";
		private const string KeyShowLevelInspector = KeyPrefix + "showLevelInspector";

		// Defaults
		private const bool DefaultShowWelcomeOnOpen = true;
		private const bool DefaultAutoSyncOnOpen = true;
		private const bool DefaultAutoNamingEnabled = true;

		// Template supports {index} or {index:000}
		private const string DefaultLevelNameTemplate = "Level_{index:000}";
		private const bool DefaultShowLevelInspector = false;

		public static bool ShowWelcomeOnOpen
		{
			get => EditorPrefs.GetBool(KeyShowWelcomeOnOpen, DefaultShowWelcomeOnOpen);
			set => EditorPrefs.SetBool(KeyShowWelcomeOnOpen, value);
		}

		public static bool AutoSyncOnOpen
		{
			get => EditorPrefs.GetBool(KeyAutoSyncOnOpen, DefaultAutoSyncOnOpen);
			set => EditorPrefs.SetBool(KeyAutoSyncOnOpen, value);
		}

		public static bool AutoNamingEnabled
		{
			get => EditorPrefs.GetBool(KeyAutoNamingEnabled, DefaultAutoNamingEnabled);
			set => EditorPrefs.SetBool(KeyAutoNamingEnabled, value);
		}

		public static string LevelNameTemplate
		{
			get => EditorPrefs.GetString(KeyLevelNameTemplate, DefaultLevelNameTemplate);
			set => EditorPrefs.SetString(KeyLevelNameTemplate,
				string.IsNullOrWhiteSpace(value) ? DefaultLevelNameTemplate : value);
		}

		public static bool ShowLevelInspector
		{
			get => EditorPrefs.GetBool(KeyShowLevelInspector, DefaultShowLevelInspector);
			set => EditorPrefs.SetBool(KeyShowLevelInspector, value);
		}

		public static void ResetToDefaults()
		{
			ShowWelcomeOnOpen = DefaultShowWelcomeOnOpen;
			AutoSyncOnOpen = DefaultAutoSyncOnOpen;
			AutoNamingEnabled = DefaultAutoNamingEnabled;
			LevelNameTemplate = DefaultLevelNameTemplate;
			ShowLevelInspector = DefaultShowLevelInspector;
		}
	}
}
#endif