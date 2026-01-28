using System.Runtime.CompilerServices;

// BrewedCode.Foundation assembly consolidates all Foundations (Events, Logging, Singleton, TimerManager)
// Allow all test assemblies to access internal types

[assembly: InternalsVisibleTo("BrewedCode.Events.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.Logging.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.TimerManager.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.Crafting.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.Theme.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.VitalGauge.Tests")]
