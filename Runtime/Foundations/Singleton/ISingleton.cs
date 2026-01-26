namespace BrewedCode.Singleton
{
    /// <summary>
    /// Interface for singleton classes.
    /// </summary>
    public interface ISingleton
    {
        /// <summary>
        /// Gets whether the singleton's instance is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        void InitializeSingleton();

        /// <summary>
        /// Clears the singleton instance.
        /// </summary>
        void ClearSingleton();
    }
}
