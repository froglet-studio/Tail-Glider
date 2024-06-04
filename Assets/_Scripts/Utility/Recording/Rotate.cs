using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicShore
{
    public class Rotate : MonoBehaviour
    {
        [SerializeField]
        private float speed = 10;

        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            float speed = this.speed * Time.deltaTime;
            transform.Rotate(0, speed, 0);


        }
    }
}
