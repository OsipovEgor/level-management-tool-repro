using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(menuName = "Game/Levels/Level Database", fileName = "LevelDatabase")]
	public class LevelDatabase : ScriptableObject
	{
		[Tooltip("Ordered list used for progression / UI. Identity must NOT rely on this order.")]
		public List<LevelConfig> orderedLevels = new();

		public int Count => orderedLevels?.Count ?? 0;

		public LevelConfig GetByIndex(int index)
		{
			if (orderedLevels == null || index < 0 || index >= orderedLevels.Count) return null;
			return orderedLevels[index];
		}
	}
}