#if MOREMOUNTAINS_TOOLS
using System.Collections;
using System.Collections.Generic;
using BrewedCode.Logging;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace BrewedCode.Utils.MoreMountains
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSystemEvents : MMMonoBehaviour
    {
        private ILog? _logger;

        [MMInspectorGroup("Target", true, 115)]
        [Tooltip("If empty, uses the ParticleSystem on this GameObject.")]
        public ParticleSystem target;

        [MMInspectorGroup("Options", true, 36)]
        [Tooltip("Automatically set Main.StopAction = Callback to enable OnParticleSystemStopped().")]
        public bool autoSetStopActionCallback = true;

        [Tooltip("When true, includes sub-emitters in 'finished' detection.")]
        public bool includeChildrenForFinished = true;

        [Tooltip("Enable per-frame polling (Update) for play/pause/resume/stop edges and finished detection.")]
        public bool enableStatePolling = true;

        [MMInspectorGroup("Lifecycle Events", true, 48)]
        public UnityEvent OnPlayed;
        public UnityEvent OnPaused;
        public UnityEvent OnResumed;
        public UnityEvent OnStoppedEdge; // edge when entering stopped state
        public UnityEvent OnFinishedAll; // no particles alive (incl. children if set)

        [MMInspectorGroup("Stop Callback (requires StopAction = Callback)", true, 99)]
        public UnityEvent OnStoppedCallback; // from OnParticleSystemStopped()

        [System.Serializable]
        public class GameObjectEvent : UnityEvent<GameObject> { }

        [MMInspectorGroup("Collision", true, 42)]
        public GameObjectEvent OnCollision;

        [System.Serializable]
        public class TriggerCountsEvent : UnityEvent<int, int, int, int> { }

        [MMInspectorGroup("Trigger (counts)", true, 87)]
        [Tooltip("Invoked with counts: Enter, Exit, Inside, Outside (in this order).")]
        public TriggerCountsEvent OnTriggerSummary;

        // --- internals ---
        private ParticleSystem _ps;
        private bool _wasPlaying, _wasPaused, _wasAlive;

        // reusable buffers to avoid GC
        private readonly List<ParticleSystem.Particle> _enter = new();
        private readonly List<ParticleSystem.Particle> _exit = new();
        private readonly List<ParticleSystem.Particle> _inside = new();
        private readonly List<ParticleSystem.Particle> _outside = new();

        void Awake()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(ParticleSystemEvents));
            }
            catch
            {
                _logger = null;
            }

            _ps = target ? target : GetComponent<ParticleSystem>();
            if (!_ps)
            {
                _logger?.ErrorSafe("[ParticleSystemEvents] No ParticleSystem found.");
                enabled = false;
                return;
            }

            if (autoSetStopActionCallback)
            {
                var main = _ps.main;
                main.stopAction = ParticleSystemStopAction.Callback;
            }
        }

        void OnEnable()
        {
            SnapshotState();
        }

        void Update()
        {
            if (!_ps || !enableStatePolling) return;

            bool playing = _ps.isPlaying;
            bool paused = _ps.isPaused;
            bool alive = _ps.IsAlive(includeChildrenForFinished);

            // Play edge
            if (!_wasPlaying && playing) OnPlayed?.Invoke();

            // Pause/Resume edges
            if (!_wasPaused && paused) OnPaused?.Invoke();
            if (_wasPaused && playing) OnResumed?.Invoke();

            // Stop edge
            if ((_wasPlaying || _wasPaused) && _ps.isStopped) OnStoppedEdge?.Invoke();

            // Finished (no particles alive)
            if (_wasAlive && !alive) OnFinishedAll?.Invoke();

            _wasPlaying = playing;
            _wasPaused = paused;
            _wasAlive = alive;
        }

        void SnapshotState()
        {
            _wasPlaying = _ps.isPlaying;
            _wasPaused = _ps.isPaused;
            _wasAlive = _ps.IsAlive(includeChildrenForFinished);
        }

        // --------- native callbacks ---------

        // Requires Main.stopAction == Callback
        void OnParticleSystemStopped()
        {
            OnStoppedCallback?.Invoke();

            // If polling is disabled, still provide a robust "finished" signal:
            if (!enableStatePolling)
                StartCoroutine(WaitAllDeadThenInvokeFinished());
        }

        // Trigger Module
        void OnParticleTrigger()
        {
            int enter = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, _enter);
            int exit = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, _exit);
            int inside = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, _inside);

            int outside = 0;
            try
            {
                outside = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Outside, _outside);
            }
            catch
            {
                outside = 0;
            }

            OnTriggerSummary?.Invoke(enter, exit, inside, outside);

            _enter.Clear();
            _exit.Clear();
            _inside.Clear();
            _outside.Clear();
        }

        // Collision Module
        void OnParticleCollision(GameObject other)
        {
            OnCollision?.Invoke(other);
        }

        // --------- helpers ---------

        private IEnumerator WaitAllDeadThenInvokeFinished()
        {
            // Wait until no particles are alive (optionally includes sub-emitters)
            yield return new WaitWhile(() => _ps && _ps.IsAlive(includeChildrenForFinished));
            OnFinishedAll?.Invoke();
        }
    }
}
#endif
