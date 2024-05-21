using CosmicShore.Core;
using System.Collections.Generic;

using UnityEngine;



public abstract class SpawnableAbstractBase : MonoBehaviour
{
    protected System.Random rng = new System.Random();
    protected List<Trail> trails = new List<Trail>();


    public abstract GameObject Spawn();

    protected virtual void CreateBlock(Vector3 position, Vector3 lookPosition, string blockId, Trail trail, Vector3 scale, TrailBlock trailBlock, GameObject container, Teams team = Teams.Blue)
    {
        var Block = Instantiate(trailBlock);
        Block.Team = team;
        Block.ownerId = "public";
        Block.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookPosition - position));
        Block.transform.SetParent(container.transform, false);
        Block.ID = blockId;
        Block.TargetScale = scale;
        Block.Trail = trail;
        trail.Add(Block);
    }
}