using CosmicShore.Game.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CosmicShore.Game.AI;
using CosmicShore.Game.Projectiles;
using CosmicShore.Integrations.Enums;
using CosmicShore.Models.ScriptableObjects;
using QFSW.QC.Actions;

namespace CosmicShore.Core
{
    // TODO: Why are these structs and not classes?
    // I ask because they seem to be structs in the C sense:
    // sort of like classes with no functions. These also
    // contain a reference to a List<>, which is a class.
    // So they're structs that contain a class. I don't get
    // it.
    [Serializable]
    public struct InputEventShipActionMapping
    {
        public InputEvents InputEvent;
        public List<ShipAction> ShipActions;
    }

    [Serializable]
    public struct ResourceEventShipActionMapping
    {
        public ResourceEvents ResourceEvent;
        public List<ShipAction> ClassActions;
    }

    [RequireComponent(typeof(ResourceSystem))]
    [RequireComponent(typeof(TrailSpawner))]
    [RequireComponent(typeof(ShipStatus))]
    public class Ship : MonoBehaviour
    {
        readonly Dictionary<TrailBlockImpactEffects, Action<Ship, TrailBlockProperties>> ImpactTrailBlockActions = new()
        {
            {
                TrailBlockImpactEffects.PlayHaptics,
                (ship, trailBlockProperties) => {
                    if (!ship.ShipStatus.AutoPilotEnabled)
                    {
                        HapticController.PlayHaptic(HapticType.BlockCollision);
                    }
                }
            },
            {
                TrailBlockImpactEffects.DrainHalfAmmo,

                (ship, trailBlockProperties) => {
                    ship.ResourceSystem.ChangeAmmoAmount(-ship.ResourceSystem.CurrentAmmo / 2f);
                }
            },
            {
                TrailBlockImpactEffects.DebuffSpeed,
                (ship, trailBlockProperties) => {
                    ship.ShipTransformer.ModifyThrottle(
                        trailBlockProperties.speedDebuffAmount, ship.speedModifierDuration);
                }
            },
            {
                TrailBlockImpactEffects.OnlyBuffSpeed,
                (ship, trailBlockProperties) => {
                    if (trailBlockProperties.speedDebuffAmount > 1)
                    {
                        ship.ShipTransformer.ModifyThrottle(
                            trailBlockProperties.speedDebuffAmount, ship.speedModifierDuration);
                    }
                }
            },
            {
                TrailBlockImpactEffects.ChangeBoost,
                (ship, trailBlockProperties) => {
                    ship.ResourceSystem.ChangeBoostAmount(ship.BlockChargeChange);
                }
            },
            {
                TrailBlockImpactEffects.Attach, (ship, trailBlockProperties) => {
                    ship.Attach(trailBlockProperties.trailBlock);
                    ship.ShipStatus.GunsActive = true;
                }
            },
            {
                TrailBlockImpactEffects.ChangeAmmo, (ship, trailBlockProperties) => {
                    ship.ResourceSystem.ChangeAmmoAmount(ship.BlockChargeChange);
                }
            },
            {
                TrailBlockImpactEffects.Bounce, (ship, trailBlockProperties) => {
                    var cross = Vector3.Cross(ship.transform.forward, trailBlockProperties.trailBlock.transform.forward);
                    var normal = Quaternion.AngleAxis(90, cross) * trailBlockProperties.trailBlock.transform.forward;
                    var reflectForward = Vector3.Reflect(ship.transform.forward, normal);
                    var reflectUp = Vector3.Reflect(ship.transform.up, normal);
                    ship.ShipTransformer.GentleSpinShip(reflectForward, reflectUp, 1);
                    ship.ShipTransformer.ModifyVelocity(
                        (ship.transform.position - trailBlockProperties.trailBlock.transform.position).normalized * 5,
                        Time.deltaTime * 15);
                }
            },
            {
                TrailBlockImpactEffects.Explode, (ship, trailBlockProperties) => {
                    trailBlockProperties.trailBlock.Explode(
                        ship.ShipStatus.Course * ship.ShipStatus.Speed, ship.Team, ship.Player.PlayerName);
                }
            },
        };

