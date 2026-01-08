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
		[Min(0)] public int Target;
		public string Tag;
	}
}