namespace BrewedCode.Events
{
    /// <summary>
    /// Interface for objects that own/emit scoped events.
    /// Implement this on MonoBehaviours that should be event emitters.
    /// </summary>
    /// <example>
    /// public class Enemy : MonoBehaviour, IEventScope
    /// {
    ///     private EventScopeKey _scopeKey;
    ///     public EventScopeKey ScopeKey => _scopeKey;
    ///
    ///     private void Awake()
    ///     {
    ///         _scopeKey = EventScopeKey.ForInstance(GetInstanceID());
    ///     }
    /// }
    /// </example>
    public interface IEventScope
    {
        /// <summary>
        /// The unique scope key for this object.
        /// Implementations should compute this once during initialization and cache it.
        /// </summary>
        EventScopeKey ScopeKey { get; }
    }
}
