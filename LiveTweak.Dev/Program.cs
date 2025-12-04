using LiveTweak;
using LiveTweak.Attributes;
using LiveTweak.Dev;

internal class Program
{
    private static void Main()
    {
        Console.WriteLine("Hello, Live Tweak developer!");

        Task.Run(() => LiveTweaks.StartEditor(CancellationToken.None));

        // Keep the process alive forever
        Thread.Sleep(Timeout.Infinite);
    }

    [LiveTweak("Player Speed", "Movement", Min = 0d, Max = 100d)]
    public static float PlayerSpeed = 5.0f;

    // Value with callback when changed
    [LiveTweak("Volume", "Audio", Min = 0, Max = 1, OnChanged = nameof(OnVolumeChanged))]
    public static float MasterVolume = 0.8f;


    [LiveTweak("Debug Mode")]
    public static bool DebugEnabled = false;

    [LiveTweak("Key Bindings", "Keys")]
    public static Dictionary<string, string> KeyBindings = new()
    {
        { "Jump", "Space" },
        { "Crouch", "Ctrl" },
        { "Shoot", "LeftMouse" }
    };

    [LiveTweak("Key Bindings Index", "Keys", OnChanged = nameof(OnDictionaryIntChanged))]
    public static Dictionary<int, int> KeyBindingsIndex = new()
    {
        { 1, 10 },
        { 2, 20 },
        { 3, 30 }
    };

    [LiveTweak("Key Bindings Enable", "Keys", Min = 2, Max = 5)]
    public static Dictionary<string, int[]> KeyBindingsEnable = new()
    {
        { "Key1", [2, 1]},
        { "Key2", [10] },
        { "Key3", [3] }
    };

    [LiveTweak("Key Types Bindings ", "Keys")]
    public static Dictionary<KeyType, string> KeyTypesBindings = new()
    {
        { KeyType.Jump, "Space"},
        { KeyType.Crouch, "Ctrl"}
    };

    private static void OnVolumeChanged()
    {
        Console.WriteLine($"Volume changed to: {MasterVolume}");
    }

    private static void OnDictionaryIntChanged(string member, int key, int value)
    {
        Console.WriteLine($"change value Key: {key}, Value: {value} , {member}");
        foreach (var kvp in KeyBindingsIndex)
        {
            Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
        }
    }
}
