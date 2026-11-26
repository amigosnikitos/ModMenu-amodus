namespace Amodus.Features;

/// <summary>
/// The list of all features. To add a new one: create a class that derives from <see cref="Feature"/>
/// and put an instance into <see cref="All"/> - the button, the hotkey and the config entry appear by themselves.
/// </summary>
internal static class FeatureRegistry
{
    public static readonly NoClipFeature NoClip = new NoClipFeature();

    public static readonly ChatFeature Chat = new ChatFeature();

    /// <summary>The order here is the order of the buttons in the menu.</summary>
    public static readonly Feature[] All = { NoClip, Chat };
}
