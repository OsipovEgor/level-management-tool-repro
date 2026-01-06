#if UNITY_EDITOR
using System;

namespace Game.Levels.EditorTool
{
	[Serializable]
	public class LevelAssetRecord
	{
		public string assetPath;
		public string json;

		public string databasePath;
		public int databaseIndex = -1;
	}
}
#endif