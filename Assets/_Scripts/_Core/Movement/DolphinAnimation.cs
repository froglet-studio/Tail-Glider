using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class DolphinAnimation : ShipAnimation
{
    [SerializeField] Transform Fusilage;

    [SerializeField] Transform LeftWing;

    [SerializeField] Transform RightWing;

    [SerializeField] Transform TailStart;

    [SerializeField] Transform TailEnd;

    [SerializeField] Transform LeftTail;

    [SerializeField] Transform RightTail;

    [SerializeField] float animationScaler = 25f;
    [SerializeField] float yawAnimationScaler = 15f;
    [SerializeField] float rollAnimationScaler = 15f;
    [SerializeField] float lerpAmount = 2f;
    [SerializeField] float smallLerpAmount = .7f;

    public override void PerformShipAnimations(float pitch, float yaw, float roll, float throttle)
    {
        // Ship animations TODO: figure out how to leverage a single definition for pitch, etc. that captures the gyro in the animations.

        AnimatePart(Fusilage,
                    -pitch * animationScaler,
                    yaw * animationScaler,
                    roll * animationScaler);

        AnimatePart(LeftWing,
                    Brake(throttle) * yawAnimationScaler,
                    -(throttle - yaw) * yawAnimationScaler,
                    (roll + pitch) * rollAnimationScaler);
                    

        AnimatePart(RightWing,
                    Brake(throttle) * yawAnimationScaler,
                    (throttle + yaw) * yawAnimationScaler,
                    (roll - pitch) * rollAnimationScaler);

        AnimatePart(TailStart,
                    -pitch * animationScaler,
                    yaw * animationScaler,
                    roll * animationScaler);

        AnimatePart(TailEnd,
                    -pitch * animationScaler,
                    yaw * animationScaler,
                    roll * animationScaler);

        AnimatePart(LeftTail,
                    -pitch * animationScaler,
                    yaw * animationScaler,
                    roll * animationScaler);

        AnimatePart(RightTail,
                    -pitch * animationScaler,
                    yaw * animationScaler,
                    roll * animationScaler);
    }

    public override void Idle()
    {
        LeftWing.localRotation = Quaternion.Lerp(LeftWing.localRotation, Quaternion.identity, smallLerpAmount * Time.deltaTime);
        RightWing.localRotation = Quaternion.Lerp(RightWing.localRotation, Quaternion.identity, smallLerpAmount * Time.deltaTime);
        Fusilage.localRotation = Quaternion.Lerp(Fusilage.localRotation, Quaternion.identity, smallLerpAmount * Time.deltaTime);
        TailStart.localRotation = Quaternion.Lerp(TailStart.localRotation, Quaternion.identity, smallLerpAmount * Time.deltaTime);
        TailEnd.localRotation = Quaternion.Lerp(TailEnd.localRotation, Quaternion.identity, smallLerpAmount * Time.deltaTime);
        LeftTail.localRotation = Quaternion.Lerp(LeftTail.localRotation, Quaternion.identity, smallLerpAmount * Time.deltaTime);
        RightTail.localRotation = Quaternion.Lerp(RightTail.localRotation, Quaternion.identity, smallLerpAmount * Time.deltaTime);
    }

    void AnimatePart(Transform part, float partPitch, float partYaw, float partRoll)
    {
        part.localRotation = Quaternion.Lerp(
                                    part.localRotation,
                                    Quaternion.Euler(
                                        partPitch,
                                        partYaw,
                                        partRoll),  
                                    lerpAmount * Time.deltaTime);
    }

    float Brake(float throttle)
    {
        var brakeThreshold = .65f;
        float newThrottle;
        if (throttle < brakeThreshold) newThrottle = throttle - brakeThreshold;
        else newThrottle = 0;
        return newThrottle;
    }
}