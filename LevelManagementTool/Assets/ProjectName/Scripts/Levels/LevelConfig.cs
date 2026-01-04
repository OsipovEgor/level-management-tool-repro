using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Levels
{
	[CreateAssetMenu(menuName = "Game/Levels/Level Config", fileName = "LevelConfig_")]
	public class LevelConfig : ScriptableObject
	{
		[SerializeField, HideInInspector] private string stableId;

		public string StableId => stableId;

		[Header("Balance")] [Min(1)] public int timeLimitSeconds = 60;
		[Min(0)] public int difficulty = 0;

		[Header("Goals (0..3)")] public List<LevelGoal> goals = new List<LevelGoal>(3);

#if UNITY_EDITOR
		private void OnValidate()
		{
			// Ensure StableId exists and stays stable.
			if (string.IsNullOrEmpty(stableId))
			{
				stableId = Guid.NewGuid().ToString("N");
				EditorUtility.SetDirty(this);
			}

			// Clamp goals count to 0..3
			if (goals != null && goals.Count > 3)
				goals.RemoveRange(3, goals.Count - 3);
		}
#endif
	}
}