using System;
using UnityEngine;

namespace Game.Levels
{
	public enum GoalType
	{
		Collect,
		ReachSize,
		Destroy,
		Survive
	}

	[Serializable]
	public struct LevelGoal
	{
		public GoalType Type;

		[Min(1)] public int Target;

		// Optional discriminator (e.g. "Cars", "Trees", etc.)
		public string Tag;
	}
}