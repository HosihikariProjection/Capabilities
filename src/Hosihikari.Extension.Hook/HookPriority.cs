namespace Hosihikari.Extension.Hook;

/// <summary>Priority of an installed hook; lower values run earlier.</summary>
public enum HookPriority
{
    /// <summary>Runs first.</summary>
    Highest = 0,

    /// <summary>Runs after <see cref="Highest"/>.</summary>
    High = 100,

    /// <summary>The default.</summary>
    Normal = 200,

    /// <summary>Runs after <see cref="Normal"/>.</summary>
    Low = 300,

    /// <summary>Runs last.</summary>
    Lowest = 400,
}
