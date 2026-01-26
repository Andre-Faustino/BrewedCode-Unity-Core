using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrewedCode.Utils
{
    public static class HierarchyUtils
    {
        /// <summary>
        /// Instantiates a prefab as a child of the given parent for each item in the collection.
        /// Optionally invokes a callback with the created instance and the corresponding data object.
        /// </summary>
        /// <typeparam name="T1">Type of the prefab, must inherit from UnityEngine.Object.</typeparam>
        /// <typeparam name="T2">Type of the data objects.</typeparam>
        /// <param name="parent">Parent GameObject to attach the instantiated prefabs to.</param>
        /// <param name="prefab">Prefab to instantiate.</param>
        /// <param name="gameObjects">Collection of data objects to associate with each instance.</param>
        /// <param name="callback">Optional callback executed with each instance and its data object.</param>
        public static void InstantiateGameObjectsToParent<T1, T2>(
            GameObject parent,
            T1 prefab,
            IEnumerable<T2> gameObjects,
            Action<T1, T2> callback = null)
            where T1 : Object
        {
            foreach (var targetObject in gameObjects)
            {
                var instance = Object.Instantiate(prefab, parent.transform);
                callback?.Invoke(instance, targetObject);
            }
        }

        /// <summary>
        /// Destroys all child GameObjects under the given parent.
        /// </summary>
        /// <param name="parent">Parent GameObject whose children will be destroyed.</param>
        public static void DestroyAllChildren(GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                Object.Destroy(child.gameObject);
            }
        }
    }
}
