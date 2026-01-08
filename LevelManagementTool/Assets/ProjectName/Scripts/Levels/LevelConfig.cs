using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Levels
{
	public class LevelConfig : ScriptableObject
	{
		[SerializeField, HideInInspector] private string stableId;

#if UNITY_EDITOR
		public string StableId
		{
			get => stableId;
			set => stableId = value;
		}
#else
		public string StableId => stableId;
#endif //UNITY_EDITOR

		[Header("Balance")] [Min(0)] public int timeLimitSeconds = 60;
		[Min(0)] public int difficulty;

		[Header("Goals (0..3)")] public List<LevelGoal> goals = new(3);

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (string.IsNullOrEmpty(stableId))
			{
				stableId = Guid.NewGuid().ToString("N");
			}

			if (goals != null && goals.Count > 3)
				goals.RemoveRange(3, goals.Count - 3);
		}
#endif
	}
}