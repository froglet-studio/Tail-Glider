using UnityEngine;

namespace CosmicShore.Core
{
    [System.Serializable]
    public struct TrailBlockProperties
    {
        public Vector3 position;
        public float volume;
        public float speedDebuffAmount; // don't use more than two sig figs, see ship.DebuffSpeed
        public TrailBlock trailBlock;
        public int Index;
        public Trail Trail;
        public bool Shielded;
        public bool IsSuperShielded;
        public float TimeCreated;
    }
}