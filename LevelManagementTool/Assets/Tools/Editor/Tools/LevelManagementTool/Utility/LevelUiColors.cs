#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelUiColors
	{
		public static Color SelectedBg =>
			EditorGUIUtility.isProSkin
				? new Color(0.18f, 0.38f, 0.75f, 0.28f)
				: new Color(0.25f, 0.50f, 0.95f, 0.18f);

		public static Color ErrorBg =>
			EditorGUIUtility.isProSkin
				? new Color(0.70f, 0.20f, 0.20f, 0.20f)
				: new Color(0.95f, 0.35f, 0.35f, 0.16f);

		public static Color WarnBg =>
			EditorGUIUtility.isProSkin
				? new Color(0.85f, 0.65f, 0.12f, 0.18f)
				: new Color(1.00f, 0.85f, 0.20f, 0.16f);

		/// <summary>Рисует прямоугольник фона без влияния на layout.</summary>
		public static void DrawBg(Rect rect, Color color)
		{
			if (rect.width <= 0 || rect.height <= 0) return;
			EditorGUI.DrawRect(rect, color);
		}
	}
}
#endif