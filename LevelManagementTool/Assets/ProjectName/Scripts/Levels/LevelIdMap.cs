using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
	[Serializable]
	public struct LevelIdMapEntry
	{
		public int Index;
		public string StableId;
		public LevelConfig Level;
	}

	[CreateAssetMenu(menuName = "Game/Levels/Level Id Map", fileName = "LevelIdMap")]
	public class LevelIdMap : ScriptableObject
	{
		public List<LevelIdMapEntry> entries = new List<LevelIdMapEntry>();
	}
}