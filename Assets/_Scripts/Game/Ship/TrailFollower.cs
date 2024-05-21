
using UnityEngine;

namespace CosmicShore.Core
{
	// TODO: move to enum folder
	public enum TrailFollowerDirection
	{
		Forward = 1,
		Backward = -1,
	}

	public class TrailFollower : MonoBehaviour
	{
		int attachedBlockIndex;
		Trail attachedTrail;
		Teams team;

		public bool IsAttached { get { return attachedTrail != null; } }
		public TrailBlock AttachedTrailBlock { get { return attachedTrail.GetBlock(attachedBlockIndex); } }

		ShipStatus shipData;
		Ship ship;

		void Start()
		{
			// TODO: find a better way of setting team that doesn't assume a ship
			ship = GetComponent<Ship>();
			team = ship.Team;
			shipData = GetComponent<ShipStatus>();
		}
	}
}