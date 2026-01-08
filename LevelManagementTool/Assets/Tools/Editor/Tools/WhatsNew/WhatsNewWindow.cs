using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectName.Editor.ToolsUpdates
{
	public class WhatsNewWindow : EditorWindow
	{
		private Vector2 _scroll;
		private readonly Dictionary<string, Texture2D> _screenshots = new();

		public static void ShowWindow()
		{
			WhatsNewWindow window = GetWindow<WhatsNewWindow>("What's New");
			window.minSize = new Vector2(1200, 700);
		}

		private void OnGUI()
		{
			DrawHeader();
			DrawAutoOpenToggle();

			EditorGUILayout.Space(12);

			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			DrawSection_Intro();

			DrawSection_EntryPoint();
			DrawSection_FirstStep();
			DrawSection_TopBarButtons();

			DrawSection_LevelTable();
			DrawSection_DeleteLevels();

			DrawSection_LevelInspector();
			DrawSection_EditButtons();

			DrawSection_Validation();
			DrawSection_LevelIdMap();

			EditorGUILayout.EndScrollView();
		}

		#region Header

		private void DrawHeader()
		{
			EditorGUILayout.Space(6);

			GUIStyle style = new(EditorStyles.boldLabel)
			{
				fontSize = 18
			};

			EditorGUILayout.LabelField("Introducing the brand-new \"Level Management Tool\"!", style);
			EditorGUILayout.Space(4);

			EditorGUILayout.LabelField(
				"This update improves level authoring workflow and reduces live-ops risk related to saves." +
				"\nEven though implementing the solution for client-side saves will fall on the shoulders of our brave gameplay developers! Haha!",
				EditorStyles.wordWrappedLabel
			);
		}

		private void DrawAutoOpenToggle()
		{
			EditorGUILayout.Space(8);

			bool autoOpen = AutoOpenEditorWindow.AutoOpenEnabled;
			bool newValue = EditorGUILayout.Toggle("Open this panel on Unity start", autoOpen);

			if (newValue != autoOpen)
				AutoOpenEditorWindow.AutoOpenEnabled = newValue;
		}

		#endregion

		#region Sections

		private void DrawSection_Intro()
		{
			DrawScreenshotHint("0.0.LevelManagementTool.png");

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

			DrawScreenshotHint("1.0.HowToOpenLevelManagementTool.png");
		}

		private void DrawSection_FirstStep()
		{
			DrawSectionTitle("Level Database");

			DrawParagraph("To start editing levels, you need to select a LevelDatabase.");

			DrawScreenshotHint("2.0.SelectLevelDataBase.png");
		}

		private void DrawSection_TopBarButtons()
		{
			DrawSectionTitle("Top Bar Buttons");

			DrawParagraph("At the top of the panel, you can find the following useful buttons:");
			DrawBullet("Refresh" +
					   "\nRefreshes the panel to reflect the current state of the Level Database." +
					   "\n(Mostly technical, usually not needed.)");
			DrawBullet("Sync DB" +
					   "\nSynchronizes the Level Database and adds duplicated levels found in the project." +
					   "\nNot recommended — always create levels through this panel.");
			DrawBullet("Export ID Map" +
					   "\nExports a new Level Database with fixed order and unique static IDs." +
					   "\nUsed by gameplay systems to save player progress.");
			DrawBullet("Create Level" +
					   "\nCreates a new level and automatically adds it to the Level Database." +
					   "\nFully supports Ctrl + Z / Undo | Ctrl + Y / Redo.");
			DrawBullet("Validate" +
					   "\nRuns manual validation." +
					   "\n(Validation also runs automatically on every change.)");

			DrawScreenshotHint("3.0.TopBarButtons.png");

			DrawBullet("Search field" +
					   "\nSearch levels by name");

			DrawScreenshotHint("3.1.Search.png");

			DrawBullet("Create Options" +
					   "\nConfigure initial data, auto-naming, and number of levels to create");

			DrawScreenshotHint("3.3.CreateLevel_CreateOptions.png");
			DrawScreenshotHint("3.2.CreateLevel_CreateOptions.png");
		}

		private void DrawSection_DeleteLevels()
		{
			DrawSectionTitle("Safe Level Deletion");

			DrawBullet("Remove levels directly from the tool");
			DrawBullet("Assets and database links are handled automatically");
			DrawBullet("Fully supports Ctrl + Z / Undo | Ctrl + Y / Redo.");

			DrawScreenshotHint("4.1.LevelsPreviewTable_Multiselect_Delete.png");
		}

		private void DrawSection_LevelTable()
		{
			DrawSectionTitle("Centralized Level Table");

			DrawScreenshotHint("4.0.LevelsPreviewTable.png");

			DrawBullet("Drag & drop table with all levels in the database" +
					   "\nReordering rows updates the database order");

			DrawScreenshotHint("5.0.LevelTableDragNDrop.png");
		}

		private void DrawSection_LevelInspector()
		{
			DrawSectionTitle("Level Inspector");

			DrawScreenshotHint("5.0.LevelInspector.png");

			DrawBullet("Level Inspector" +
					   "\nReal-time inspection and editing with full Undo / Redo support");

			DrawScreenshotHint("5.0.LevelInspector_Single_Multiple.png");
		}

		private void DrawSection_EditButtons()
		{
			DrawSectionTitle("Edit Buttons (Levels and Goals)");

			DrawScreenshotHint("6.0.EditButtons.png");

			DrawBullet("Allow you to edit multiple levels at once." +
					   "\nYou can apply the same values to all levels or edit each level individually.");

			DrawScreenshotHint("6.1.EditLevels_SameForSelected.png");
			DrawScreenshotHint("6.2.EditLevels_Individual.png");
			DrawScreenshotHint("6.3.EditGoals_SameForSelected.png");
			DrawScreenshotHint("6.4.EditGoals_Individual.png");
		}

		private void DrawSection_Validation()
		{
			DrawSectionTitle("Validation & Issue Surfacing");

			DrawBullet("Issue Filters" +
					   "\nallow you to display only problematic files.");

			DrawScreenshotHint("7.0.TableFilters.png");

			DrawBullet("Validation covers both Levels and Goals");
			DrawBullet("Problems are surfaced in multiple places to prevent mistakes slipping through");
			DrawParagraph("Where you’ll see validation feedback:");

			DrawBullet("Bottom panel message summarizing current issues");
			DrawBullet("Issues column in the table highlights problematic rows");
			DrawBullet("Inspector shows a detailed message for the selected level(s)");
			DrawBullet("Multi-selection shows which files have which issues");

			DrawScreenshotHint("7.0.TableFilters_Issues.png");
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

		private void DrawSectionTitle(string sectionTitle)
		{
			EditorGUILayout.Space(14);
			EditorGUILayout.LabelField(sectionTitle, EditorStyles.boldLabel);
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
			GUIStyle style = new(EditorStyles.helpBox)
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
			Texture2D tex = GetScreenshot(fileName);
			if (!tex)
			{
				EditorGUILayout.HelpBox($"Screenshot not found: {fileName}", MessageType.Warning);
				return;
			}

			float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;

			float nativeW = tex.width / pixelsPerPoint;

			float availW = position.width - 40f;
			float desiredW = drawAtNativeSize ? nativeW : availW;

			if (maxWidth > 0f)
				desiredW = Mathf.Min(desiredW, maxWidth);

			desiredW = Mathf.Min(desiredW, availW);

			float aspect = (float)tex.height / tex.width;
			float desiredH = desiredW * aspect;

			Rect rect = GUILayoutUtility.GetRect(desiredW, desiredH, GUILayout.ExpandWidth(false));
			EditorGUI.DrawPreviewTexture(rect, tex, null, ScaleMode.ScaleToFit);

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
			if (_screenshots.TryGetValue(fileName, out Texture2D tex))
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
			if (!tex)
				return;

			ScreenshotPreviewWindow w = CreateInstance<ScreenshotPreviewWindow>();
			w._tex = tex;
			w.titleContent = new GUIContent(title);
			w.minSize = new Vector2(400, 300);
			w.ShowUtility();
		}

		private void OnGUI()
		{
			if (!_tex)
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
					_zoom = 0f;
			}

			float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;

			Vector2 nativeSizePoints = new(_tex.width / pixelsPerPoint, _tex.height / pixelsPerPoint);

			Vector2 targetSizePoints;
			if (_zoom <= 0f)
			{
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

			Rect rect = GUILayoutUtility.GetRect(targetSizePoints.x, targetSizePoints.y, GUILayout.ExpandWidth(false));
			EditorGUI.DrawPreviewTexture(rect, _tex, null, ScaleMode.StretchToFill);

			EditorGUILayout.EndScrollView();
		}
	}
}