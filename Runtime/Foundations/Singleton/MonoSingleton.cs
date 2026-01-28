using UnityEngine;

namespace BrewedCode.Singleton
{
    /// <summary>
    /// The basic MonoBehaviour singleton implementation, this singleton is destroyed after scene changes, use <see cref="PersistentMonoSingleton{T}"/> if you want a persistent and global singleton instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DefaultExecutionOrder(-50)]
    public abstract class MonoSingleton<T> : MonoBehaviour, ISingleton where T : MonoSingleton<T>
    {
        #region Fields

        /// <summary>
        /// The instance.
        /// </summary>
        private static T instance;

        /// <summary>
        /// The initialization status of the singleton's instance.
        /// </summary>
        private SingletonInitializationStatus initializationStatus = SingletonInitializationStatus.None;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or creates the singleton instance.
        ///
        /// IMPORTANT: Uses == null (not != null) to properly detect destroyed Unity objects.
        /// When a GameObject is destroyed, its MonoBehaviour references become "fake null"
        /// (destroyed but not .NET null). Using == null catches this correctly.
        ///
        /// Without this: destroyed instance stays in _instance, later accesses fail.
        /// With this: destroyed instance is detected, FindObjectOfType searches again.
        /// </summary>
        /// <value>The instance.</value>
        public static T Instance
        {
            get
            {
                // == null checks for BOTH:
                // 1. _instance is actually null (.NET null)
                // 2. _instance was destroyed (Unity fake null)
                // Using == prevents dangling references to destroyed GameObjects
                if (instance == null)
                {
#if UNITY_6000
                    instance = FindAnyObjectByType<T>();
#else
                    instance = FindObjectOfType<T>();
#endif
                    if (instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = typeof(T).Name;
                        instance = obj.AddComponent<T>();
                        instance.OnMonoSingletonCreated();
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Gets whether the singleton's instance is initialized.
        /// </summary>
        public virtual bool IsInitialized => this.initializationStatus == SingletonInitializationStatus.Initialized;

        /// <summary>
        /// Gets whether a valid singleton instance exists (not destroyed).
        /// </summary>
        public static bool HasInstance => instance != null;

        #endregion

        #region Unity Messages

        /// <summary>
        /// Use this for initialization.
        /// </summary>
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;

                // Initialize existing instance
                InitializeSingleton();
            }
            else if (instance != this)
            {

                // Destory duplicates
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This gets called once the singleton's instance is created.
        /// </summary>
        protected virtual void OnMonoSingletonCreated()
        {

        }

        protected virtual void OnInitializing()
        {

        }

        protected virtual void OnInitialized()
        {

        }

        #endregion

        #region Public Methods

        public virtual void InitializeSingleton()
        {
            if (this.initializationStatus != SingletonInitializationStatus.None)
            {
                return;
            }

            this.initializationStatus = SingletonInitializationStatus.Initializing;
            OnInitializing();
            this.initializationStatus = SingletonInitializationStatus.Initialized;
            OnInitialized();
        }

        public virtual void ClearSingleton() { }

        public static void CreateInstance()
        {
            DestroyInstance();
            instance = Instance;
        }

        public static void DestroyInstance()
        {
            if (instance == null)
            {
                return;
            }

            instance.ClearSingleton();
            instance = default(T);
        }

        #endregion
    }
}
