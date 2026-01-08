#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public sealed class LevelNamePromptWindow : EditorWindow
	{
		private string _name;
		private string _folder;
		private string _error;

		private Action<string> _onOk;
		private Action _onCancel;

		private const float Width = 420f;
		private HashSet<string> _existingNames;

		public static void Show(
			string title,
			string folder,
			string initialName,
			HashSet<string> existingNames,
			Action<string> onOk,
			Action onCancel)
		{
			LevelNamePromptWindow w = CreateInstance<LevelNamePromptWindow>();
			w._existingNames = existingNames ?? new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
			w.titleContent = new GUIContent(title);
			w._folder = folder;
			w._name = initialName ?? string.Empty;
			w._onOk = onOk;
			w._onCancel = onCancel;

			w.minSize = new Vector2(Width, 140);
			w.maxSize = new Vector2(Width, 200);

			w.ShowModalUtility();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Create Level", EditorStyles.boldLabel);
			EditorGUILayout.Space(6);

			EditorGUILayout.LabelField("Folder", _folder);
			EditorGUILayout.Space(4);

			GUI.SetNextControlName("LevelNameField");
			_name = EditorGUILayout.TextField("Name", _name);

			Validate();

			if (!string.IsNullOrEmpty(_error))
			{
				EditorGUILayout.HelpBox(_error, MessageType.Error);
			}
			else
			{
				EditorGUILayout.HelpBox("Enter a valid name. Asset extension will be .asset", MessageType.Info);
			}

			EditorGUILayout.Space(8);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Cancel", GUILayout.Width(90)))
				{
					_onCancel?.Invoke();
					Close();
					return;
				}

				using (new EditorGUI.DisabledScope(!IsValid()))
				{
					if (GUILayout.Button("Create", GUILayout.Width(90)))
					{
						_onOk?.Invoke(_name.Trim());
						Close();
						return;
					}
				}
			}

			switch (Event.current.type)
			{
				case EventType.Repaint:
					EditorGUI.FocusTextInControl("LevelNameField");
					break;
				case EventType.KeyDown when Event.current.keyCode == KeyCode.Return ||
											Event.current.keyCode == KeyCode.KeypadEnter:
				{
					if (IsValid())
					{
						_onOk?.Invoke(_name.Trim());
						Close();
					}

					Event.current.Use();
					break;
				}
				case EventType.KeyDown:
				{
					if (Event.current.keyCode == KeyCode.Escape)
					{
						_onCancel?.Invoke();
						Close();
						Event.current.Use();
					}

					break;
				}
			}
		}

		private void Validate()
		{
			_error = null;

			string trimmed = (_name ?? string.Empty).Trim();
			if (string.IsNullOrEmpty(trimmed))
			{
				_error = "Name cannot be empty.";
				return;
			}

			foreach (char c in Path.GetInvalidFileNameChars())
			{
				if (trimmed.IndexOf(c) < 0)
					continue;

				_error = $"Name contains invalid character: '{c}'";
				return;
			}

			if (trimmed.EndsWith(".", StringComparison.Ordinal) || trimmed.EndsWith(" ", StringComparison.Ordinal))
			{
				_error = "Name cannot end with '.' or space.";
				return;
			}

			if (_existingNames != null && _existingNames.Contains(trimmed))
			{
				_error = "This level name is already used by another LevelConfig asset.";
				return;
			}

			string assetPath = $"{_folder}/{trimmed}.asset";

			if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
				return;

			_error = "An asset with this file name already exists in the target folder.";
		}

		private bool IsValid() => string.IsNullOrEmpty(_error);
	}
}
#endif