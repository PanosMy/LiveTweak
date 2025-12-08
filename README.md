# LiveTweak

A runtime tweaking library for .NET applications that allows you to expose and modify static fields, properties, and methods at runtime through a native Avalonia editor.

## Overview

LiveTweak enables developers to mark static members with attributes and then view/modify them at runtime without recompiling. This is particularly useful for:

- Game development (tweaking game parameters in real-time)
- Configuration tuning during development
- Debugging and testing scenarios
- Live demonstrations

## Features

- **Attribute-based configuration**: Mark fields, properties, and methods with `[LiveTweak]` and `[LiveTweakAction]` attributes
- **Native Editor**: Cross-platform Avalonia-based desktop editor
- **Category organization**: Group tweakable items by category
- **Type support**: Supports int, float, double, bool, string, enums, and dictionaries
- **Callbacks**: Optional callback methods when values change

## Project Structure

```
LiveTweak/
│   └── Attributes/ # Attribute definitions (namespace: LiveTweak.Attributes)
│       ├── LiveTweakAttribute.cs
│       └── LiveTweakActionAttribute.cs
├── LiveTweak/                    # Main LiveTweak library
│   ├── Application/              # Application layer (interfaces, helpers)
│   ├── Domain/                   # Domain models and abstractions
│   ├── Editor/                   # Avalonia UI editor
│   ├── Infrastructure/           # Implementation details (reflection)
│   └── LiveTweaks.cs             # Main entry point
└── LiveTweak.Dev/                # Development/demo project
    └── Program.cs
```

## Requirements

- .NET 10.0 or later
- Avalonia 11.3.9 (for native editor)

## Installation

### Package Reference

Add the following package references to your project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/LiveTweak/LiveTweak.csproj" />
  <ProjectReference Include="path/to/Attribute/LiveTweak.Attribute.csproj" />
</ItemGroup>
```

## Usage

### 1. Mark Fields/Properties with Attributes

```csharp
using LiveTweak.Abstractions.Attributes;

public class GameSettings
{
    // Basic value with label and min/max range
    [LiveTweak("Player Speed", "Movement", Min: 0, Max: 100)]
    public static float PlayerSpeed = 5.0f;

    // Value with callback when changed
    [LiveTweak("Volume", "Audio", Min: 0, Max: 1 OnChanged = nameof(OnVolumeChanged))]
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

    private static void OnVolumeChanged()
    {
        Console.WriteLine($"Volume changed to: {MasterVolume}");
    }
}
```

### 2. Mark Methods as Actions

```csharp
using LiveTweak.Abstractions.Attributes;

public class GameActions
{
    [LiveTweakAction("Restart Level", "Game")]
    public static void RestartLevel()
    {
        Console.WriteLine("Level restarted!");
    }

    [LiveTweakAction("Clear Cache", "System")]
    public static void ClearCache()
    {
        // Clear cache logic
    }
}
```

## Attribute Reference

### `[LiveTweak]`

Applied to static fields or properties to make them tweakable.

| Parameter   | Type     | Description                                      |
| ----------- | -------- | ------------------------------------------------ |
| `label`     | `string` | Display name in the UI                           |
| `min`       | `double` | Minimum value (for numeric types)                |
| `max`       | `double` | Maximum value (for numeric types)                |
| `category`  | `string` | Category for grouping in UI (default: "General") |
| `OnChanged` | `string` | Name of static method to call when value changes |

### `[LiveTweakAction]`

Applied to static parameters methods to make them invocable from the UI.

| Parameter  | Type     | Description                                      |
| ---------- | -------- | ------------------------------------------------ |
| `label`    | `string` | Display name in the UI                           |
| `category` | `string` | Category for grouping in UI (default: "General") |

## Supported Types

- `int`, `long`
- `float`, `double`
- `bool`
- `string`
- `enum` types
- `Dictionary<TKey, TValue>` and `IDictionary`
- `ICollection` types

## Building

```bash
dotnet build LiveTweak.slnx
```

## Running the Demo

```bash
cd LiveTweak.Dev
dotnet run
```
