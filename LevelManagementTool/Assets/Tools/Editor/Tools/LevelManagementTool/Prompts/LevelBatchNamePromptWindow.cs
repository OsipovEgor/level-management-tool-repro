#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public sealed class LevelBatchNamePromptWindow : EditorWindow
	{
		private struct GeneratedName
		{
			public string name;
			public string error; // null = ok
		}

		private string _folder;
		private HashSet<string> _existingNames; // LevelConfig asset names (case-insensitive)

		private string _prefix = "Level_";
		private string _suffix = "";
		private int _startIndex = 1;
		private int _digits = 3;
		private int _count = 10;

		private string _error;
		private List<GeneratedName> _generated = new List<GeneratedName>();

		private Action<List<string>, bool> _onOk2;
		private Action _onCancel;

		private Vector2 _scroll;

		private bool _canInsertIntoDatabase;
		private bool _insertIntoDatabaseAfterSelected = true;
		private string _insertAfterLabel = "(end)";

		public static void Show(
			string title,
			string folder,
			int count,
			HashSet<string> existingNames,
			bool canInsertIntoDatabase,
			string insertAfterLabel,
			Action<List<string>, bool> onOk,
			Action onCancel)
		{
			var w = CreateInstance<LevelBatchNamePromptWindow>();
			w.titleContent = new GUIContent(title);
			w._folder = folder;
			w._count = Mathf.Clamp(count, 2, 200);
			w._existingNames = existingNames ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			w._canInsertIntoDatabase = canInsertIntoDatabase;
			w._insertAfterLabel = string.IsNullOrEmpty(insertAfterLabel) ? "(end)" : insertAfterLabel;
			w._onOk2 = onOk;
			w._onCancel = onCancel;

			w.minSize = new Vector2(520, 420);
			w.maxSize = new Vector2(900, 900);

			w.ShowModalUtility();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Batch Create Levels", EditorStyles.boldLabel);
			EditorGUILayout.Space(6);

			EditorGUILayout.LabelField("Folder", _folder);

			EditorGUILayout.Space(6);
			using (new EditorGUILayout.VerticalScope("box"))
			{
				_count = EditorGUILayout.IntField("Count", _count);
				_count = Mathf.Clamp(_count, 2, 200);

				_prefix = EditorGUILayout.TextField("Prefix", _prefix);
				_suffix = EditorGUILayout.TextField("Suffix", _suffix);

				_startIndex = EditorGUILayout.IntField("Start index", _startIndex);
				_startIndex = Mathf.Max(0, _startIndex);

				_digits = EditorGUILayout.IntSlider("Digits (zero padding)", _digits, 1, 6);
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Use next available start index", GUILayout.Width(240)))
				{
					_startIndex = ComputeNextAvailableStartIndex();
					GUI.FocusControl(null);
				}

				GUILayout.FlexibleSpace();
			}

			EditorGUILayout.Space(6);
			using (new EditorGUILayout.VerticalScope("box"))
			{
				using (new EditorGUI.DisabledScope(!_canInsertIntoDatabase))
				{
					_insertIntoDatabaseAfterSelected = EditorGUILayout.ToggleLeft(
						$"Insert into LevelDatabase after: {_insertAfterLabel}",
						_insertIntoDatabaseAfterSelected);

					if (!_canInsertIntoDatabase)
						EditorGUILayout.HelpBox("No LevelDatabase selected. Levels will be created as assets only.",
							MessageType.None);
				}
			}

			GenerateAndValidate();

			if (!string.IsNullOrEmpty(_error))
				EditorGUILayout.HelpBox(_error, MessageType.Error);
			else
				EditorGUILayout.HelpBox(
					"Preview names below. Create will be enabled when all names are valid and unique.",
					MessageType.Info);

			EditorGUILayout.Space(8);
			DrawPreview();

			EditorGUILayout.Space(10);
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Cancel", GUILayout.Width(110)))
				{
					_onCancel?.Invoke();
					Close();
					return;
				}

				using (new EditorGUI.DisabledScope(!IsValid()))
				{
					if (GUILayout.Button("Create", GUILayout.Width(110)))
					{
						_onOk2?.Invoke(GetGeneratedNames(), _insertIntoDatabaseAfterSelected);
						Close();
						return;
					}
				}
			}

			// Enter/Esc
			if (Event.current.type == EventType.KeyDown)
			{
				if (Event.current.keyCode == KeyCode.Escape)
				{
					_onCancel?.Invoke();
					Close();
					Event.current.Use();
				}
				else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
				{
					if (IsValid())
					{
						_onOk2?.Invoke(GetGeneratedNames(), _insertIntoDatabaseAfterSelected);
						Close();
					}

					Event.current.Use();
				}
			}
		}

		private List<string> GetGeneratedNames()
		{
			var list = new List<string>(_generated.Count);
			foreach (var e in _generated)
				list.Add(e.name);
			return list;
		}

		private int ComputeNextAvailableStartIndex()
		{
			// Ищем максимум среди имён вида: prefix + digits + suffix (digits = число, длина любая)
			// Затем возвращаем max+1.
			var prefix = (_prefix ?? "").Trim();
			var suffix = (_suffix ?? "").Trim();

			int max = -1;

			foreach (var name in _existingNames)
			{
				if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
				if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;

				var mid = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
				if (string.IsNullOrEmpty(mid)) continue;

				// mid должен быть числом
				if (int.TryParse(mid, out var idx))
					max = Mathf.Max(max, idx);
			}

			return max + 1 < 0 ? 0 : max + 1;
		}

		private void DrawPreview()
		{
			EditorGUILayout.LabelField("Name preview", EditorStyles.boldLabel);

			using (var sv = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.Height(220)))
			{
				_scroll = sv.scrollPosition;

				if (_generated.Count == 0)
				{
					EditorGUILayout.LabelField("(no names)");
					return;
				}

				int max = Mathf.Min(50, _generated.Count);
				for (int i = 0; i < max; i++)
				{
					var entry = _generated[i];

					if (entry.error == null)
					{
						EditorGUILayout.LabelField(entry.name);
					}
					else
					{
						var prevColor = GUI.color;
						GUI.color = new Color(1f, 0.45f, 0.45f);

						EditorGUILayout.LabelField(
							$"{entry.name}  —  {entry.error}",
							EditorStyles.boldLabel
						);

						GUI.color = prevColor;
					}
				}

				if (_generated.Count > max)
					EditorGUILayout.LabelField($"... and {_generated.Count - max} more");
			}
		}

		private void GenerateAndValidate()
		{
			_error = null;
			_generated.Clear();

			var prefix = (_prefix ?? "").Trim();
			var suffix = (_suffix ?? "").Trim();

			if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
			{
				_error = "Prefix and suffix cannot both be empty.";
				return;
			}

			var localSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			for (int i = 0; i < _count; i++)
			{
				int idx = _startIndex + i;
				string num = idx.ToString(new string('0', _digits));
				string name = $"{prefix}{num}{suffix}";

				string entryError = null;

				// invalid chars
				foreach (var c in Path.GetInvalidFileNameChars())
				{
					if (name.IndexOf(c) >= 0)
					{
						entryError = $"Invalid character '{c}'";
						break;
					}
				}

				if (entryError == null &&
					(name.EndsWith(".", StringComparison.Ordinal) || name.EndsWith(" ", StringComparison.Ordinal)))
				{
					entryError = "Cannot end with '.' or space";
				}

				if (entryError == null && !localSet.Add(name))
				{
					entryError = "Duplicate in generated list";
				}

				if (entryError == null && _existingNames.Contains(name))
				{
					entryError = "Already exists (LevelConfig)";
				}

				if (entryError == null)
				{
					var assetPath = $"{_folder}/{name}.asset";
					if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
						entryError = "Asset already exists in folder";
				}

				_generated.Add(new GeneratedName
				{
					name = name,
					error = entryError
				});
			}

			// Общая ошибка (для HelpBox + блокировки Create)
			if (_generated.Any(e => e.error != null))
				_error = "Some generated names are invalid. See highlighted entries below.";
		}

		private bool IsValid()
		{
			if (!string.IsNullOrEmpty(_error)) return false;
			if (_generated == null || _generated.Count < 2) return false;
			return _generated.All(e => e.error == null);
		}
	}
}
#endif