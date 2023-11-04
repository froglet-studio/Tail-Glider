﻿using System.Collections;
using System.Collections.Generic;
using _Scripts._Core.Input;
using StarWriter.Core;
using UnityEngine;

namespace _Scripts._Core.Ship.Projectiles
{
    public class AOEExplosion : MonoBehaviour
    {
        [HideInInspector] public float speed;

        protected const float PI_OVER_TWO = Mathf.PI / 2;
        protected Vector3 MaxScaleVector;

        [HideInInspector] public float MaxScale = 200f;

        [Header("Explosion Settings")]
        [SerializeField] protected float ExplosionDuration = 2f;
        [SerializeField] protected float ExplosionDelay = 0.2f;

        [Header("Impact Effects")]
        [SerializeField] private List<ShipImpactEffects> shipImpactEffects;
        [SerializeField] private bool affectSelf = false;
        [SerializeField] private bool destructive = true;
        [SerializeField] private bool devastating = false;

        protected static GameObject container;

        // Material and Team
        [HideInInspector] public Material Material { get; set; }
        [HideInInspector] public Teams Team;
        [HideInInspector] public StarWriter.Core.Ship Ship;
        [HideInInspector] public bool AnonymousExplosion;

        protected virtual void Start()
        {
            InitializeProperties();
            StartCoroutine(ExplodeCoroutine());
        }

        private void InitializeProperties()
        {
            speed = MaxScale / ExplosionDuration;
            if (container == null) container = new GameObject("AOEContainer");

            if (Team == Teams.Unassigned)
                Team = Ship.Team;
            if (Material == null)
                Material = new Material(Ship.AOEExplosionMaterial);

            // SetParent with false to take container's world position
            transform.SetParent(container.transform, worldPositionStays: false);
            MaxScaleVector = new Vector3(MaxScale, MaxScale, MaxScale);
            
            StartCoroutine(ExplodeCoroutine());
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            var impactVector = (other.transform.position - transform.position).normalized * speed;

            if (other.TryGetComponent<TrailBlock>(out var trailBlock))
            {
                if ((trailBlock.Team == Team && !affectSelf) || !destructive)
                {
                    trailBlock.ActivateShield(2f);
                    return;
                }

                if (AnonymousExplosion)
                    trailBlock.Explode(impactVector, Teams.None, "🔥GuyFawkes🔥", devastating);
                else
                    trailBlock.Explode(impactVector, Ship.Team, Ship.Player.PlayerName, devastating);
            }
            if (other.TryGetComponent<ShipGeometry>(out var shipGeometry))
            {
                if (shipGeometry.Ship.Team == Team && !affectSelf)
                    return;

                PerformShipImpactEffects(shipGeometry, impactVector);
            }
        }

        protected virtual IEnumerator ExplodeCoroutine()
        {
            yield return new WaitForSeconds(ExplosionDelay);

            if (TryGetComponent<MeshRenderer>(out var meshRenderer))
                meshRenderer.material = Material;

            var elapsedTime = 0f;
            while (elapsedTime < ExplosionDuration)
            {
                elapsedTime += Time.deltaTime;
                var easing = Mathf.Sin((elapsedTime / ExplosionDuration) * PI_OVER_TWO);
                transform.localScale = Vector3.Lerp(Vector3.zero, MaxScaleVector, easing);
                Material.SetFloat("_Opacity", 1 - easing);
                yield return null;
            }

            Destroy(gameObject);
        }

        protected virtual void PerformShipImpactEffects(ShipGeometry shipGeometry, Vector3 impactVector)
        {
            foreach (ShipImpactEffects effect in shipImpactEffects)
            {
                switch (effect)
                {
                    case ShipImpactEffects.TrailSpawnerCooldown:
                        shipGeometry.Ship.TrailSpawner.PauseTrailSpawner();
                        shipGeometry.Ship.TrailSpawner.RestartTrailSpawnerAfterDelay(10);
                        break;
                    case ShipImpactEffects.PlayHaptics:
                        if (!shipGeometry.Ship.ShipStatus.AutoPilotEnabled)
                            HapticController.PlayHaptic(HapticType.ShipCollision);
                        break;
                    case ShipImpactEffects.SpinAround:
                        shipGeometry.Ship.ShipController.SpinShip(impactVector);
                        break;
                    case ShipImpactEffects.Knockback:
                        if (shipGeometry.Ship.Team == Team)
                        {
                            shipGeometry.Ship.ShipController.ModifyVelocity(impactVector * 100, 2);
                            shipGeometry.Ship.ShipController.ModifyThrottle(1.012f, 6); // TODO: the magic number here is needed for the Time Grizzly
                        }
                        else shipGeometry.Ship.ShipController.ModifyVelocity(impactVector * 100, 3);
                        //shipGeometry.Ship.transform.localPosition += impactVector / 2f;
                        break;
                    case ShipImpactEffects.Stun:
                        shipGeometry.Ship.ShipController.ModifyThrottle(.1f, 2);
                        break;
                }
            }
        }

        public virtual void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
