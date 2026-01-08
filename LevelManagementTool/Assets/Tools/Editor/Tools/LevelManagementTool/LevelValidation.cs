#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace Game.Levels.EditorTool
{
	public enum ValidationSeverity
	{
		Info,
		Warning,
		Error
	}

	public readonly struct ValidationIssue
	{
		public readonly ValidationSeverity Severity;
		public readonly string Message;
		public readonly LevelConfig Level;

		public ValidationIssue(ValidationSeverity severity, string message, LevelConfig level)
		{
			Severity = severity;
			Message = message;
			Level = level;
		}
	}

	public static class LevelValidation
	{
		public static List<ValidationIssue> ValidateAll(LevelDatabase db, IReadOnlyList<LevelConfig> allLevels)
		{
			List<ValidationIssue> issues = new();

			// Duplicate stable IDs
			IEnumerable<IGrouping<string, LevelConfig>> idGroups = allLevels
				.Where(levelConfig => levelConfig && !string.IsNullOrEmpty(levelConfig.StableId))
				.GroupBy(levelConfig => levelConfig.StableId);

			foreach (IGrouping<string, LevelConfig> g in idGroups)
			{
				if (g.Count() > 1)
					issues.Add(new ValidationIssue(ValidationSeverity.Error, $"Duplicate StableId: {g.Key}",
						g.First()));
			}

			foreach (LevelConfig lvl in allLevels)
			{
				if (!lvl) continue;

				if (lvl.timeLimitSeconds <= 0)
					issues.Add(new ValidationIssue(ValidationSeverity.Error, "Time limit must be > 0", lvl));

				if (lvl.difficulty <= 0)
					issues.Add(new ValidationIssue(ValidationSeverity.Error, "Difficulty must be > 0", lvl));

				if (lvl.goals != null && lvl.goals.Count > 3)
					issues.Add(new ValidationIssue(ValidationSeverity.Warning, "More than 3 goals (will be trimmed)",
						lvl));

				if (lvl.goals == null)
					continue;

				if (lvl.goals.Count == 0)
					issues.Add(new ValidationIssue(ValidationSeverity.Warning, "Goal count is 0 for level ", lvl));

				for (int i = 0; i < lvl.goals.Count; i++)
				{
					if (lvl.goals[i].Target <= 0)
						issues.Add(new ValidationIssue(ValidationSeverity.Error, $"Goal[{i}] target must be > 0",
							lvl));
				}
			}

			// Missing from DB
			if (db == null)
				return issues;

			HashSet<LevelConfig> set = new(db.orderedLevels);
			foreach (LevelConfig lvl in allLevels)
			{
				if (lvl != null && !set.Contains(lvl))
					issues.Add(new ValidationIssue(ValidationSeverity.Warning,
						"Level is not registered in LevelDatabase", lvl));
			}

			return issues;
		}
	}
}
#endif