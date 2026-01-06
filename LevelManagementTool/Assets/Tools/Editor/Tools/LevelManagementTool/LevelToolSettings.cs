#if UNITY_EDITOR
using UnityEditor;

namespace Game.Levels.EditorTool
{
	/// <summary>
	/// Per-user editor settings stored in EditorPrefs (not shared in project).
	/// </summary>
	public static class LevelToolSettings
	{
		private const string KeyPrefix = "Game.Levels.EditorTool.LevelToolSettings.";

		private const string KeyShowWelcomeOnOpen = KeyPrefix + "showWelcomeOnOpen";
		private const string KeyAutoSyncOnOpen    = KeyPrefix + "autoSyncOnOpen";
		private const string KeyAutoNamingEnabled = KeyPrefix + "autoNamingEnabled";
		private const string KeyLevelNameTemplate = KeyPrefix + "levelNameTemplate";

		// Defaults
		public const bool DefaultShowWelcomeOnOpen = true;
		public const bool DefaultAutoSyncOnOpen    = true;
		public const bool DefaultAutoNamingEnabled = true;

		// Template supports {index} or {index:000}
		public const string DefaultLevelNameTemplate = "Level_{index:000}";

		public static bool showWelcomeOnOpen
		{
			get => EditorPrefs.GetBool(KeyShowWelcomeOnOpen, DefaultShowWelcomeOnOpen);
			set => EditorPrefs.SetBool(KeyShowWelcomeOnOpen, value);
		}

		public static bool autoSyncOnOpen
		{
			get => EditorPrefs.GetBool(KeyAutoSyncOnOpen, DefaultAutoSyncOnOpen);
			set => EditorPrefs.SetBool(KeyAutoSyncOnOpen, value);
		}

		public static bool autoNamingEnabled
		{
			get => EditorPrefs.GetBool(KeyAutoNamingEnabled, DefaultAutoNamingEnabled);
			set => EditorPrefs.SetBool(KeyAutoNamingEnabled, value);
		}

		public static string levelNameTemplate
		{
			get => EditorPrefs.GetString(KeyLevelNameTemplate, DefaultLevelNameTemplate);
			set => EditorPrefs.SetString(KeyLevelNameTemplate, string.IsNullOrWhiteSpace(value) ? DefaultLevelNameTemplate : value);
		}

		/// <summary>
		/// Optional helper if you want a "Reset to defaults" button.
		/// </summary>
		public static void ResetToDefaults()
		{
			showWelcomeOnOpen = DefaultShowWelcomeOnOpen;
			autoSyncOnOpen = DefaultAutoSyncOnOpen;
			autoNamingEnabled = DefaultAutoNamingEnabled;
			levelNameTemplate = DefaultLevelNameTemplate;
		}
	}
}
#endif
