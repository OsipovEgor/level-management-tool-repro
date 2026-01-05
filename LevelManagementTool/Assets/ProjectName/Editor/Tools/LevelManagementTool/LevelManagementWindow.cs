#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public class LevelManagementWindow : EditorWindow
	{
		private LevelManagementContext _ctx;
		private LevelManagementController _controller;

		private bool _firstRefresh;

		public static void ShowWindow()
		{
			var w = GetWindow<LevelManagementWindow>("Level Management");
			w.minSize = new Vector2(900, 550);
			w._firstRefresh = false;
			w.Show();
		}

		private void OnEnable()
		{
			_ctx = new LevelManagementContext();
			_controller = new LevelManagementController(_ctx);

			_controller.LoadSettings();
			InitCreatePresetUI();

			LevelAssetUndoManager.AssetsChanged -= OnAssetsChanged;
			LevelAssetUndoManager.AssetsChanged += OnAssetsChanged;

			_controller.RefreshLevels();
			_controller.Validate();
		}

		private void OnDisable()
		{
			LevelAssetUndoManager.AssetsChanged -= OnAssetsChanged;
		}

		private void OnAssetsChanged()
		{
			EditorApplication.delayCall += () =>
			{
				_controller.RefreshLevels();
				_controller.Validate();
				Repaint();
			};
		}

		private void OnGUI()
		{
			LevelTopBarView.Draw(_ctx, _controller);
			LevelWelcomeView.Draw(_ctx, _controller);

			if (_ctx.Database == null)
			{
				EditorGUILayout.HelpBox("Select a LevelDatabase asset to start.", MessageType.Info);
				return;
			}

			if (!_firstRefresh)
			{
				Debug.Log("Refreshing LevelManagementWindow");
				_controller.RefreshLevels();
				_controller.Validate();
				_firstRefresh = true;
			}

			LevelCreateOptionsView.Draw(_ctx);

			EditorGUILayout.BeginHorizontal();

// Сначала готовим ReorderableList, чтобы таблица могла войти в reorder-mode
			LevelTableView.EnsureDbReorderableList(_ctx, _controller, Repaint);

// Теперь рисуем таблицу
			LevelTableView.Draw(_ctx, _controller, Repaint);

			LevelInspectorView.Draw(_ctx, _controller);

			EditorGUILayout.EndHorizontal();
		}

		private void InitCreatePresetUI()
		{
			_ctx.PresetGoals ??= new System.Collections.Generic.List<LevelGoal>(3);

			if (_ctx.PresetGoals.Count == 0)
			{
				_ctx.PresetGoals.Add(new LevelGoal { Type = GoalType.Collect, Target = 50, Tag = "Buildings" });
				_ctx.PresetGoals.Add(new LevelGoal { Type = GoalType.ReachSize, Target = 20, Tag = "Medium Size" });
			}

			_ctx.PresetGoalsList = new ReorderableList(_ctx.PresetGoals, typeof(LevelGoal), true, true, true, true)
			{
				drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Default Goals (0..3)"),
				onCanAddCallback = _ => _ctx.PresetGoals.Count < 3,
				drawElementCallback = (rect, index, _, _) =>
				{
					LevelGoal goal = _ctx.PresetGoals[index];

					float lineH = EditorGUIUtility.singleLineHeight;
					rect.y += 2;

					Rect r0 = new(rect.x, rect.y, rect.width * 0.34f, lineH);
					Rect r1 = new(rect.x + rect.width * 0.35f, rect.y, rect.width * 0.18f, lineH);
					Rect r2 = new(rect.x + rect.width * 0.54f, rect.y, rect.width * 0.46f, lineH);

					goal.Type = (GoalType)EditorGUI.EnumPopup(r0, goal.Type);
					goal.Target = Mathf.Max(1, EditorGUI.IntField(r1, goal.Target));
					goal.Tag = EditorGUI.TextField(r2, goal.Tag);

					_ctx.PresetGoals[index] = goal;
				}
			};
		}
	}
}
#endif