        readonly Dictionary<CrystalImpactEffects, Action<Ship, CrystalProperties>> CrystalImpactActions = new()
        {
            { CrystalImpactEffects.AreaOfEffectExplosion, (ship, crystalProperties) =>
                {
                    var AOEExplosion = Instantiate(ship.AOEPrefab).GetComponent<AOEExplosion>();
                    AOEExplosion.Ship = ship;
                    AOEExplosion.SetPositionAndRotation(ship.transform.position, ship.transform.rotation);
                    AOEExplosion.MaxScale =  Mathf.Lerp(ship.minExplosionScale, ship.maxExplosionScale, ship.ResourceSystem.CurrentAmmo);
                }
            },
            { CrystalImpactEffects.IncrementLevel, (ship, crystalProperties) =>
                {
                     // TODO: consider removing here and leaving this up to the crystals.
                    ship.ResourceSystem.IncrementLevel(crystalProperties.Element);
                }
            },
            { CrystalImpactEffects.FillCharge, (ship, crystalProperties) =>
                {
                     ship.ResourceSystem.ChangeBoostAmount(crystalProperties.fuelAmount);
                }
            },
            { CrystalImpactEffects.Boost, (ship, crystalProperties) =>
                {
                    ship.ShipTransformer.ModifyThrottle(crystalProperties.speedBuffAmount, 4 * ship.speedModifierDuration);
                }
            },
            { CrystalImpactEffects.DrainAmmo, (ship, crystalProperties) =>
                {
                    ship.ResourceSystem.ChangeAmmoAmount(-ship.ResourceSystem.CurrentAmmo);
                }
            },
            { CrystalImpactEffects.GainOneThirdMaxAmmo, (ship, crystalProperties) =>
                {
                    ship.ResourceSystem.ChangeAmmoAmount(ship.ResourceSystem.MaxAmmo/3f);
                }
            },
            { CrystalImpactEffects.GainFullAmmo, (ship, crystalProperties) =>
                {
                    ship.ResourceSystem.ChangeAmmoAmount(ship.ResourceSystem.MaxAmmo);
                }
            },
        };

        // TODO: Why are so many of these public but hidden in the Inspector?
        // I understand that, that way, they can be changed from code but
        // not from the Editor. But why do it that way? (I'm not saying or
        // implying it's wrong. I'm just wondering.)
        [SerializeField] List<ImpactProperties> impactProperties;
        [HideInInspector] public CameraManager cameraManager;
        [HideInInspector] public InputController InputController;
        [HideInInspector] public ResourceSystem ResourceSystem;

        [Header("Ship Meta")]
        [SerializeField] string Name;
        [SerializeField] public ShipTypes ShipType;

        [Header("Ship Components")]
        [HideInInspector] public TrailSpawner TrailSpawner;
        [HideInInspector] public ShipTransformer ShipTransformer;
        [HideInInspector] public AIPilot AutoPilot;
        [HideInInspector] public ShipStatus ShipStatus;
        [SerializeField] Skimmer nearFieldSkimmer { get; set; }
        [SerializeField] GameObject OrientationHandle;
        [SerializeField] public List<GameObject> shipGeometries;

        [Header("Optional Ship Components")]
        [SerializeField] Silhouette Silhouette;
        [SerializeField] GameObject AOEPrefab;
        [SerializeField] Skimmer farFieldSkimmer;
        [SerializeField] public ShipCameraCustomizer ShipCameraCustomizer;
        [SerializeField] public Transform FollowTarget;

        [Header("Environment Interactions")]
        [SerializeField] public List<CrystalImpactEffects> crystalImpactEffects;
        [ShowIf(CrystalImpactEffects.AreaOfEffectExplosion)] [SerializeField] float minExplosionScale = 50; // TODO: depricate "ShowIf" once we adopt modularity
        [ShowIf(CrystalImpactEffects.AreaOfEffectExplosion)] [SerializeField] float maxExplosionScale = 400;

        [SerializeField] List<TrailBlockImpactEffects> trailBlockImpactEffects;
        [SerializeField] public float BlockChargeChange { get; set; }

        [Header("Configuration")]
        [SerializeField] public float boostMultiplier = 4f; // TODO: Move to ShipController
        [SerializeField] public float boostFuelAmount = -.01f;
        [SerializeField] bool bottomEdgeButtons = false;

        [SerializeField] List<InputEventShipActionMapping> inputEventShipActions;
        Dictionary<InputEvents, List<ShipAction>> ShipControlActions = new();

