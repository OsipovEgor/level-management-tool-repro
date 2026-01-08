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
		private static readonly HashSet<string> ExcludedFromUI = new()
		{
			"m_Script",
			"stableId",
			"goals"
		};

		private List<LevelConfig> _targets;
		private List<LevelConfig> _drafts;
		private LevelConfig _commonDraft;
		private SerializedObject _commonSo;

		private Vector2 _scroll;

		// UX toggles
		private bool _editSameForAll = true;
		private bool _showPerLevel = true;

		private Action _onApplied;

		private bool _commonDirtyFromNonSerialized;

		#region Open / Lifecycle

		public static void Show(string title, IReadOnlyList<LevelConfig> targets, Action onApplied = null)
		{
			if (targets == null) return;

			List<LevelConfig> list = targets.Where(t => t != null).Distinct().ToList();
			if (list.Count == 0) return;

			LevelEditPromptWindow w = CreateInstance<LevelEditPromptWindow>();
			w.titleContent = new GUIContent(title);
			w._targets = list;
			w._onApplied = onApplied;

			w.BuildDrafts();

			w.minSize = new Vector2(650, 560);
			w.ShowModalUtility();
		}

		private void OnDisable()
		{
			DisposeDrafts();
		}

		#endregion

		#region Drafts

		private void BuildDrafts()
		{
			DisposeDrafts();

			if (_targets == null || _targets.Count == 0)
				return;

			_drafts = new List<LevelConfig>(_targets.Count);

			foreach (LevelConfig target in _targets)
			{
				LevelConfig draft = Instantiate(target);
				draft.name = target.name;
				draft.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				_drafts.Add(draft);
			}

			_commonDraft = Instantiate(_drafts[0]);
			_commonDraft.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
			_commonSo = new SerializedObject(_commonDraft);

			_commonDirtyFromNonSerialized = false;
		}

		private void DisposeDrafts()
		{
			if (_drafts != null)
			{
				foreach (LevelConfig d in _drafts)
				{
					if (d != null)
						DestroyImmediate(d);
				}

				_drafts = null;
			}

			if (_commonDraft != null)
				DestroyImmediate(_commonDraft);

			_commonDraft = null;
			_commonSo = null;
			_commonDirtyFromNonSerialized = false;
		}

		#endregion

		#region GUI

		private void OnGUI()
		{
			if (_targets == null || _targets.Count == 0 || _drafts == null)
			{
				EditorGUILayout.HelpBox("No targets.", MessageType.Info);
				if (GUILayout.Button("Close")) Close();
				return;
			}

			if (_drafts == null || _drafts.Count == 0)
				BuildDrafts();

			if (_drafts == null || _drafts.Count == 0)
			{
				EditorGUILayout.HelpBox("Failed to build drafts.", MessageType.Warning);
				if (GUILayout.Button("Close")) Close();
				return;
			}

			EditorGUILayout.LabelField(
				_targets.Count == 1 ? "Edit Level" : $"Edit Levels ({_targets.Count})",
				EditorStyles.boldLabel);

			EditorGUILayout.Space(4);
			DrawTopToggles();
			EditorGUILayout.Space(6);

			using (EditorGUILayout.ScrollViewScope sv = new(_scroll))
			{
				_scroll = sv.scrollPosition;

				if (_editSameForAll)
				{
					DrawCommonEditor();

					if (_showPerLevel)
					{
						EditorGUILayout.Space(10);
						DrawPerLevelReadOnly();
					}
				}
				else
				{
					DrawPerLevelEditable();
				}
			}

			DrawFooter();
		}

		private void DrawTopToggles()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				_editSameForAll = EditorGUILayout.ToggleLeft(
					"Same values for selected levels",
					_editSameForAll);

				GUILayout.Space(14);

				using (new EditorGUI.DisabledScope(!_editSameForAll))
				{
					_showPerLevel = EditorGUILayout.ToggleLeft(
						"Show per-level values",
						_showPerLevel);
				}

				GUILayout.FlexibleSpace();
			}
		}

		#endregion

		#region Common Editor

		private void DrawCommonEditor()
		{
			using (new EditorGUILayout.VerticalScope("box"))
			{
				EditorGUILayout.HelpBox(
					"Editing a draft template. Changes are applied only after clicking Apply.",
					MessageType.None);

				_commonSo.Update();

				DrawTwoColumn(
					() => DrawAutoProperties(_commonSo),
					() => DrawGoalsColumn_Common(_commonSo));

				bool changed = _commonSo.ApplyModifiedProperties();

				if (!changed && !_commonDirtyFromNonSerialized)
					return;

				_commonDirtyFromNonSerialized = false;

				foreach (LevelConfig draft in _drafts)
				{
					EditorUtility.CopySerialized(_commonDraft, draft);
				}
			}
		}

		private void DrawGoalsColumn_Common(SerializedObject so)
		{
			EditorGUILayout.LabelField("Goals (0..3)", EditorStyles.boldLabel);

			List<LevelGoal> goals = _commonDraft.goals != null
				? LevelGoalsDeepCopy(_commonDraft.goals)
				: new List<LevelGoal>(LevelGoalsEditorGUI.MaxGoals);

			EditorGUI.BeginChangeCheck();
			LevelGoalsEditorGUI.DrawGoalsEditor(ref goals);

			if (!EditorGUI.EndChangeCheck())
				return;

			_commonDraft.goals = LevelGoalsDeepCopy(goals);

			_commonDirtyFromNonSerialized = true;

			foreach (LevelConfig draft in _drafts)
			{
				draft.goals = LevelGoalsDeepCopy(goals);
			}

			EditorUtility.SetDirty(_commonDraft);
			so.Update();

			GUI.changed = true;
		}

		private static List<LevelGoal> LevelGoalsDeepCopy(List<LevelGoal> src)
		{
			List<LevelGoal> list = new(LevelGoalsEditorGUI.MaxGoals);

			if (src == null)
				return list;

			for (int i = 0; i < src.Count && i < LevelGoalsEditorGUI.MaxGoals; i++)
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

		#endregion

		#region Per-Level Editors

		private void DrawPerLevelReadOnly()
		{
			EditorGUILayout.LabelField("Per-level preview (drafts)", EditorStyles.boldLabel);

			for (int index = 0; index < _drafts.Count; index++)
			{
				using (new EditorGUILayout.VerticalScope("box"))
				{
					DrawLevelHeader(_targets[index]);

					using (new EditorGUI.DisabledScope(true))
					{
						SerializedObject so = new(_drafts[index]);
						so.Update();

						int indexCopy = index;
						DrawTwoColumn(
							() => DrawAutoProperties(so),
							() => DrawGoalsColumn_PerLevel(_drafts[indexCopy]));

						so.ApplyModifiedProperties();
					}
				}
			}
		}

		private void DrawPerLevelEditable()
		{
			EditorGUILayout.LabelField("Per-level edit (drafts)", EditorStyles.boldLabel);

			for (int index = 0; index < _drafts.Count; index++)
			{
				using (new EditorGUILayout.VerticalScope("box"))
				{
					DrawLevelHeader(_targets[index]);

					SerializedObject so = new(_drafts[index]);
					so.Update();

					int indexCopy = index;
					DrawTwoColumn(
						() => DrawAutoProperties(so),
						() => DrawGoalsColumn_PerLevel(_drafts[indexCopy]));

					so.ApplyModifiedProperties();
				}
			}
		}

		#endregion

		#region Helpers

		private static void DrawLevelHeader(LevelConfig original)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField(original.name, EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Ping", GUILayout.Width(50)))
					EditorGUIUtility.PingObject(original);
			}
		}

		private static void DrawTwoColumn(Action left, Action right)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(260)))
					left?.Invoke();

				GUILayout.Space(12);

				using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(360)))
					right?.Invoke();
			}
		}

		private static void DrawAutoProperties(SerializedObject so)
		{
			SerializedProperty prop = so.GetIterator();
			bool enterChildren = true;

			while (prop.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (ExcludedFromUI.Contains(prop.name))
					continue;

				EditorGUILayout.PropertyField(prop, true);
			}
		}

		private static void DrawGoalsColumn_PerLevel(LevelConfig draft)
		{
			EditorGUILayout.LabelField("Goals (0..3)", EditorStyles.boldLabel);

			List<LevelGoal> goals = draft.goals != null
				? LevelGoalsDeepCopy(draft.goals)
				: new List<LevelGoal>(LevelGoalsEditorGUI.MaxGoals);

			EditorGUI.BeginChangeCheck();
			LevelGoalsEditorGUI.DrawGoalsEditor(ref goals);

			if (!EditorGUI.EndChangeCheck())
				return;

			draft.goals = LevelGoalsDeepCopy(goals);
			GUI.changed = true;
		}

		#endregion

		#region Apply / Cancel

		private void CommitDrafts()
		{
			Undo.RecordObjects(_targets.Cast<UnityEngine.Object>().ToArray(), "Edit Levels");

			for (int i = 0; i < _targets.Count; i++)
			{
				LevelConfig original = _targets[i];
				LevelConfig draft = _drafts[i];

				if (!original || !draft)
					continue;

				string stableId = original.StableId;
				string originalName = original.name;

				EditorUtility.CopySerialized(draft, original);

				original.StableId = stableId;
				original.name = originalName;

				EditorUtility.SetDirty(original);
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

				if (!GUILayout.Button("Apply", GUILayout.Width(110)))
					return;

				CommitDrafts();
				_onApplied?.Invoke();
				Close();
			}
		}

		#endregion
	}
}
#endif