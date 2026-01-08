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
		// Fields hidden from UI
		private static readonly HashSet<string> ExcludedFromUI = new()
		{
			"m_Script",
			"stableId",
			"goals" // goals are drawn in the right column
		};

		private List<LevelConfig> _targets; // original assets
		private List<LevelConfig> _drafts;  // in-memory drafts
		private LevelConfig _commonDraft;   // template draft
		private SerializedObject _commonSO;

		private Vector2 _scroll;

		// UX toggles
		private bool _editSameForAll = true;
		private bool _showPerLevel = true;

		private Action _onApplied;

		// IMPORTANT: goals are edited outside SerializedObject (direct list edit),
		// so ApplyModifiedProperties() may not detect changes (especially remove).
		private bool _commonDirtyFromNonSerialized;

		#region Open / Lifecycle

		public static void Show(string title, IReadOnlyList<LevelConfig> targets, Action onApplied = null)
		{
			if (targets == null) return;

			var list = targets.Where(t => t != null).Distinct().ToList();
			if (list.Count == 0) return;

			var w = CreateInstance<LevelEditPromptWindow>();
			w.titleContent = new GUIContent(title);
			w._targets = list;
			w._onApplied = onApplied;

			w.BuildDrafts();

			w.minSize = new Vector2(1040, 560);
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

			foreach (var t in _targets)
			{
				var d = Instantiate(t);
				d.name = t.name;
				d.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				_drafts.Add(d);
			}

			_commonDraft = Instantiate(_drafts[0]);
			_commonDraft.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
			_commonSO = new SerializedObject(_commonDraft);

			_commonDirtyFromNonSerialized = false;
		}

		private void DisposeDrafts()
		{
			if (_drafts != null)
			{
				foreach (var d in _drafts)
					if (d != null)
						DestroyImmediate(d);
				_drafts = null;
			}

			if (_commonDraft != null)
				DestroyImmediate(_commonDraft);

			_commonDraft = null;
			_commonSO = null;
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

			using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
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

				_commonSO.Update();

				DrawTwoColumn(
					() => DrawAutoProperties(_commonSO),
					() => DrawGoalsColumn_Common(_commonSO));

				bool changed = _commonSO.ApplyModifiedProperties();

				// propagate template to drafts (preview only)
				// IMPORTANT: also propagate when goals changed outside SerializedObject
				if (changed || _commonDirtyFromNonSerialized)
				{
					_commonDirtyFromNonSerialized = false;

					foreach (var d in _drafts)
						EditorUtility.CopySerialized(_commonDraft, d);
				}
			}
		}

		private void DrawGoalsColumn_Common(SerializedObject so)
		{
			EditorGUILayout.LabelField("Goals (0..3)", EditorStyles.boldLabel);

			// Work on a local list to avoid accidental shared references
			var goals = _commonDraft.goals != null
				? LevelGoalsDeepCopy(_commonDraft.goals)
				: new List<LevelGoal>(LevelGoalsEditorGUI.MaxGoals);

			EditorGUI.BeginChangeCheck();
			LevelGoalsEditorGUI.DrawGoalsEditor(ref goals);
			if (EditorGUI.EndChangeCheck())
			{
				// 1) Set to common draft (as deep copy)
				_commonDraft.goals = LevelGoalsDeepCopy(goals);

				// 2) Mark "non-serialized dirty" so common editor will propagate even if ApplyModifiedProperties=false
				_commonDirtyFromNonSerialized = true;

				// 3) Keep drafts in sync immediately (so preview is correct right away)
				foreach (var d in _drafts)
					d.goals = LevelGoalsDeepCopy(goals);

				// 4) Help Unity notice object changed outside SerializedObject (safe no-op if unnecessary)
				EditorUtility.SetDirty(_commonDraft);
				so.Update();

				GUI.changed = true;
			}
		}

		private static List<LevelGoal> LevelGoalsDeepCopy(List<LevelGoal> src)
		{
			var list = new List<LevelGoal>(LevelGoalsEditorGUI.MaxGoals);
			if (src == null) return list;

			for (int i = 0; i < src.Count && i < LevelGoalsEditorGUI.MaxGoals; i++)
			{
				var g = src[i];
				list.Add(new LevelGoal
				{
					Type = g.Type,
					Target = g.Target,
					Tag = g.Tag ?? ""
				});
			}

			return list;
		}

		#endregion

		#region Per-Level Editors

		private void DrawPerLevelReadOnly()
		{
			EditorGUILayout.LabelField("Per-level preview (drafts)", EditorStyles.boldLabel);

			for (int i = 0; i < _drafts.Count; i++)
			{
				using (new EditorGUILayout.VerticalScope("box"))
				{
					DrawLevelHeader(_targets[i]);

					using (new EditorGUI.DisabledScope(true))
					{
						var so = new SerializedObject(_drafts[i]);
						so.Update();

						DrawTwoColumn(
							() => DrawAutoProperties(so),
							() => DrawGoalsColumn_PerLevel(_drafts[i]));

						so.ApplyModifiedProperties();
					}
				}
			}
		}

		private void DrawPerLevelEditable()
		{
			EditorGUILayout.LabelField("Per-level edit (drafts)", EditorStyles.boldLabel);

			for (int i = 0; i < _drafts.Count; i++)
			{
				using (new EditorGUILayout.VerticalScope("box"))
				{
					DrawLevelHeader(_targets[i]);

					var so = new SerializedObject(_drafts[i]);
					so.Update();

					DrawTwoColumn(
						() => DrawAutoProperties(so),
						() => DrawGoalsColumn_PerLevel(_drafts[i]));

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
				using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(520)))
					left?.Invoke();

				GUILayout.Space(12);

				using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(360)))
					right?.Invoke();
			}
		}

		private static void DrawAutoProperties(SerializedObject so)
		{
			var prop = so.GetIterator();
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

			var goals = draft.goals != null
				? LevelGoalsDeepCopy(draft.goals)
				: new List<LevelGoal>(LevelGoalsEditorGUI.MaxGoals);

			EditorGUI.BeginChangeCheck();
			LevelGoalsEditorGUI.DrawGoalsEditor(ref goals);
			if (EditorGUI.EndChangeCheck())
			{
				draft.goals = LevelGoalsDeepCopy(goals);
				GUI.changed = true;
			}
		}

		#endregion

		#region Apply / Cancel

		private void CommitDrafts()
		{
			Undo.RecordObjects(_targets.Cast<UnityEngine.Object>().ToArray(), "Edit Levels");

			for (int i = 0; i < _targets.Count; i++)
			{
				var original = _targets[i];
				var draft = _drafts[i];
				if (original == null || draft == null) continue;

				// Preserve fields that must never change
				string stableId = original.StableId;
				string originalName = original.name;

				EditorUtility.CopySerialized(draft, original);

				// Restore protected values
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
					Close(); // drafts discarded
					return;
				}

				if (GUILayout.Button("Apply", GUILayout.Width(110)))
				{
					CommitDrafts();
					_onApplied?.Invoke();
					Close();
				}
			}
		}

		#endregion
	}
}
#endif