        [SerializeField] List<ResourceEventShipActionMapping> resourceEventClassActions;
        Dictionary<ResourceEvents, List<ShipAction>> ClassResourceActions = new();

        [Header("Leveling Targets")]
        [SerializeField] LevelAwareShipAction MassAbilityTarget;
        [SerializeField] LevelAwareShipAction ChargeAbilityTarget;
        [SerializeField] LevelAwareShipAction SpaceAbilityTarget;
        [SerializeField] LevelAwareShipAction TimeAbilityTarget;
        [SerializeField] LevelAwareShipAction ChargeAbility2Target;

        [Header("Passive Effects")]
        public List<ShipLevelEffects> LevelEffects;
        [SerializeField] float closeCamDistance;
        [SerializeField] float farCamDistance;

        readonly Dictionary<InputEvents, float> inputAbilityStartTimes = new();
        readonly Dictionary<ResourceEvents, float> resourceAbilityStartTimes = new();

        Material _ShipMaterial;

        public Material ShipMaterial {
            get

            {
                return _ShipMaterial;
            }
            set
            {
                _ShipMaterial = value;
                ApplyShipMaterial();

            }
        }

        [HideInInspector] public Material AOEExplosionMaterial { get; set; }
        [HideInInspector] public Material AOEConicExplosionMaterial { get; set; }
        [HideInInspector] public Material SkimmerMaterial { get; set; }
        public readonly float speedModifierDuration = 2f;

        SO_Guide _guide;

        // Guide and guide upgrade properties
        public SO_Guide Guide
        {
            get
            {
                return _guide;
            }
            set
            {
                _guide = value;
                ResourceSystem.InitialChargeLevel = Guide.InitialCharge;
                ResourceSystem.InitialMassLevel = Guide.InitialMass;
                ResourceSystem.InitialSpaceLevel = Guide.InitialSpace;
                ResourceSystem.InitialTimeLevel = Guide.InitialTime;

            }
        }

        private bool IsUsingGamepad
        {
            get {
                return UnityEngine.InputSystem.Gamepad.current != null;
            }
        }

        private Dictionary<Element, SO_GuideUpgrade> _guideUpgrades;
        public Dictionary<Element, SO_GuideUpgrade> GuideUpgrades
        {
            get => _guideUpgrades;
            set
            {
                _guideUpgrades = value;

                if (_guideUpgrades != null)
                {
                    UpdateLevel(Element.Charge, ResourceSystem.GetLevel(Element.Charge));
                    UpdateLevel(Element.Time, ResourceSystem.GetLevel(Element.Time));
                    UpdateLevel(Element.Mass, ResourceSystem.GetLevel(Element.Mass));
                    UpdateLevel(Element.Space, ResourceSystem.GetLevel(Element.Space));
                }
            }
        }

        Teams team;
        public Teams Team
        {
            get => team;
            set
            {
                team = value;
                if (nearFieldSkimmer != null) nearFieldSkimmer.team = value;
                if (farFieldSkimmer != null) farFieldSkimmer.team = value;
            }
        }

        Player player;
        public Player Player
        {
            get => player;
            set
            {
                player = value;
                if (nearFieldSkimmer != null) nearFieldSkimmer.Player = value;
                if (farFieldSkimmer != null) farFieldSkimmer.Player = value;
            }
        }

        void Awake()
        {
            ResourceSystem = GetComponent<ResourceSystem>();
            ShipTransformer = GetComponent<ShipTransformer>();
            TrailSpawner = GetComponent<TrailSpawner>();
            ShipStatus = GetComponent<ShipStatus>();

            // TODO: P1 GOES AWAY
            ResourceSystem.OnElementLevelChange += UpdateLevel;
        }

