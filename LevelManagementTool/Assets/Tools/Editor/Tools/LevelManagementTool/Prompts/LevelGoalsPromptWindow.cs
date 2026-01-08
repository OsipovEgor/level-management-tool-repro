#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public sealed class LevelGoalsPromptWindow : EditorWindow
	{
		private const int MaxGoals = 3;

		private List<LevelGoal> _singleGoals;
		private Action<List<LevelGoal>> _onOkSingle;
		private Action _onCancelSingle;

		private bool _isMulti;
		private List<LevelConfig> _targets;
		private Dictionary<string, List<LevelGoal>> _goalsByStableId;
		private Action<Dictionary<LevelConfig, List<LevelGoal>>> _onOkMulti;
		private Action _onCancelMulti;

		private bool _editSameForAll = true;
		private List<LevelGoal> _commonGoals;

		private bool _showPerLevelGoals = true;
		private Vector2 _scroll;

		// ---------------- Public API ----------------

		public static void ShowMulti(
			string title,
			IReadOnlyList<LevelConfig> targets,
			Action<Dictionary<LevelConfig, List<LevelGoal>>> onOk,
			Action onCancel,
			bool defaultEditSameForAll = true,
			bool defaultShowPerLevelGoals = true)
		{
			if (targets == null || targets.Count == 0) return;

			var w = CreateInstance<LevelGoalsPromptWindow>();
			w.titleContent = new GUIContent(title);

			w._isMulti = true;
			w._targets = targets.Where(t => t != null).Distinct().ToList();
			w._onOkMulti = onOk;
			w._onCancelMulti = onCancel;

			w._editSameForAll = defaultEditSameForAll;
			w._showPerLevelGoals = defaultShowPerLevelGoals;

			w.BuildGoalsCacheFromTargets();
			w._commonGoals = DeepCopyGoals(w.GetGoalsForTarget(w._targets[0]));

			w.minSize = new Vector2(700, 520);
			w.ShowModalUtility();
		}

		// ---------------- GUI ----------------

		private void OnGUI()
		{
			if (!_isMulti)
			{
				DrawSingleMode();
				return;
			}

			DrawMultiMode();
		}

		private void DrawSingleMode()
		{
			EditorGUILayout.LabelField("Goals (0..3)", EditorStyles.boldLabel);
			EditorGUILayout.Space(6);

			using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
			{
				_scroll = sv.scrollPosition;
				LevelGoalsEditorGUI.DrawGoalsEditor(ref _singleGoals);
			}

			DrawFooterSingle();
		}

		private void DrawMultiMode()
		{
			if (_targets == null || _targets.Count == 0)
			{
				EditorGUILayout.HelpBox("No targets.", MessageType.Info);
				DrawFooterMulti(enabledOk: false);
				return;
			}

			EditorGUILayout.LabelField($"Edit Goals — {_targets.Count} selected level(s)", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);

			using (new EditorGUILayout.HorizontalScope())
			{
				bool newSame = EditorGUILayout.ToggleLeft(
					new GUIContent("Edit same goals for all selected levels", "Edits apply to all levels on OK."),
					_editSameForAll);

				if (newSame != _editSameForAll)
				{
					_editSameForAll = newSame;
					if (_editSameForAll)
						_commonGoals = DeepCopyGoals(GetGoalsForTarget(_targets[0]));
				}

				GUILayout.Space(14);

				_showPerLevelGoals = EditorGUILayout.ToggleLeft(
					new GUIContent("Show per-level goals", "Keeps all levels' goals visible for comparison."),
					_showPerLevelGoals);

				GUILayout.FlexibleSpace();

				bool mixed = !AllGoalsSameAcrossTargets();
				EditorGUILayout.LabelField(mixed ? "Mixed values" : "All same",
					mixed ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel,
					GUILayout.Width(90));
			}

			EditorGUILayout.Space(6);

			using (EditorGUILayout.ScrollViewScope sv = new(_scroll))
			{
				_scroll = sv.scrollPosition;

				if (_editSameForAll)
				{
					EditorGUILayout.HelpBox("Editing common goals. Press OK to apply to all selected levels.",
						MessageType.None);
					LevelGoalsEditorGUI.DrawGoalsEditor(ref _commonGoals);

					if (_showPerLevelGoals)
					{
						EditorGUILayout.Space(10);
						DrawPerLevelReadOnlyTable();
					}
				}
				else
				{
					DrawPerLevelEditableTable();
				}
			}

			DrawFooterMulti(enabledOk: true);
		}

		// ---------------- Per-level Tables ----------------

		private void DrawPerLevelReadOnlyTable()
		{
			EditorGUILayout.LabelField("Current goals per level (read-only)", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);

			foreach (LevelConfig lvl in _targets)
			{
				if (lvl == null)
					continue;

				List<LevelGoal> goals = GetGoalsForTarget(lvl) ?? new List<LevelGoal>(MaxGoals);

				using (new EditorGUILayout.VerticalScope("box"))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.LabelField(lvl.name, EditorStyles.boldLabel);
						if (GUILayout.Button("Ping", GUILayout.Width(50)))
							EditorGUIUtility.PingObject(lvl);
					}

					if (goals.Count == 0)
					{
						EditorGUILayout.LabelField("(empty)", EditorStyles.miniLabel);
						continue;
					}

					using (new EditorGUI.DisabledScope(true))
					{
						foreach (LevelGoal g in goals)
						{
							using (new EditorGUILayout.HorizontalScope())
							{
								EditorGUILayout.EnumPopup(g.Type, GUILayout.Width(160));
								EditorGUILayout.IntField(g.Target, GUILayout.Width(80));
								EditorGUILayout.TextField(g.Tag ?? "", GUILayout.ExpandWidth(true));
							}
						}
					}
				}
			}
		}

		private void DrawPerLevelEditableTable()
		{
			EditorGUILayout.LabelField("Goals per level (editable)", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);

			foreach (LevelConfig lvl in _targets)
			{
				if (lvl == null) continue;

				List<LevelGoal> goals = GetGoalsForTarget(lvl) ?? new List<LevelGoal>(MaxGoals);

				using (new EditorGUILayout.VerticalScope("box"))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.LabelField(lvl.name, EditorStyles.boldLabel);
						if (GUILayout.Button("Ping", GUILayout.Width(50)))
							EditorGUIUtility.PingObject(lvl);
					}

					LevelGoalsEditorGUI.DrawGoalsEditor(ref goals);

					SetGoalsForTarget(lvl, goals);
				}
			}

			EditorGUILayout.Space(6);
			EditorGUILayout.HelpBox(
				"Tip: If you want to apply one set of goals to everyone, enable 'Edit same goals for all selected levels'.",
				MessageType.None);
		}

		// ---------------- Footer ----------------

		private void DrawFooterSingle()
		{
			EditorGUILayout.Space(10);
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Cancel", GUILayout.Width(110)))
				{
					_onCancelSingle?.Invoke();
					Close();
				}

				if (!GUILayout.Button("OK", GUILayout.Width(110)))
					return;

				_onOkSingle?.Invoke(DeepCopyGoals(_singleGoals));
				Close();
			}
		}

		private void DrawFooterMulti(bool enabledOk)
		{
			EditorGUILayout.Space(10);
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Cancel", GUILayout.Width(110)))
				{
					_onCancelMulti?.Invoke();
					Close();
				}

				using (new EditorGUI.DisabledScope(!enabledOk))
				{
					if (!GUILayout.Button("OK", GUILayout.Width(110)))
						return;

					_onOkMulti?.Invoke(BuildResultMapForOk());
					Close();
				}
			}
		}

		// ---------------- Data helpers ----------------

		private void BuildGoalsCacheFromTargets()
		{
			_goalsByStableId = new Dictionary<string, List<LevelGoal>>(StringComparer.OrdinalIgnoreCase);

			foreach (LevelConfig lvl in _targets)
			{
				if (lvl == null)
					continue;

				_goalsByStableId[lvl.StableId] = DeepCopyGoals(lvl.goals);
			}
		}

		private List<LevelGoal> GetGoalsForTarget(LevelConfig lvl)
		{
			if (!lvl)
				return null;

			if (_goalsByStableId != null && _goalsByStableId.TryGetValue(lvl.StableId, out List<LevelGoal> goals))
				return goals;

			return DeepCopyGoals(lvl.goals);
		}

		private void SetGoalsForTarget(LevelConfig lvl, List<LevelGoal> goals)
		{
			if (!lvl)
				return;

			_goalsByStableId ??= new Dictionary<string, List<LevelGoal>>(StringComparer.OrdinalIgnoreCase);
			_goalsByStableId[lvl.StableId] = DeepCopyGoals(goals);
		}

		private Dictionary<LevelConfig, List<LevelGoal>> BuildResultMapForOk()
		{
			Dictionary<LevelConfig, List<LevelGoal>> map = new();

			if (_editSameForAll)
			{
				List<LevelGoal> common = DeepCopyGoals(_commonGoals);

				foreach (LevelConfig lvl in _targets)
				{
					if (lvl != null)
						map[lvl] = DeepCopyGoals(common);
				}

				return map;
			}

			foreach (LevelConfig lvl in _targets)
			{
				if (lvl == null) continue;
				map[lvl] = DeepCopyGoals(GetGoalsForTarget(lvl));
			}

			return map;
		}

		private bool AllGoalsSameAcrossTargets()
		{
			if (_targets == null || _targets.Count <= 1)
				return true;

			List<LevelGoal> first = GetGoalsForTarget(_targets[0]);

			for (int i = 1; i < _targets.Count; i++)
			{
				if (!GoalsEqual(first, GetGoalsForTarget(_targets[i])))
					return false;
			}

			return true;
		}

		private static List<LevelGoal> DeepCopyGoals(List<LevelGoal> src)
		{
			List<LevelGoal> list = new(MaxGoals);
			if (src == null) return list;

			for (int i = 0; i < src.Count && i < MaxGoals; i++)
			{
				LevelGoal goal = src[i];

				list.Add(new LevelGoal
				{
					Type = goal.Type,
					Target = goal.Target,
					Tag = goal.Tag ?? ""
				});
			}

			return list;
		}

		private static bool GoalsEqual(List<LevelGoal> a, List<LevelGoal> b)
		{
			if (a == null && b == null) return true;
			if (a == null || b == null) return false;
			if (a.Count != b.Count) return false;

			for (int i = 0; i < a.Count; i++)
			{
				LevelGoal ga = a[i];
				LevelGoal gb = b[i];
				if (ga.Type != gb.Type) return false;
				if (ga.Target != gb.Target) return false;
				if (!string.Equals(ga.Tag ?? "", gb.Tag ?? "", StringComparison.Ordinal)) return false;
			}

			return true;
		}
	}
}
#endif