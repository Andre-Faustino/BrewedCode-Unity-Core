using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrewedCode.Events
{
    /// <summary>
    /// Automatically manages scene-scoped event channels.
    /// Initializes on domain reload, hooks into scene events.
    /// Scene-scoped channels are cleared when the scene unloads.
    /// </summary>
    internal static class SceneScopeProvider
    {
        private static readonly Dictionary<int, EventScopeKey> s_SceneScopeKeys = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            s_SceneScopeKeys.Clear();

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // Register currently loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    RegisterScene(scene);
                }
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterScene(scene);
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            if (s_SceneScopeKeys.TryGetValue(scene.handle, out var scopeKey))
            {
                EventChannelRegistry.ClearScope(scopeKey);
                s_SceneScopeKeys.Remove(scene.handle);
            }
        }

        private static void RegisterScene(Scene scene)
        {
            if (s_SceneScopeKeys.ContainsKey(scene.handle)) return;

            var scopeKey = EventScopeKey.ForScene(scene.handle);
            s_SceneScopeKeys[scene.handle] = scopeKey;

#if UNITY_EDITOR
            EventDebugCapture.OnSceneScopeCreated(scene.name, scopeKey);
#endif
        }

        /// <summary>
        /// Gets the scope key for the currently active scene.
        /// </summary>
        public static EventScopeKey GetCurrentSceneScopeKey()
        {
            var activeScene = SceneManager.GetActiveScene();
            return GetSceneScopeKey(activeScene);
        }

        /// <summary>
        /// Gets the scope key for a specific scene.
        /// </summary>
        public static EventScopeKey GetSceneScopeKey(Scene scene)
        {
            if (s_SceneScopeKeys.TryGetValue(scene.handle, out var key))
                return key;

            // Lazy create if not yet tracked
            key = EventScopeKey.ForScene(scene.handle);
            s_SceneScopeKeys[scene.handle] = key;

#if UNITY_EDITOR
            EventDebugCapture.OnSceneScopeCreated(scene.name, key);
#endif

            return key;
        }

        /// <summary>
        /// Gets the scope key for a scene by name.
        /// Returns Global scope if scene is not loaded.
        /// </summary>
        public static EventScopeKey GetSceneScopeKey(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"[EventSystem] Scene '{sceneName}' is not loaded. Returning Global scope.");
                return EventScopeKey.Global;
            }

            return GetSceneScopeKey(scene);
        }
    }
}