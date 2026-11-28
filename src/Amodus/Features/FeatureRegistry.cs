namespace Amodus.Features;

/// <summary>
/// The list of all features. To add a new one: create a class that derives from <see cref="Feature"/>
/// and put an instance into <see cref="All"/> - the row in the menu, the hotkey and the config entries appear by themselves.
/// </summary>
internal static class FeatureRegistry
{
    public static readonly NoClipFeature NoClip = new NoClipFeature();

    public static readonly SeeGhostsFeature SeeGhosts = new SeeGhostsFeature();

    public static readonly ChatFeature Chat = new ChatFeature();

    public static readonly GhostChatFeature GhostChat = new GhostChatFeature();

    public static readonly CopyCodeFeature CopyCode = new CopyCodeFeature();

    /// <summary>The order here is the order of the rows in the menu (inside each tab).</summary>
    public static readonly Feature[] All = { NoClip, SeeGhosts, Chat, GhostChat, CopyCode };
}
