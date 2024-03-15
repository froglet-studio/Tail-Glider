using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Device;

namespace CosmicShore
{
    public class CommandShipTransformer : ShipTransformer
    {

        protected override void Start()
        {
            base.Start();
            ship.ShipStatus.CommandStickControls = true;
            speed = .1f;
        }

        protected override void MoveShip()
        {
            transform.position = Vector3.Lerp(transform.position, inputController.ThreeDPosition, speed * Time.deltaTime);
            shipStatus.Course = inputController.ThreeDPosition - transform.position;
        }

        protected override void RotateShip()
        {
            Quaternion newRotation = Quaternion.LookRotation(shipStatus.Course, Vector3.back);
            transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, lerpAmount * Time.deltaTime);
        }

    }
}
