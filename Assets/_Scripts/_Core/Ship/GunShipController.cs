using UnityEngine;
using StarWriter.Core;

public class GunShipController : ShipController
{
    [SerializeField] Gun topGun;
    [SerializeField] Gun leftGun;
    [SerializeField] Gun rightGun;
    [SerializeField] TrailFollower trailFollower;

    public int nextBlockIndex = 1;
    public int previousBlockIndex;
    //float trailLerpAmount;
    bool moveForward = true;
    bool attached = false;

    [SerializeField] float maxTrailSpeed = 1f;
    [SerializeField] float reducedTrailSpeed = 1f;

    //int padding = 3; 

    Player player;

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();

        topGun.Team = player.Team;
        topGun.Ship = ship;

        leftGun.Team = player.Team;
        leftGun.Ship = ship;
        
        rightGun.Team = player.Team;
        rightGun.Ship = ship;
        
    }

    protected override void Update()
    {
        base.Update();
        Fire();
    }
    protected override void MoveShip()
    {
        if (shipData.Attached && !attached)
        {
            attached = !attached;
            trailFollower.Attach(shipData.AttachedTrailBlock);
        }
        else if (!shipData.Attached && attached)
        {
            attached = !attached;
            trailFollower.Detach();
        }
     
        if (attached)
        {
            Slide();
        }
        else
        {
            base.MoveShip();
        }

    //    var velocity = (minimumSpeed - (Mathf.Abs(inputController.XSum) * ThrottleScaler)) * transform.forward + (inputController.XSum * ThrottleScaler * transform.right);
    //    shipData.VelocityDirection = velocity.normalized;
    //    shipData.InputSpeed = velocity.magnitude;
    //    transform.position += shipData.Speed * shipData.VelocityDirection * Time.deltaTime;

    }

    void Fire()
    {
        topGun.FireGun(player.transform, shipData.VelocityDirection * shipData.Speed);
        leftGun.FireGun(player.transform, shipData.VelocityDirection * shipData.Speed);
        rightGun.FireGun(player.transform, shipData.VelocityDirection * shipData.Speed);
    }
    void Slide()
    {

        //var trail = shipData.AttachedTrailBlock.Trail;

        trailFollower.Throttle = inputController.XDiff;
        trailFollower.Move();

        shipData.AttachedTrailBlock = trailFollower.AttachedTrailBlock;

        if (shipData.AttachedTrailBlock.destroyed)
            shipData.AttachedTrailBlock.Restore();

        // TODO: need to restore the below block to give player ability to change direction
        /*
        if (Vector3.Dot(transform.forward, trailFollower.Heading) < 0)
        {
            moveForward = !moveForward;

            if (moveForward)
                trailFollower.SetDirection(TrailFollowerDirection.Forward);
            else
                trailFollower.SetDirection(TrailFollowerDirection.Backward);
        }
        */

        //if (moveForward && ship.TrailSpawner.gap > 0) nextBlockIndex = shipData.AttachedTrailBlock.Index + gapStep;
        //else if (ship.TrailSpawner.gap > 0) nextBlockIndex = shipData.AttachedTrailBlock.Index - gapStep;
        //else if (moveForward) nextBlockIndex = shipData.AttachedTrailBlock.Index + 1;


        //if (moveForward) nextBlockIndex = shipData.AttachedTrailBlock.Index + 1;
        //else nextBlockIndex = shipData.AttachedTrailBlock.Index - 1;

        //var distance = trailSpawner.trailList[nextBlockIndex].transform.position - shipData.AttachedTrailBlock.transform.position;
        //var timeSpaceCorrectedInput = inputController.XDiff * Time.deltaTime / distance.magnitude;

        //if (trailSpawner.trailList[(int)nextBlockIndex].destroyed)
        //    trailLerpAmount += reducedTrailSpeed * timeSpaceCorrectedInput;
        //else
        //    trailLerpAmount += maxTrailSpeed * timeSpaceCorrectedInput;


        //transform.position = Vector3.Lerp(shipData.AttachedTrailBlock.transform.position,
        //                                  trailSpawner.trailList[nextBlockIndex].transform.position,
        //                                  trailLerpAmount);

        //if (trailLerpAmount > 1)
        //{
        //    shipData.AttachedTrailBlock = trailSpawner.trailList[nextBlockIndex];
        //    trailLerpAmount -= 1f;
        //}

        //if (nextBlockIndex < padding || nextBlockIndex > trailSpawner.trailList.Count - padding)
        //{
        //    transform.rotation *= Quaternion.Euler(0, 180, 0);
        //}






        //transform.rotation = Quaternion.Lerp(shipData.attachedTrail.TrailSpawner.trailList[previousBlockIndex].transform.rotation,
        //                                     shipData.attachedTrail.TrailSpawner.trailList[nextBlockIndex].transform.rotation,
        //                                     trailLerpAmount);

    }
}
