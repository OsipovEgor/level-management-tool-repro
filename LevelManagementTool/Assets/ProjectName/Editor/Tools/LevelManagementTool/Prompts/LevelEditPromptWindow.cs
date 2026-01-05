#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public sealed class LevelEditPromptWindow : EditorWindow
	{
		private static readonly HashSet<string> ExcludedProps = new()
		{
			"m_Script",
			"stableId",
			"goals" // goals рисуем отдельной секцией
		};

		private List<LevelConfig> _targets;
		private SerializedObject _so;

		// goals UI
		private bool _showGoals = true;
		private bool _useGoalsModal = true; // опционально: кнопка "Edit Goals..." внутри
		private Vector2 _scroll;

		private Action _onApplied; // чтобы дернуть Validate/Repaint снаружи при желании

		public static void Show(string title, IReadOnlyList<LevelConfig> targets, Action onApplied = null)
		{
			if (targets == null) return;

			var list = targets.Where(t => t != null).Distinct().ToList();
			if (list.Count == 0) return;

			var w = CreateInstance<LevelEditPromptWindow>();
			w.titleContent = new GUIContent(title);
			w._targets = list;
			w._onApplied = onApplied;

			w.minSize = new Vector2(760, 520);
			w.ShowModalUtility();
		}

		private void OnEnable()
		{
			if (_targets == null || _targets.Count == 0) return;
		}

		private void OnGUI()
		{
			if (_targets == null || _targets.Count == 0)
			{
				EditorGUILayout.HelpBox("No targets.", MessageType.Info);
				if (GUILayout.Button("Close")) Close();
				return;
			}
			_so = new SerializedObject(_targets.ToArray());

			EditorGUILayout.LabelField(
				_targets.Count == 1 ? "Edit Level" : $"Edit Levels ({_targets.Count})",
				EditorStyles.boldLabel);

			EditorGUILayout.Space(6);

			using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
			{
				_scroll = sv.scrollPosition;

				_so.Update();

				DrawAutoProperties(_so);

				EditorGUILayout.Space(8);
				DrawGoalsSection(_so);

				_so.ApplyModifiedProperties();
			}

			DrawFooter();
		}

		private static void DrawAutoProperties(SerializedObject so)
		{
			var prop = so.GetIterator();
			bool enterChildren = true;

			while (prop.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (ExcludedProps.Contains(prop.name))
					continue;

				EditorGUILayout.PropertyField(prop, includeChildren: true);
			}
		}

		private void DrawGoalsSection(SerializedObject so)
		{
			_showGoals = EditorGUILayout.Foldout(_showGoals, "Goals", true);
			if (!_showGoals) return;

			using (new EditorGUI.IndentLevelScope())
			{
				// Вариант А: встроенно, стандартно (Unity сам покажет mixed values)
				SerializedProperty goalsProp = so.FindProperty("goals");
				if (goalsProp != null)
				{
					EditorGUILayout.PropertyField(goalsProp, includeChildren: true);
				}
				else
				{
					EditorGUILayout.HelpBox("Property 'goals' not found.", MessageType.Warning);
				}

				// Вариант Б: кнопка открыть твою продвинутую goals-модалку
				_useGoalsModal = EditorGUILayout.ToggleLeft("Use Goals modal editor", _useGoalsModal);
				if (_useGoalsModal)
				{
					if (GUILayout.Button("Open Goals Editor...", GUILayout.Width(200)))
					{
						// открываем твой LevelGoalsPromptWindow в multi-режиме
						var targetsCopy = _targets.ToList();
						LevelGoalsPromptWindow.ShowMulti(
							title: "Edit Goals",
							targets: targetsCopy,
							onOk: map =>
							{
								// Тут ожидается, что у контроллера есть ApplyGoalsPerLevel(map).
								// Если его нет — можно сделать статический helper, но лучше в контроллер.
								ApplyGoalsPerLevel_Inline(map);
								// обновим SerializedObject, чтобы UI отражал изменения
								_so = new SerializedObject(_targets.ToArray());
								Repaint();
								_onApplied?.Invoke();
							},
							onCancel: () => { },
							defaultEditSameForAll: true,
							defaultShowPerLevelGoals: true
						);
					}
				}
			}
		}

		private static void ApplyGoalsPerLevel_Inline(Dictionary<LevelConfig, List<LevelGoal>> map)
		{
			if (map == null || map.Count == 0) return;

			var levels = map.Keys.Where(x => x != null).ToArray();
			Undo.RecordObjects(levels, "Apply Goals (Per Level)");

			foreach (var kv in map)
			{
				var lvl = kv.Key;
				if (lvl == null) continue;

				var goals = kv.Value ?? new List<LevelGoal>();
				lvl.goals ??= new List<LevelGoal>(3);
				lvl.goals.Clear();

				for (int i = 0; i < goals.Count && i < 3; i++)
					lvl.goals.Add(goals[i]);

				EditorUtility.SetDirty(lvl);
			}

			AssetDatabase.SaveAssets();
		}

		private void DrawFooter()
		{
			EditorGUILayout.Space(10);
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Cancel", GUILayout.Width(110)))
				{
					Close();
					return;
				}

				if (GUILayout.Button("Apply", GUILayout.Width(110)))
				{
					// ApplyModifiedProperties уже делается в OnGUI, но Undo/Dirty лучше фиксировать тут при необходимости.
					foreach (var t in _targets)
						if (t != null) EditorUtility.SetDirty(t);

					AssetDatabase.SaveAssets();
					_onApplied?.Invoke();
					Close();
				}
			}
		}
	}
}
#endif
