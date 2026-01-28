using UnityEngine.Events;

namespace BrewedCode.Utils
{
    public static class UnityEventExtensions
    {
        /// <summary>
        /// Adiciona um listener que roda apenas uma vez e se remove sozinho.
        /// </summary>
        public static void Once(this UnityEvent unityEvent, UnityAction call)
        {
            UnityAction wrapper = null;
            wrapper = () =>
            {
                call.Invoke();
                unityEvent.RemoveListener(wrapper);
            };
            unityEvent.AddListener(wrapper);
        }

        /// <summary>
        /// Adiciona um listener que roda apenas uma vez e se remove sozinho.
        /// (Versão com 1 argumento)
        /// </summary>
        public static void Once<T>(this UnityEvent<T> unityEvent, UnityAction<T> call)
        {
            UnityAction<T> wrapper = null;
            wrapper = (arg) =>
            {
                call.Invoke(arg);
                unityEvent.RemoveListener(wrapper);
            };
            unityEvent.AddListener(wrapper);
        }

        /// <summary>
        /// Adiciona um listener que roda apenas uma vez e se remove sozinho.
        /// (Versão com 2 argumentos)
        /// </summary>
        public static void Once<T1, T2>(this UnityEvent<T1, T2> unityEvent, UnityAction<T1, T2> call)
        {
            UnityAction<T1, T2> wrapper = null;
            wrapper = (arg1, arg2) =>
            {
                call.Invoke(arg1, arg2);
                unityEvent.RemoveListener(wrapper);
            };
            unityEvent.AddListener(wrapper);
        }

        // Dá pra expandir pra UnityEvent<T1,T2,T3> se precisar.
    }
}
