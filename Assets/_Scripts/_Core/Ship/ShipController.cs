using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarWriter.Core;
using StarWriter.Core.Input;


public class ShipController : MonoBehaviour
{
    #region Ship
    protected Ship ship;
    ShipAnimation shipAnimation;
    protected ShipData shipData;
    protected ResourceSystem resourceSystem;
    #endregion

    protected string uuid;
    protected InputController inputController;

    public delegate void Boost(string uuid, float amount);
    public static event Boost OnBoost;

    protected float speed;
    public float boostDecay = 1;

    public float defaultMinimumSpeed = 10f;
    public float DefaultThrottleScaler = 50;
    public float MaxBoostDecay = 10;
    public float BoostDecayGrowthRate = .03f;

    [HideInInspector] public float minimumSpeed;
    [HideInInspector] public float ThrottleScaler;

    public float rotationThrottleScaler = 0;
    public float rotationScaler = 130f;

    protected readonly float lerpAmount = 2f;
    readonly float smallLerpAmount = .7f;

    protected Quaternion displacementQuaternion;
    Quaternion inverseInitialRotation = new(0, 0, 0, 0);

    // Start is called before the first frame update
    protected void Start()
    {
        ship = GetComponent<Ship>();
        uuid = GameObject.FindWithTag("Player").GetComponent<Player>().PlayerUUID;
        shipData = ship.GetComponent<ShipData>();
        resourceSystem = ship.GetComponent<ResourceSystem>();

        minimumSpeed = defaultMinimumSpeed;
        ThrottleScaler = DefaultThrottleScaler;
        displacementQuaternion = transform.rotation;
        inputController = ship.inputController;
    }

    // Update is called once per frame
    protected void Update()
    {
        if (inputController == null) inputController = ship.inputController;

        RotateShip();
        MoveShip();
        shipData.blockRotation = transform.rotation; // TODO: movee this
    }

    protected void RotateShip()
    {
        Pitch();
        Yaw();
        Roll();

        if (inputController.isGyroEnabled && !Equals(inverseInitialRotation, new Quaternion(0, 0, 0, 0)))
        {
            // Updates GameObjects blockRotation from input device's gyroscope
            transform.rotation = Quaternion.Lerp(
                                        transform.rotation,
                                        displacementQuaternion * inputController.GetGyroRotation(),
                                        lerpAmount);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(
                                        transform.rotation,
                                        displacementQuaternion,
                                        lerpAmount);
        }
    }

    void StoreBoost()
    {
        boostDecay += BoostDecayGrowthRate;
    }

    public void EndDrift() 
    {
        ship.GetComponent<TrailSpawner>().SetDotProduct(1);
        StartCoroutine(DecayingBoostCoroutine());
    }

    IEnumerator DecayingBoostCoroutine()
    {
        shipData.BoostDecaying = true;
        while (boostDecay > 1)
        {
            boostDecay = Mathf.Clamp(boostDecay - Time.deltaTime, 1, MaxBoostDecay);
            Debug.Log(boostDecay);
            yield return null;
        }
        shipData.BoostDecaying = false;
    }

    protected void Pitch()
    {
        displacementQuaternion = Quaternion.AngleAxis(
                            inputController.YSum * -(speed * rotationThrottleScaler + rotationScaler) * Time.deltaTime,
                            transform.right) * displacementQuaternion;
    }

    protected void Roll()
    {
        displacementQuaternion = Quaternion.AngleAxis(
                            inputController.YDiff * (speed * rotationThrottleScaler + rotationScaler) * Time.deltaTime,
                            transform.forward) * displacementQuaternion;
    }


    protected virtual void Yaw()  // These need to not use *= ... remember quaternions are not commutative
    {
        displacementQuaternion = Quaternion.AngleAxis(
                            inputController.XSum * (speed * rotationThrottleScaler + rotationScaler) *
                                (Screen.currentResolution.width / Screen.currentResolution.height) * Time.deltaTime,
                            transform.up) * displacementQuaternion;
    }

    protected virtual void MoveShip()
    {
        float boostAmount = 1f;
        if (shipData.Boosting && resourceSystem.CurrentCharge > 0) // TODO: if we run out of fuel while full speed and straight the ship data still thinks we are boosting
        {
            boostAmount = ship.boostMultiplier;
            OnBoost?.Invoke(uuid, ship.boostFuelAmount);
        }
        if (shipData.BoostDecaying) boostAmount *= boostDecay;
        speed = Mathf.Lerp(speed, inputController.XDiff * ThrottleScaler * boostAmount + minimumSpeed, lerpAmount * Time.deltaTime);

        // Move ship velocityDirection
        shipData.InputSpeed = speed;
        

        if (!shipData.Drifting)
        {
            shipData.VelocityDirection = transform.forward;
        }
        else
        {
            StoreBoost();
            ship.GetComponent<TrailSpawner>().SetDotProduct(Vector3.Dot(shipData.VelocityDirection, transform.forward));
        }

        transform.position += shipData.Speed * Time.deltaTime * shipData.VelocityDirection;

    }

    protected void InvokeBoost(float amount)
    {
        OnBoost?.Invoke(uuid, amount);
    }
}