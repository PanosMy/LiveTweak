using LiveTweak.Editor;

namespace LiveTweak;

public static class LiveTweaks
{
    public static void StartEditor(CancellationToken token)
    {
        TweakEditorApp.Start(token);
    }
}
