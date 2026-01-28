using UnityEngine;

namespace BrewedCode.Utils
{
    public sealed class PendingDestroyMarker : MonoBehaviour
    {
    }

    public static class DestroyUtils
    {
        public static void PendingAndDestroy(this GameObject? go)
        {
            if (go == null) return;
            go.AddComponent<PendingDestroyMarker>();
            Object.Destroy(go);
        }

        public static bool IsPendingToDestroy(this GameObject? go)
        {
            return go && go.GetComponent<PendingDestroyMarker>();
        }
    }
}
