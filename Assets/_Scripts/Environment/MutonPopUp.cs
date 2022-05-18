using UnityEngine;
using System.Collections.Generic;
using StarWriter.Core.Input;

public class MutonPopUp : MonoBehaviour
{
    #region Events
    public delegate void PopUpCollision(string uuid, float amount);
    public static event PopUpCollision OnMutonPopUpCollision;

    public delegate void OnCollisionIncreaseScore(string uuid, int amount);
    public static event OnCollisionIncreaseScore AddToScore;
    #endregion

    #region Floats
    [SerializeField]
    float intensityAmount = 0.07f;
    
    [SerializeField]
    float sphereRadius = 100;
    
    #endregion

    [SerializeField]
    int scoreBonus = 5;

    #region Referenced in Inspector
    [SerializeField]
    GameObject spentMutonPrefab;  
    
    [SerializeField]
    GameObject Muton;
    
    [SerializeField]
    Material material;

    [SerializeField]
    float lifeTimeIncrease;
    #endregion

    Material tempMaterial;
    List<Collider> collisions;

    void Start()
    {
        collisions = new List<Collider>();
    }

    void OnCollisionEnter(Collision collision)
    {
        collisions.Add(collision.collider);
    }

    private void Update()
    {
        if (collisions.Count > 0)
        {
            Collide(collisions[0]);
            collisions.Clear();
        }
    }

    private void Collide(Collider other)
    {
        // make an exploding muton
        var spentMuton = Instantiate<GameObject>(spentMutonPrefab);
        spentMuton.transform.position = transform.position;
        spentMuton.transform.localEulerAngles = transform.localEulerAngles;
        tempMaterial = new Material(material);
        spentMuton.GetComponent<Renderer>().material = tempMaterial;

        GameObject ship = other.transform.parent.parent.gameObject;
        
        if (ship == GameObject.FindWithTag("Player"))
        {
            // Player Collision
            //muton animation and haptics
            StartCoroutine(spentMuton.GetComponent<Impact>().ImpactCoroutine(
                ship.transform.forward * ship.GetComponent<InputController>().speed, tempMaterial, "Player"));
            HapticController.PlayMutonCollisionHaptics();
            //update intensity bar and score
            OnMutonPopUpCollision(ship.GetComponent<Player>().PlayerUUID, intensityAmount); // excess Intensity flows into score
            if (AddToScore != null) { AddToScore(ship.GetComponent<Player>().PlayerUUID, scoreBonus); }
        }
        else
        {
            // AI collision
            if (ship == GameObject.FindWithTag("red"))
            {
                StartCoroutine(spentMuton.GetComponent<Impact>().ImpactCoroutine(
                    ship.transform.forward * ship.GetComponent<AiShipController>().speed, tempMaterial, "red"));
                
                //reset ship aggression
                AiShipController controllerScript = ship.GetComponent<AiShipController>();
                controllerScript.lerpAmount = .2f;
            }
            else
            {
                StartCoroutine(spentMuton.GetComponent<Impact>().ImpactCoroutine(
                     ship.transform.forward * ship.GetComponent<AiShipController>().speed, tempMaterial, "blue"));
                
                //reset ship aggression
                AiShipController controllerScript = ship.GetComponent<AiShipController>();
                controllerScript.lerpAmount = .2f;
            }
        }

        // TODO play SFX sound

        // Move the muton
        StartCoroutine(Muton.GetComponent<FadeIn>().FadeInCoroutine());
        transform.SetPositionAndRotation(UnityEngine.Random.insideUnitSphere * sphereRadius, UnityEngine.Random.rotation);

        // Grow tail
        TrailSpawner trailScript = ship.GetComponent<TrailSpawner>();
        trailScript.lifeTime += lifeTimeIncrease;
    }
}
