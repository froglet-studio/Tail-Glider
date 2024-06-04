using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CosmicShore
{
    public class RotatingCamera : MonoBehaviour
    {
        [SerializeField]
        private GameObject CenterOfRotation;

        [SerializeField]
        private float Speed = 2;

        [SerializeField]
        private float Acceleration = 1.2f;

        public float xVelocity = 0;
        public float yVelocity = 0;
        private float velocityThreshhold = 0.2f;
        public bool rotatingX = false;
        public bool rotatingY = false;

        void Update()
        {
            Rotate();
        }

        public void OnCameraMovement(InputAction.CallbackContext context)
        {
            Vector2 movement = context.ReadValue<Vector2>();

            yVelocity = movement.y * Speed;
            rotatingY = movement.y != 0.0f;
            xVelocity = movement.x * Speed;
            rotatingX = movement.x != 0.0f;
        }

        private void Rotate()
        {
            Vector3 spine = new Vector3(CenterOfRotation.transform.position.x, transform.position.y, CenterOfRotation.transform.position.z);

            gameObject.transform.RotateAround(CenterOfRotation.transform.position, Vector3.right, yVelocity * Time.deltaTime);
            gameObject.transform.RotateAround(spine, Vector3.up, xVelocity * Time.deltaTime);

            Vector3 myRotation = gameObject.transform.localEulerAngles;
            gameObject.transform.localEulerAngles = new Vector3(myRotation.x, myRotation.y, 0);
        }
    }
}
