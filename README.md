# Aprillz.MewUI

Minimal, code-first .NET GUI library aimed at NativeAOT + Trim.

**Status:** experimental prototype (APIs and behavior may change).

**Note:** most of the code in this repository is written with the help of GPT.

**Sample project NativeAOT + Trimmed publish output size** (`win-x64-trimmed`): single EXE ~ `2.2 MB`

<img width="620" height="588" alt="image" src="https://github.com/user-attachments/assets/3851bbca-88d0-4a2b-835f-56093244c694" />

<img width="620" height="588" alt="image" src="https://github.com/user-attachments/assets/793b6c01-724e-4684-9236-29e5ad7ed04b" />


 
## C# Markup at a Glance

```csharp
var window = new Window()
    .Title("Hello MewUI")
    .Size(520, 360)
    .Padding(12)
    .Content(
        new StackPanel()
            .Spacing(8)
            .Children(
                new Label()
                    .Text("Hello, Aprillz.MewUI")
                    .FontSize(18)
                    .Bold(),
                new Button()
                    .Content("Quit")
                    .OnClick(() => Application.Quit())
            )
    );

Application.Run(window);
```

## Concept

MewUI is a code-first UI library with three priorities:
- **Fluent C# markup** for building UI trees (no XAML)
- **AOT-friendly binding** (`ObservableValue<T>`, delegate-based, avoid reflection)
- **NativeAOT + trimming friendliness** (interop via `LibraryImport`)

Out of scope (for now):
- Full XAML/WPF compatibility
- Huge control catalog and designer tooling

## Quick Start

Prerequisites: .NET SDK (`net10.0-windows`).

Build:
- `dotnet build .\\MewUI.slnx -c Release`

Run sample:
- `dotnet run --project .\\samples\\MewUI.Sample\\MewUI.Sample.csproj -c Release`

Publish (NativeAOT + Trim, size-focused):
- `dotnet publish .\\samples\\MewUI.Sample\\MewUI.Sample.csproj -c Release -p:PublishProfile=win-x64-trimmed`

## NativeAOT / Trim

- The library aims to be trimming-safe by default (explicit code paths, no reflection-based binding).
- Windows interop uses source-generated P/Invoke (`LibraryImport`) for NativeAOT compatibility.
- If you introduce new interop or dynamic features, verify with the trimmed publish profile above.

This project is optimized for *publish-time size* (NativeAOT + trimming), but exact output size depends on:
- .NET SDK version, RID, and linker/ILC settings
- Which rendering backend you publish with (Direct2D vs GDI)
- Fonts/resources you include

To check output size locally:
- Publish: `dotnet publish .\\samples\\MewUI.Sample\\MewUI.Sample.csproj -c Release -p:PublishProfile=win-x64-trimmed`
- Inspect: `samples\\MewUI.Sample\\bin\\Release\\net10.0-windows\\win-x64\\publish\\trimmed\\`

Reference (sample, `win-x64-trimmed`):
- `Aprillz.MewUI Demo.exe` ~ `2,257 KB`

## State & Binding (AOT-friendly)

Bindings are explicit and delegate-based (no reflection):

```csharp
using Aprillz.MewUI.Binding;
using Aprillz.MewUI.Controls;

var percent = new ObservableValue<double>(0.25);

var slider = new Slider().BindValue(percent);
var label  = new Label().BindText(percent, v => $"Percent ({v:P0})");
```

## Theme

Theme is split into two parts:
- `Palette` - colors (including derived colors based on background + accent)
- `Theme` - non-color parameters (corner radius, default font, plus a `Palette`)

Accent switching:

```csharp
Theme.Current = Theme.Current.WithAccent(Aprillz.MewUI.Primitives.Color.FromRgb(214, 176, 82));
```

## Controls / Panels

Controls:
- `Label`, `Button`, `TextBox`
- `CheckBox`, `RadioButton`
- `ListBox`, `ComboBox`
- `Slider`, `ProgressBar`
- `Window`

Panels:
- `Grid` (rows/columns with `Auto`, `*`, pixel)
- `StackPanel` (horizontal/vertical + spacing)
- `DockPanel` (dock edges + last-child fill)
- `UniformGrid` (equal cells)
- `WrapPanel` (wrap + item size + spacing)

## Rendering Backends

Rendering is abstracted through:
- `IGraphicsFactory` / `IGraphicsContext`

The sample defaults to Direct2D, with a GDI backend also available.

## Platform Abstraction

Windowing and the message loop are abstracted behind a platform layer. The current implementation is Windows-only (`Win32PlatformHost`), with the intent to add Linux/macOS backends later.

## DPI

The sample EXE includes an `app.manifest` enabling PerMonitorV2 DPI awareness:
- `samples/MewUI.Sample/app.manifest`

Internally, layout is in DIPs and graphics backends convert to device pixels (with pixel snapping) for crisp 1px borders.

## License

MIT. See `LICENSE`.
