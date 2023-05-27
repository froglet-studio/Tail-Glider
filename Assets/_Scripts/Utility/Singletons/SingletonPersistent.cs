using UnityEngine;

namespace StarWriter.Utility.Singleton
{
    /// <remarks>
    /// Creates a Singleton GameObject which is persistent thru scenes
    /// </remarks>
    public class SingletonPersistent<T> : MonoBehaviour where T : Component
    {
        public static T Instance { get; private set; }

        public virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
                DontDestroyOnLoad(this);
            }
            else
                Destroy(gameObject);
        }
    }
}