using UnityEngine;
using UnityEngine.Events;

namespace BrewedCode.Utils
{
    [RequireComponent(typeof(Collider2D))]
    public class TriggerRelay2D : MonoBehaviour
    {
        [System.Serializable] public class TriggerEvent : UnityEvent<Collider2D>
        {
        }

        [Header("Trigger Events")]
        public TriggerEvent OnEnter;
        public TriggerEvent OnExit;
        public TriggerEvent OnStay;

        private void OnTriggerEnter2D(Collider2D other)
        {
            OnEnter?.Invoke(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            OnExit?.Invoke(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            OnStay?.Invoke(other);
        }
    }
}
