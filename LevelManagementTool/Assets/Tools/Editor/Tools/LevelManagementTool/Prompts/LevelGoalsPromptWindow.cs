#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Levels;

namespace Game.Levels.EditorTool
{
	public sealed class LevelGoalsPromptWindow : EditorWindow
	{
		private const int MaxGoals = 3;

		// -------- Single mode (старое поведение) --------
		private List<LevelGoal> _singleGoals;
		private Action<List<LevelGoal>> _onOkSingle;
		private Action _onCancelSingle;

		// -------- Multi mode (новое) --------
		private bool _isMulti;
		private List<LevelConfig> _targets;
		private Dictionary<string, List<LevelGoal>> _goalsByStableId; // editable copies per level
		private Action<Dictionary<LevelConfig, List<LevelGoal>>> _onOkMulti;
		private Action _onCancelMulti;

		private bool _editSameForAll = true;
		private List<LevelGoal> _commonGoals;

		private bool _showPerLevelGoals = true; // “галочка держать перед глазами”
		private Vector2 _scroll;

		// ---------------- Public API ----------------

		public static void Show(string title, List<LevelGoal> initial, Action<List<LevelGoal>> onOk, Action onCancel)
		{
			var w = CreateInstance<LevelGoalsPromptWindow>();
			w.titleContent = new GUIContent(title);

			w._isMulti = false;
			// IMPORTANT: deep copy to avoid mutating source goals even if LevelGoal is a class
			w._singleGoals = DeepCopyGoals(initial);

			w._onOkSingle = onOk;
			w._onCancelSingle = onCancel;

			w.minSize = new Vector2(520, 300);
			w.ShowModalUtility();
		}

		/// <summary>
		/// Multi: отображает goals для всех уровней сразу (без кликов).
		/// Галка "Edit same for all" включает общий редактор.
		/// Галка "Show per-level goals" держит все значения перед глазами.
		/// </summary>
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

			w.minSize = new Vector2(980, 520);
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

			using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
			{
				_scroll = sv.scrollPosition;

				// 1) Общий редактор
				if (_editSameForAll)
				{
					EditorGUILayout.HelpBox("Editing common goals. Press OK to apply to all selected levels.", MessageType.None);
					LevelGoalsEditorGUI.DrawGoalsEditor(ref _commonGoals);

					// 2) И по твоему желанию — держим перед глазами, что у кого сейчас
					if (_showPerLevelGoals)
					{
						EditorGUILayout.Space(10);
						DrawPerLevelReadOnlyTable();
					}
				}
				else
				{
					// Пер-уровневое редактирование: всё на экране сразу (без кликов)
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

			foreach (var lvl in _targets)
			{
				if (lvl == null) continue;

				var goals = GetGoalsForTarget(lvl) ?? new List<LevelGoal>(MaxGoals);

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
						for (int i = 0; i < goals.Count; i++)
						{
							var g = goals[i];
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

			foreach (var lvl in _targets)
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

					// inline editor for this level
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

				if (GUILayout.Button("OK", GUILayout.Width(110)))
				{
					// IMPORTANT: deep copy on output too
					_onOkSingle?.Invoke(DeepCopyGoals(_singleGoals));
					Close();
				}
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
					if (GUILayout.Button("OK", GUILayout.Width(110)))
					{
						_onOkMulti?.Invoke(BuildResultMapForOk());
						Close();
					}
				}
			}
		}

		// ---------------- Data helpers ----------------

		private void BuildGoalsCacheFromTargets()
		{
			_goalsByStableId = new Dictionary<string, List<LevelGoal>>(StringComparer.OrdinalIgnoreCase);

			foreach (var lvl in _targets)
			{
				if (lvl == null) continue;
				_goalsByStableId[lvl.StableId] = DeepCopyGoals(lvl.goals);
			}
		}

		private List<LevelGoal> GetGoalsForTarget(LevelConfig lvl)
		{
			if (lvl == null) return null;

			if (_goalsByStableId != null && _goalsByStableId.TryGetValue(lvl.StableId, out var goals))
				return goals;

			return DeepCopyGoals(lvl.goals);
		}

		private void SetGoalsForTarget(LevelConfig lvl, List<LevelGoal> goals)
		{
			if (lvl == null) return;
			_goalsByStableId ??= new Dictionary<string, List<LevelGoal>>(StringComparer.OrdinalIgnoreCase);
			_goalsByStableId[lvl.StableId] = DeepCopyGoals(goals);
		}

		private Dictionary<LevelConfig, List<LevelGoal>> BuildResultMapForOk()
		{
			var map = new Dictionary<LevelConfig, List<LevelGoal>>();

			if (_editSameForAll)
			{
				var common = DeepCopyGoals(_commonGoals);
				foreach (var lvl in _targets)
					if (lvl != null) map[lvl] = DeepCopyGoals(common);
				return map;
			}

			foreach (var lvl in _targets)
			{
				if (lvl == null) continue;
				map[lvl] = DeepCopyGoals(GetGoalsForTarget(lvl));
			}

			return map;
		}

		private bool AllGoalsSameAcrossTargets()
		{
			if (_targets == null || _targets.Count <= 1) return true;

			List<LevelGoal> first = GetGoalsForTarget(_targets[0]);
			for (int i = 1; i < _targets.Count; i++)
			{
				if (!GoalsEqual(first, GetGoalsForTarget(_targets[i])))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Deep copy goals to avoid mutating original assets on Cancel (important if LevelGoal is a class).
		/// </summary>
		private static List<LevelGoal> DeepCopyGoals(List<LevelGoal> src)
		{
			var list = new List<LevelGoal>(MaxGoals);
			if (src == null) return list;

			for (int i = 0; i < src.Count && i < MaxGoals; i++)
			{
				var g = src[i];

				// If LevelGoal is a struct: this is just a value copy.
				// If LevelGoal is a class: we create a new instance.
				list.Add(new LevelGoal
				{
					Type = g.Type,
					Target = g.Target,
					Tag = g.Tag ?? ""
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
