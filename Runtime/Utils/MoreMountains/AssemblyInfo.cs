using System.Runtime.CompilerServices;

// Test assemblies - allow access to internals
[assembly: InternalsVisibleTo("BrewedCode.Events.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.Logging.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.TimerManager.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.Crafting.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.Theme.Tests")]
[assembly: InternalsVisibleTo("BrewedCode.VitalGauge.Tests")]

// Internal dependencies
[assembly: InternalsVisibleTo("BrewedCode.Foundation")]