        void Start()
        {
            cameraManager = CameraManager.Instance;
            InputController = player.GetComponent<InputController>();
            AutoPilot = GetComponent<AIPilot>();
            if (!FollowTarget) FollowTarget = transform;
            if (bottomEdgeButtons) Player.GameCanvas.MiniGameHUD.PositionButtonPanel(true);

            for (int i = 0; i < shipGeometries.Count; i++)
                shipGeometries[i].AddComponent<ShipGeometry>().Ship = this;

            for (int i = 0; i < inputEventShipActions.Count; i++)
            {
                if (!ShipControlActions.ContainsKey(inputEventShipActions[i].InputEvent))
                {
                    ShipControlActions.Add(inputEventShipActions[i].InputEvent, inputEventShipActions[i].ShipActions);
                }
                else
                {
                    ShipControlActions[inputEventShipActions[i].InputEvent].AddRange(inputEventShipActions[i].ShipActions);
                }
            }

            for (int i = 0; i < ShipControlActions.Count; i++)
            {
                var shipControlAction = ShipControlActions.ElementAt(i).Value;
                for (int j = 0; j < shipControlAction.Count; j++)
                {
                    shipControlAction[j].Ship = this;
                }
            }

            for (int i = 0; i < resourceEventClassActions.Count; i++)
            {
                if (!ClassResourceActions.ContainsKey(resourceEventClassActions[i].ResourceEvent))
                {
                    ClassResourceActions.Add(
                        resourceEventClassActions[i].ResourceEvent, resourceEventClassActions[i].ClassActions);
                }
                else
                {
                    ClassResourceActions[resourceEventClassActions[i].
                        ResourceEvent].AddRange(resourceEventClassActions[i].ClassActions);
                }
            }

            for (int i = 0; i < ClassResourceActions.Count; i++)
            {
                List<ShipAction> shipAction = ClassResourceActions.ElementAt(i).Value;
                for (int j = 0; j < shipAction.Count; j++)
                {
                    shipAction[j].Ship = this;
                }
            }

            if (!AutoPilot.AutoPilotEnabled)
            {
                if (ShipControlActions.ContainsKey(InputEvents.Button1Action))
                {
                    Player.GameCanvas.MiniGameHUD.SetButtonActive(!IsUsingGamepad, 1);
                }

                if (ShipControlActions.ContainsKey(InputEvents.Button2Action))
                {
                    Player.GameCanvas.MiniGameHUD.SetButtonActive(!IsUsingGamepad, 2);
                }

                if (ShipControlActions.ContainsKey(InputEvents.Button3Action))
                {
                    Player.GameCanvas.MiniGameHUD.SetButtonActive(!IsUsingGamepad, 3);
                }
            }
        }

        [Serializable] public struct ElementStat
        {
            public string StatName;
            public Element Element;

            public ElementStat(string statname, Element element)
            {
                StatName = statname;
                Element = element;
            }
        }

        [SerializeField] List<ElementStat> ElementStats = new List<ElementStat>();
        public void NotifyElementalFloatBinding(string statName, Element element)
        {
            Debug.Log($"Ship.NotifyShipStatBinding - statName:{statName}, element:{element}");
            if (!ElementStats.Where(x => x.StatName == statName).Any())
            {
                ElementStats.Add(new ElementStat(statName, element));
            }

            Debug.Log($"Ship.NotifyShipStatBinding - ElementStats.Count:{ElementStats.Count}");
        }

        public void PerformCrystalImpactEffects(CrystalProperties crystalProperties)
        {
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.CrystalCollected(this, crystalProperties);
            }

            foreach (CrystalImpactEffects effect in crystalImpactEffects)
            {
                CrystalImpactActions[effect](this, crystalProperties);
            }
        }

        public void PerformTrailBlockImpactEffects(TrailBlockProperties trailBlockProperties)
        {
            for (int i = 0; i < trailBlockImpactEffects.Count; i++)
            {
                ImpactTrailBlockActions[trailBlockImpactEffects[i]](this, trailBlockProperties);
            }
        }

        public void PerformShipControllerActions(InputEvents controlType)
        {
            if (!inputAbilityStartTimes.ContainsKey(controlType))
            {
                inputAbilityStartTimes.Add(controlType, Time.time);
            }
            else
            {
                inputAbilityStartTimes[controlType] = Time.time;
            }

            if (ShipControlActions.ContainsKey(controlType))
            {
                var shipControlActions = ShipControlActions[controlType];
                foreach (var action in shipControlActions)
                {
                    action.StartAction();
                }
            }
        }

        public void StopShipControllerActions(InputEvents controlType)
        {
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.AbilityActivated(
                    Team, player.PlayerName, controlType, Time.time-inputAbilityStartTimes[controlType]);
            }

