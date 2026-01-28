using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrewedCode.Utils
{
    public interface IEditorTick
    {
        /// Master switch individual
        bool EditorTickEnabled { get; }

        /// Intervalo desejado entre ticks deste alvo (segundos). <= 0 roda todo frame do editor.
        double EditorTickInterval { get; }

        /// Chamado quando o intervalo foi cumprido (ou todo frame, se <= 0)
        void EditorTick(float deltaTime);
    }

    [InitializeOnLoad]
    public static class EditorTickService
    {
        private const double ScanInterval = 1.0;

        private static readonly List<IEditorTick> _targets = new(128);
        private static readonly Dictionary<IEditorTick, double> _lastTickTime = new(128);

        private static double _lastScanTime;
        private static double _lastUpdateTime;

        static EditorTickService()
        {
            _lastScanTime = EditorApplication.timeSinceStartup;
            _lastUpdateTime = _lastScanTime;
            EditorApplication.update += Update;
            Rescan();
        }

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastUpdateTime);
            _lastUpdateTime = now;

            // re-scan periódico (criações/remoções)
            if (now - _lastScanTime >= ScanInterval)
            {
                Rescan();
                _lastScanTime = now;
            }

            // tick por alvo respeitando o intervalo individual
            for (int i = 0; i < _targets.Count; i++)
            {
                var obj = _targets[i] as Object;
                if (!obj)
                {
                    continue;
                } // destruído

                var mb = obj as MonoBehaviour;
                if (!mb || !mb.isActiveAndEnabled)
                {
                    continue;
                }

                var itick = _targets[i];
                if (!itick.EditorTickEnabled)
                {
                    continue;
                }

                double interval = itick.EditorTickInterval;
                if (interval <= 0d)
                {
                    itick.EditorTick(dt);
                    continue;
                }

                // verifica relógio por-alvo
                if (!_lastTickTime.TryGetValue(itick, out var last))
                {
                    // primeira vez: dispara já e registra
                    _lastTickTime[itick] = now;
                    itick.EditorTick(dt);
                    continue;
                }

                if (now - last >= interval)
                {
                    itick.EditorTick((float)interval); // ou dt; escolha sua semântica
                    _lastTickTime[itick] = now;
                }
            }
        }

        private static void Rescan()
        {
            // limpa alvos inexistentes do dicionário
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                if (!(_targets[i] as Object)) _targets.RemoveAt(i);
            }

            var existing = new HashSet<IEditorTick>(_targets);

            // adiciona novos
            var allBehaviours =
                Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in allBehaviours)
            {
                if (b is IEditorTick it && !existing.Contains(it))
                {
                    _targets.Add(it);
                    // inicializa relógio para que possa disparar no próximo ciclo
                    if (!_lastTickTime.ContainsKey(it))
                        _lastTickTime[it] = 0d;
                }
            }

            // remove relógios de alvos que sumiram
            var keys = new List<IEditorTick>(_lastTickTime.Keys);
            foreach (var k in keys)
            {
                if (!(k as Object)) _lastTickTime.Remove(k);
            }
        }

        [MenuItem("Tools/Editor Tick/Force Rescan")]
        private static void ForceRescan() => Rescan();
    }

    /// <summary>
    /// Abstract base for editor-time ticking via EditorTickService (IEditorTick).
    /// Not included in builds.
    /// </summary>
    public abstract class EditorTickerBase : MonoBehaviour, IEditorTick
    {
        [Header("Editor Ticker")]
        [SerializeField, Tooltip("Master switch to run this ticker in the Editor.")]
        private bool _enabledInEditor = true;

        [SerializeField, Tooltip("Also run while Play Mode is active.")]
        private bool _runInPlayMode =
            false;

        [SerializeField, Tooltip("Seconds between ticks for this ticker. <= 0 updates every editor frame.")]
        private double _tickIntervalSeconds =
            0.0;

        public bool EditorTickEnabled => _enabledInEditor && (_runInPlayMode || !Application.isPlaying);

        public double EditorTickInterval => _tickIntervalSeconds;

        public void EditorTick(float deltaTime) => OnEditorTick(deltaTime);

        protected abstract void OnEditorTick(float dt);
    }
}
