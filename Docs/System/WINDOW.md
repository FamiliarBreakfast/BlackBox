# Window

**`Window.cs`** (root) | `BlackBox` | ✅ Complete

Raylib-based terminal rendering with shader support (hostspace).

## API

```csharp
// Initialization
Initialize(int termWidth = 80, int termHeight = 25, string title = "BlackBox")

// Window control
bool ShouldClose()
void Close()

// Rendering
BeginFrame()
Render()
EndFrame()

// Shaders
LoadShader(string fragmentShaderPath)
UnloadShader()

// Terminal access
Terminal? GetTerminal()
Write(string text)  // Hostspace write
```

## Basic Loop

```csharp
Window.Initialize(80, 25, "BlackBox");

while (!Window.ShouldClose()) {
    Window.BeginFrame();
    // Update logic
    Window.Render();
    Window.EndFrame();
}

Window.Close();
```

## Features

- Hardware-accelerated Raylib rendering
- Post-processing shader effects via render texture
- Animated cursor (configurable blink)
- 8x16 monospace character cells

## See Also

[Terminal](TERMINAL.md)
