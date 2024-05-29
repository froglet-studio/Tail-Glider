using CosmicShore.Core;
using Mono.CSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicShore
{
    public class Spindle : MonoBehaviour
    {
        public GameObject cylinder;
        public float Length = 1f;

        void Awake()
        {
            if (cylinder) Length = cylinder.transform.localScale.y;
        }

        public void CheckForLife()
        {
            //Debug.Log($"Checking spindle for life: GetComponentsInChildren<HealthBlock>().length = {GetComponentsInChildren<HealthBlock>().Length} GetComponentsInChildren<Spindle>().Length = {GetComponentsInChildren<Spindle>().Length}");
            if (GetComponentsInChildren<HealthBlock>().Length == 0 && GetComponentsInChildren<Spindle>().Length <= 1) // if there are no health blocks and only one spindle (this one)
            {
                Debug.Log("Spindle.Evaporating");
                Evaporate();
            }
        }

        IEnumerator Evaporate()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            float deathAnimation = 0f;
            float animationSpeed = 1f;
            while (deathAnimation < 1f)
            {
                meshRenderer.material.SetFloat("_DeathAnimation", deathAnimation);
                deathAnimation += 0.01f;
                yield return new WaitForSeconds(animationSpeed * Time.deltaTime);
            }
            Destroy(gameObject);
        }
    }
}
