#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelGoalsEditorGUI
	{
		public const int MaxGoals = 3;

		public static void DrawGoalsEditor(ref List<LevelGoal> goals)
		{
			goals ??= new List<LevelGoal>(MaxGoals);

			bool removed = false;

			for (int i = 0; i < goals.Count; i++)
			{
				LevelGoal goal = goals[i];

				using (new EditorGUILayout.HorizontalScope())
				{
					goal.Type = (GoalType)EditorGUILayout.EnumPopup(goal.Type, GUILayout.Width(160));
					goal.Target = Mathf.Max(0, EditorGUILayout.IntField(goal.Target, GUILayout.Width(80)));
					goal.Tag = EditorGUILayout.TextField(goal.Tag ?? "", GUILayout.ExpandWidth(true));

					if (GUILayout.Button("X", GUILayout.Width(24)))
					{
						goals.RemoveAt(i);
						GUI.changed = true;
						removed = true;
					}
				}

				if (removed)
					break;

				goals[i] = goal;
			}

			using (new EditorGUI.DisabledScope(goals.Count >= MaxGoals))
			{
				if (GUILayout.Button("+ Add Goal", GUILayout.Width(110)))
				{
					goals.Add(NewDefaultGoal());
					GUI.changed = true;
				}
			}

			if (goals.Count > MaxGoals)
				goals.RemoveRange(MaxGoals, goals.Count - MaxGoals);
		}

		private static LevelGoal NewDefaultGoal()
		{
			return new LevelGoal { Type = GoalType.Collect, Target = 1, Tag = "" };
		}
	}
}
#endif