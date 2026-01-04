#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public class LevelAssetUndoBridge : ScriptableObject
	{
		public List<LevelAssetRecord> created = new();

		public List<LevelAssetRecord> deleted = new();
	}
}
#endif