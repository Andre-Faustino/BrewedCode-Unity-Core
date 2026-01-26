namespace BrewedCode.ItemHub
{
    /// <summary>
    /// Runtime record for a mutable, non-stackable item instance.
    /// Payload is opaque JSON (or any serialized text) to keep the core decoupled.
    /// </summary>
    public sealed class InstanceRecord
    {
        public InstanceId InstanceId { get; init; }
        public ItemId DefinitionId { get; init; }
        public int Version { get; init; } = 1;
        public long CreatedAt { get; init; }          // Inject your global game time ticks/seconds
        public string PayloadJson { get; set; }      // JSON blob with mutable state

        public InstanceRecord Clone()
        {
            return new InstanceRecord
            {
                InstanceId = InstanceId,
                DefinitionId = DefinitionId,
                Version = Version,
                CreatedAt = CreatedAt,
                PayloadJson = PayloadJson
            };
        }
    }
}