#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Levels;

namespace Game.Levels.EditorTool
{
    public sealed class LevelGoalsPromptWindow : EditorWindow
    {
        private List<LevelGoal> _goals;
        private Action<List<LevelGoal>> _onOk;
        private Action _onCancel;

        private Vector2 _scroll;

        public static void Show(string title, List<LevelGoal> initial, Action<List<LevelGoal>> onOk, Action onCancel)
        {
            var w = CreateInstance<LevelGoalsPromptWindow>();
            w.titleContent = new GUIContent(title);
            w._goals = initial != null ? new List<LevelGoal>(initial) : new List<LevelGoal>(3);
            if (w._goals.Count > 3) w._goals.RemoveRange(3, w._goals.Count - 3);

            w._onOk = onOk;
            w._onCancel = onCancel;

            w.minSize = new Vector2(520, 300);
            w.ShowModalUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Goals (0..3)", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = sv.scrollPosition;

                for (int i = 0; i < _goals.Count; i++)
                {
                    var g = _goals[i];

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        g.Type = (GoalType)EditorGUILayout.EnumPopup(g.Type, GUILayout.Width(160));
                        g.Target = Mathf.Max(1, EditorGUILayout.IntField(g.Target, GUILayout.Width(80)));
                        g.Tag = EditorGUILayout.TextField(g.Tag ?? "", GUILayout.ExpandWidth(true));

                        if (GUILayout.Button("X", GUILayout.Width(24)))
                        {
                            _goals.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                    }

                    _goals[i] = g;
                }

                using (new EditorGUI.DisabledScope(_goals.Count >= 3))
                {
                    if (GUILayout.Button("+ Add Goal", GUILayout.Width(110)))
                        _goals.Add(new LevelGoal { Type = GoalType.Collect, Target = 1, Tag = "" });
                }
            }

            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel", GUILayout.Width(110)))
                {
                    _onCancel?.Invoke();
                    Close();
                }

                if (GUILayout.Button("OK", GUILayout.Width(110)))
                {
                    _onOk?.Invoke(new List<LevelGoal>(_goals));
                    Close();
                }
            }
        }
    }
}
#endif
