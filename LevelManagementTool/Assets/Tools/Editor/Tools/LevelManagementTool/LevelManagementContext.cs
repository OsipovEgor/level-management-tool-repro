#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	[Serializable]
	public class LevelManagementContext
	{
		// data
		public LevelDatabase Database;
		public List<LevelConfig> AllLevels = new();
		public List<LevelConfig> FilteredLevels = new();
		public List<ValidationIssue> Issues = new();

		// UI state
		public string Search = "";
		public Vector2 LeftScroll;
		public Vector2 RightScroll;
		public bool ShowWelcome = true;
		public bool ShowCreateOptions = true;

		// selection
		public int SelectedIndex = -1;
		public readonly HashSet<string> SelectedStableIds = new(StringComparer.OrdinalIgnoreCase);

		// create preset
		public int CreateCount = 1;
		public int PresetTimeLimitSeconds = 120;
		public int PresetDifficulty = 90;
		public List<LevelGoal> PresetGoals = new(3);

		// UI-only list (kept here for convenience; can be moved to Window if you prefer)
		[NonSerialized] public ReorderableList PresetGoalsList;
		[NonSerialized] public ReorderableList DbOrderList;
		[NonSerialized] public LevelDatabase DbOrderListFor; // чтобы понимать, для какой БД построено
		
		public bool IsSelected(LevelConfig lvl) => lvl != null && SelectedStableIds.Contains(lvl.StableId);

		public enum IssuesFilterMode
		{
			All,
			WithIssues,
			ErrorsOnly,
			WarningsOnly
		}

		public IssuesFilterMode IssuesFilter = IssuesFilterMode.All;
	}
}
#endif