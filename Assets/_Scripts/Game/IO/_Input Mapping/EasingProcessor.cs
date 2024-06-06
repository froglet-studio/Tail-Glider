using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CosmicShore
{
    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
    public class EasingProcessor : InputProcessor<Vector2>
    {
        #if UNITY_EDITOR
        static EasingProcessor()
        {
            Initialize();
        }
        #endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            InputSystem.RegisterProcessor<EasingProcessor>();
        }

        private float piOverFour = Mathf.PI / 4;


        // TODO: Move to a utility class, maybe as a static method.
        /// <summary>
        /// Computationally very cheap function that changes a linear input to an input that's eased in the middle.
        /// </summary>
        /// <param name="input">Sum or difference of raw input. Ranged between -2 and 2.</param>
        /// <returns>The input flattened at 0 but not at -2 or 2. Ranged -1 to 1.
        /// In effect, it has low sensitivity at the neighborhood of origin, and higher sensitivity at the extremes.</returns>
        private float Ease(float value)
        {
            int factor = value < 0 ? 1 : -1;
            // A sinusoid wave that has a period of 8, centered at zero, with the same peaks and valleys at -1 and 1.
            // Like the original sinusiod wave, it's flat at input 0.
            // It intersects with (0, -2) and (0, 2).
            float sinusoid = Mathf.Cos(value * piOverFour);
            // Shifts curve so that it now ranges from 0 to -2.
            // Between -1 and 0, it flattens towards 0 but not near 1.
            sinusoid -= 1;
            // If the input in greater than one, get positive values instead of negative.
            sinusoid *= factor;
            return sinusoid;
        }

        public override Vector2 Process(Vector2 value, InputControl input)
        {
            return new Vector2(Ease(value.x), Ease(value.y));
        }
    }
}
