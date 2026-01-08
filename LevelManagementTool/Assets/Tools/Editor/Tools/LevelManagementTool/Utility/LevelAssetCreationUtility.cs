#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public static class LevelAssetCreationUtility
	{
		public const string DefaultLevelsFolder = "Assets/ProjectName/Levels/Content";
		private static readonly Regex IndexTokenRegex = new(@"\{index(?::(?<zeros>0+))?\}", RegexOptions.Compiled);

		public static string GetUniquePath(string folder, string baseName = "LevelConfig_New")
		{
			EnsureFolderExists(folder);
			string raw = $"{folder}/{baseName}.asset";
			return AssetDatabase.GenerateUniqueAssetPath(raw);
		}

		private static void EnsureFolderExists(string folderPath)
		{
			if (string.IsNullOrEmpty(folderPath)) return;
			if (AssetDatabase.IsValidFolder(folderPath)) return;

			string parent = "Assets";
			string relative = folderPath.StartsWith("Assets/") ? folderPath.Substring("Assets/".Length) : folderPath;
			string[] parts = relative.Split('/');

			foreach (string part in parts)
			{
				if (string.IsNullOrEmpty(part)) continue;

				string current = $"{parent}/{part}";
				if (!AssetDatabase.IsValidFolder(current))
					AssetDatabase.CreateFolder(parent, part);

				parent = current;
			}
		}

		public static string GenerateNextLevelName()
		{
			string template = string.IsNullOrWhiteSpace(LevelToolSettings.LevelNameTemplate)
				? "Level_{index:000}"
				: LevelToolSettings.LevelNameTemplate.Trim();

			int nextIndex = FindNextIndex(template);
			return FormatName(template, nextIndex);
		}

		private static string FormatName(string template, int index)
		{
			return IndexTokenRegex.Replace(template, m =>
			{
				string zeros = m.Groups["zeros"].Value;

				return string.IsNullOrEmpty(zeros)
					? index.ToString()
					: index.ToString(new string('0', zeros.Length));
			});
		}

		private static int FindNextIndex(string template)
		{
			int max = 0;
			List<LevelConfig> levels = LevelAssetIndex.FindAllLevels();

			string prefix = GetTemplatePrefix(template);

			Regex rx = BuildFamilyRegex(prefix);

			foreach (LevelConfig lvl in levels)
			{
				if (lvl == null) continue;

				Match match = rx.Match(lvl.name);
				if (!match.Success) continue;

				if (int.TryParse(match.Groups[1].Value, out int idx))
					max = Mathf.Max(max, idx);
			}

			return max + 1;
		}

		private static string GetTemplatePrefix(string template)
		{
			if (string.IsNullOrEmpty(template))
				return "";

			Match m = IndexTokenRegex.Match(template);
			return !m.Success
				? template
				: template.Substring(0, m.Index);
		}

		private static Regex BuildFamilyRegex(string prefix)
		{
			string escapedPrefix = Regex.Escape(prefix ?? "");
			return new Regex("^" + escapedPrefix + @"(\d+)$", RegexOptions.Compiled);
		}
	}
}
#endif