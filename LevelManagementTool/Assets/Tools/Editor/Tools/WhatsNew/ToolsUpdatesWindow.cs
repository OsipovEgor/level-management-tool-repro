using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectName.Editor.ToolsUpdates
{
	public class ToolsUpdatesWindow : EditorWindow
	{
		private Vector2 _scroll;
		private readonly Dictionary<string, Texture2D> _screenshots = new();

		public static void ShowWindow()
		{
			var window = GetWindow<ToolsUpdatesWindow>("What's New");
			window.minSize = new Vector2(1200, 800);
		}

		private void OnGUI()
		{
			DrawHeader();
			DrawAutoOpenToggle();

			EditorGUILayout.Space(12);

			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			DrawSection_Intro();
			DrawSection_EntryPoint();
			DrawSection_LevelCreation();
			DrawSection_CreateOptions();
			DrawSection_DeleteLevels();
			DrawSection_LevelTable();
			DrawSection_Validation();
			DrawSection_Filters();
			DrawSection_MultiEdit();
			DrawSection_LevelIdMap();

			EditorGUILayout.EndScrollView();
		}

		#region Header

		private void DrawHeader()
		{
			EditorGUILayout.Space(6);

			var style = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 18
			};

			EditorGUILayout.LabelField("🆕 What's New — Level Management Tool", style);
			EditorGUILayout.Space(4);

			EditorGUILayout.LabelField(
				"This update improves level authoring workflow and reduces live-ops risk related to saves.",
				EditorStyles.wordWrappedLabel
			);
		}

		private void DrawAutoOpenToggle()
		{
			EditorGUILayout.Space(8);

			bool autoOpen = AutoOpenEditorWindow.AutoOpenEnabled;
			bool newValue = EditorGUILayout.Toggle("Open on Unity start", autoOpen);

			if (newValue != autoOpen)
				AutoOpenEditorWindow.AutoOpenEnabled = newValue;
		}

		#endregion

		#region Sections

		private void DrawSection_Intro()
		{
			DrawSectionTitle("Why this tool exists");

			DrawParagraph(
				"Designers had to manage hundreds of per-level ScriptableObjects manually, edit them one by one, " +
				"maintain a separate master list, and validate data by hand. This was slow, error-prone, and made it hard to see the big picture."
			);

			DrawParagraph(
				"Additionally, the game save system depended on the order of levels in a list. " +
				"Any reordering or insertion of new levels could cause players to resume on the wrong level after an update."
			);

			DrawParagraph(
				"The Level Management Tool centralizes level editing, reduces human error with validation and issue surfacing, " +
				"and introduces a safer way to define level order for gameplay via Level ID Map."
			);
		}

		private void DrawSection_EntryPoint()
		{
			DrawSectionTitle("New Entry Point");

			DrawParagraph("A new editor window is available at:");
			DrawCodeLine("Unity Editor → Tools → LevelManagementTool");

			DrawScreenshotHint("1.HowToAccessTool.png");
		}

		private void DrawSection_LevelCreation()
		{
			DrawSectionTitle("Controlled Level Creation");

			DrawBullet("Levels can now be created only through this tool");
			DrawBullet("Automatically registers new levels in the Level Database");
			DrawBullet("Prevents missing references and forgotten setup steps");

			DrawParagraph("This removes the need to manually drag new levels into a master list.");

			DrawScreenshotHint("2.LevelCreation.png");
		}

		private void DrawSection_CreateOptions()
		{
			DrawSectionTitle("Create Options (Undo Supported)");

			DrawBullet("Define default values for newly created levels");
			DrawBullet("Fully supports Ctrl + Z / Undo");
			DrawBullet("Speeds up batch level creation");

			DrawScreenshotHint("3.CreateLevelOptions.png");
		}

		private void DrawSection_DeleteLevels()
		{
			DrawSectionTitle("Safe Level Deletion");

			DrawBullet("Remove levels directly from the tool");
			DrawBullet("Assets and database links are handled automatically");
			DrawBullet("Fully supports Ctrl + Z / Undo");

			DrawScreenshotHint("4.InToolDeletion.png");
			DrawScreenshotHint("4.1.InToolDeletionUndoSupport.png");
		}

		private void DrawSection_LevelTable()
		{
			DrawSectionTitle("Centralized Level Table");

			DrawBullet("Drag & drop table with all levels in the database");
			DrawBullet("Reordering rows updates the database order");
			DrawBullet("Makes progression structure visible at a glance");
			DrawBullet("Includes an Issues column that flags problematic files automatically");

			DrawScreenshotHint("5.0.LevelTableDragNDrop.png");
			DrawScreenshotHint("5.1.LevelTableIssues.png");
		}

		private void DrawSection_Validation()
		{
			DrawSectionTitle("Validation & Issue Surfacing");

			DrawBullet("Validation covers both Levels and Goals");
			DrawBullet("Problems are surfaced in multiple places to prevent mistakes slipping through");

			EditorGUILayout.Space(4);
			DrawParagraph("Where you’ll see validation feedback:");

			DrawBullet("Bottom panel message summarizing current issues");
			DrawBullet("Issues column in the table highlights problematic rows");
			DrawBullet("Inspector shows a detailed message for the selected level(s)");
			DrawBullet("Multi-selection shows which files have which issues");

			DrawScreenshotHint("6.0.ValidationIssues.png");
			DrawScreenshotHint("6.1.ValidationIssues.png");
		}

		private void DrawSection_Filters()
		{
			DrawSectionTitle("Filtering & Visibility");

			DrawParagraph("Use filters to focus on what needs attention:");

			DrawBullet("All Levels (Drag & Drop Table View)");
			DrawBullet("Problematic Files");
			DrawBullet("Files with Errors");
			DrawBullet("Files with Warnings");

			DrawScreenshotHint("7.0.LevelFilters.png");
		}

		private void DrawSection_MultiEdit()
		{
			DrawSectionTitle("Multi-Level Editing");

			DrawBullet("Edit multiple levels at once");
			DrawBullet("Inspector supports multi-edit when values are identical");
			DrawBullet("Dedicated modal window for full level editing or Goals-only editing");
			DrawBullet(
				"Goals are no longer a JSON string — they are edited as a structured list of entries with fields");

			DrawScreenshotHint("8.0.EditMultipleLevels_OneValueForAll.png");
			DrawScreenshotHint("8.1.EditMultipleLevels_Individually.png");
			DrawScreenshotHint("9.0.EditMultipleLevelsGoals_OneValueForAll.png");
			DrawScreenshotHint("9.1.EditMultipleLevelsGoals_Individually.png");
		}

		private void DrawSection_LevelIdMap()
		{
			DrawSectionTitle("Level ID Map — Safe Progression Ordering");

			DrawBullet("Level order is stored separately from level assets");
			DrawBullet("Gameplay should rely on Level IDs instead of list indices");
			DrawBullet("Supports versioned Level ID Maps");

			DrawParagraph(
				"This enables reordering or inserting new levels without forcing tools to mutate the gameplay progression logic. " +
				"Migrations of existing client-side saves are expected to be handled in gameplay code using the provided ID mapping/versioning."
			);

			DrawScreenshotHint("10.0.LevelIDMap.png");
			DrawScreenshotHint("10.1.LevelIDMap.png");
		}

		#endregion

		#region UI Helpers

		private void DrawSectionTitle(string title)
		{
			EditorGUILayout.Space(14);
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
			EditorGUILayout.Space(2);
		}

		private void DrawParagraph(string text)
		{
			EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);
			EditorGUILayout.Space(4);
		}

		private void DrawBullet(string text)
		{
			EditorGUILayout.LabelField("• " + text, EditorStyles.wordWrappedLabel);
		}

		private void DrawCodeLine(string text)
		{
			var style = new GUIStyle(EditorStyles.helpBox)
			{
				fontSize = 11
			};
			EditorGUILayout.LabelField(text, style);
		}

		private void DrawScreenshotHint(string fileName)
		{
			EditorGUILayout.Space(2);
			DrawScreenshot(fileName);
		}

		private void DrawScreenshot(string fileName, float maxWidth = -1f, bool drawAtNativeSize = true)
		{
			var tex = GetScreenshot(fileName);
			if (tex == null)
			{
				EditorGUILayout.HelpBox($"Screenshot not found: {fileName}", MessageType.Warning);
				return;
			}

			float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;

			// "Нативный" размер в GUI-поинтах (1:1 по экранным пикселям)
			float nativeW = tex.width / pixelsPerPoint;
			float nativeH = tex.height / pixelsPerPoint;

			float availW = position.width - 40f;
			float desiredW = drawAtNativeSize ? nativeW : availW;

			if (maxWidth > 0f)
				desiredW = Mathf.Min(desiredW, maxWidth);

			desiredW = Mathf.Min(desiredW, availW);

			float aspect = (float)tex.height / tex.width;
			float desiredH = desiredW * aspect;

			// Кликабельная область
			var rect = GUILayoutUtility.GetRect(desiredW, desiredH, GUILayout.ExpandWidth(false));
			EditorGUI.DrawPreviewTexture(rect, tex, null, ScaleMode.ScaleToFit);

			// Хинт + обработка клика
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.Zoom);

			if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
			{
				ScreenshotPreviewWindow.Show(tex, fileName);
				Event.current.Use();
			}

			EditorGUILayout.LabelField($"📸 {fileName} (click to zoom)", EditorStyles.miniLabel);
			EditorGUILayout.Space(6);
		}

		private Texture2D GetScreenshot(string fileName)
		{
			if (_screenshots.TryGetValue(fileName, out var tex))
				return tex;

			string path = $"Assets/Tools/Levels/Editor/Screenshots/{fileName}";
			tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

			_screenshots[fileName] = tex;
			return tex;
		}

		#endregion
	}

	internal sealed class ScreenshotPreviewWindow : EditorWindow
	{
		private Texture2D _tex;
		private Vector2 _scroll;
		private float _zoom = 1f;

		public static void Show(Texture2D tex, string title = "Screenshot Preview")
		{
			if (tex == null) return;

			var w = CreateInstance<ScreenshotPreviewWindow>();
			w._tex = tex;
			w.titleContent = new GUIContent(title);
			w.minSize = new Vector2(400, 300);
			w.ShowUtility(); // маленькое отдельное utility-окно
		}

		private void OnGUI()
		{
			if (_tex == null)
			{
				EditorGUILayout.HelpBox("No screenshot texture assigned.", MessageType.Info);
				return;
			}

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				EditorGUILayout.LabelField($"{_tex.width}×{_tex.height}px", GUILayout.Width(120));

				GUILayout.FlexibleSpace();

				EditorGUILayout.LabelField("Zoom", GUILayout.Width(40));
				_zoom = GUILayout.HorizontalSlider(_zoom, 0.1f, 4f, GUILayout.Width(180));
				_zoom = Mathf.Round(_zoom * 100f) / 100f;

				if (GUILayout.Button("1:1", EditorStyles.toolbarButton, GUILayout.Width(40)))
					_zoom = 1f;

				if (GUILayout.Button("Fit", EditorStyles.toolbarButton, GUILayout.Width(40)))
					_zoom = 0f; // специальное значение: fit
			}

			float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;

			// Размер в GUI-поинтах для режима 1:1 (чтобы 1 GUI point = 1 экранный пиксель)
			// На ретине pixelsPerPoint=2, значит tex.width/2 поинтов даст 1:1 по экранным пикселям.
			Vector2 nativeSizePoints = new Vector2(_tex.width / pixelsPerPoint, _tex.height / pixelsPerPoint);

			// Fit-to-window
			Vector2 targetSizePoints;
			if (_zoom <= 0f)
			{
				// Вписываем в область окна (минус чуть-чуть под тулбар)
				float availW = position.width - 20f;
				float availH = position.height - 40f;

				float scale = Mathf.Min(availW / nativeSizePoints.x, availH / nativeSizePoints.y);
				scale = Mathf.Clamp(scale, 0.01f, 100f);
				targetSizePoints = nativeSizePoints * scale;
			}
			else
			{
				targetSizePoints = nativeSizePoints * _zoom;
			}

			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			var rect = GUILayoutUtility.GetRect(targetSizePoints.x, targetSizePoints.y, GUILayout.ExpandWidth(false));
			EditorGUI.DrawPreviewTexture(rect, _tex, null, ScaleMode.StretchToFill);

			EditorGUILayout.EndScrollView();
		}
	}
}