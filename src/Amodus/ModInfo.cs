namespace Amodus;

/// <summary>Plugin identity. Raise <see cref="Version"/> before every release.</summary>
internal static class ModInfo
{
    /// <summary>Unique id. It is also the name of the config file: BepInEx/config/com.amodus.menu.cfg.</summary>
    public const string Guid = "com.amodus.menu";

    public const string Name = "Amodus";

    public const string Version = "0.1.0";
}
