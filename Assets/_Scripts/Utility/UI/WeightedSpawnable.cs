using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace CosmicShore.Utility.UI
{
	/// <summary>
	/// A pair of values. Spawnable types, with each one attached to
	/// to the proportional weight it should carry.
	/// </summary>
	[Serializable]
	public class WeightedSpawnable {
		public WeightedSpawnable(SpawnableAbstractBase spawnable, float weight)
		{
			Spawnable = spawnable;
			Weight = weight;
		}

		/// <summary>
		/// The first value stored in this object.
		/// Will be displayed on the left in the editor.
		/// </summary>
		public SpawnableAbstractBase Spawnable = null;

		/// <summary>
		/// The second value stored in the object.
		/// Will be displayed on the right in the editor.
		/// </summary>
		public float Weight = 0f;
	}
}