            if (ShipControlActions.ContainsKey(controlType))
            {
                var shipControlActions = ShipControlActions[controlType];
                foreach (var action in shipControlActions)
                {
                    action.StopAction();
                }
            }
        }

        public void PerformClassResourceActions(ResourceEvents resourceEvent)
        {
            if (!resourceAbilityStartTimes.ContainsKey(resourceEvent))
            {
                resourceAbilityStartTimes.Add(resourceEvent, Time.time);
            }
            else
            {
                resourceAbilityStartTimes[resourceEvent] = Time.time;
            }

            if (ClassResourceActions.ContainsKey(resourceEvent))
            {
                var classResourceActions = ClassResourceActions[resourceEvent];
                foreach (var action in classResourceActions)
                {
                    action.StartAction();
                }
            }
        }

        public void StopClassResourceActions(ResourceEvents resourceEvent)
        {
            if (ClassResourceActions.ContainsKey(resourceEvent))
            {
                var classResourceActions = ClassResourceActions[resourceEvent];
                foreach (var action in classResourceActions)
                {
                    action.StopAction();
                }
            }
        }

        public void UpdateLevel(Element element, int upgradeLevel)
        {
            Debug.Log($"Ship: UpdateLevel: element{element}, upgradeLevel: {upgradeLevel}");
            if (GuideUpgrades == null)
            {
                GuideUpgrades = new();
            }

            if (GuideUpgrades.ContainsKey(element))
            {
                GuideUpgrades[element].element = element;
                GuideUpgrades[element].upgradeLevel = upgradeLevel;
            }
            else
            {
                // TODO: preset individual upgrade properties such as name, description, icon etc based on upgrade properties.
                var newUpgrade = ScriptableObject.CreateInstance<SO_GuideUpgrade>();
                newUpgrade.element = element;
                newUpgrade.upgradeLevel = upgradeLevel;
                GuideUpgrades.TryAdd(element, newUpgrade);
            }

            #if UNITY_EDITOR
            foreach (var upgrade in GuideUpgrades)
            {
                Debug.LogFormat("{0} - {1}: element: {2} upgrade level: {3}", nameof(GuideUpgrades), nameof(UpdateLevel), upgrade.Key, upgrade.Value.upgradeLevel.ToString());
            }
            #endif
        }


        public void SetBlockMaterial(Material material)
        {
            TrailSpawner.SetBlockMaterial(material);
        }

        public void SetBlockSilhouettePrefab(GameObject prefab)
        {
            if (Silhouette)
            {
                Silhouette.SetBlockPrefab(prefab);
            }
        }

        public void SetShieldedBlockMaterial(Material material)
        {
            TrailSpawner.SetShieldedBlockMaterial(material);
        }

        public void FlipShipUpsideDown() // TODO: move to shipController
        {
            OrientationHandle.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }

        public void FlipShipRightsideUp()
        {
            OrientationHandle.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        public void SetShipUp(float angle)
        {
            OrientationHandle.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        public void Teleport(Transform targetTransform)
        {
            transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
        }

        // TODO: need to be able to disable ship abilities as well for minigames
        public void DisableSkimmer()
        {
            if (nearFieldSkimmer != null)
            {
                nearFieldSkimmer.gameObject.SetActive(false);
            }
            if (farFieldSkimmer != null)
            {
                farFieldSkimmer.gameObject.SetActive(false);
            }
        }

        void ApplyShipMaterial()
        {
            if (ShipMaterial == null)
            {
                return;
            }

            for (int i = 0; i < shipGeometries.Count; i++)
            {
                if (shipGeometries[i].GetComponent<SkinnedMeshRenderer>() != null)
                {
                    var materials = shipGeometries[i].GetComponent<SkinnedMeshRenderer>().materials;
                    materials[2] = ShipMaterial;
                    shipGeometries[i].GetComponent<SkinnedMeshRenderer>().materials = materials;
                }
                else if (shipGeometries[i].GetComponent<MeshRenderer>() != null)
                {
                    var materials = shipGeometries[i].GetComponent<MeshRenderer>().materials;
                    materials[1] = ShipMaterial;
                    shipGeometries[i].GetComponent<MeshRenderer>().materials = materials;
                }
            }
        }

        //
        // Attach and Detach
        //
        public void Attach(TrailBlock trailBlock)
        {
            if (trailBlock.Trail != null)
            {
                ShipStatus.Attached = true;
                ShipStatus.AttachedTrailBlock = trailBlock;
            }
        }

    }
}