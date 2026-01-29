# Layout (Measure / Arrange / Render) & DPI Pixel Snapping

This document describes the layout/rendering contract used by MewUI, and the rules to keep layout stable across DPIs while avoiding 1px clipping artifacts.

## Terms

- **DIP**: Device-independent pixels (logical units). In MewUI, most layout coordinates/sizes are expressed in DIP.
- **Px**: Device pixels (physical pixels). `px = dip * dpiScale`.
- **dpiScale**: `Dpi / 96.0`.
- **Constraint**: The `Size availableSize` passed into `Measure(...)`.
- **DesiredSize**: The element’s preferred size after `Measure`.
- **Bounds**: The element’s arranged rectangle after `Arrange`.

## Pipeline Overview

MewUI follows an Immediate Mode UI approach (draw each frame), but uses a retained layout tree for hit-testing and for reusing layout results between frames.

The pipeline is:

1) **Measure**: top-down, determines `DesiredSize`.
2) **Arrange**: top-down, assigns `Bounds`.
3) **Render**: draws using `Bounds` and current visual state.

### When each pass runs

- `InvalidateMeasure()` marks the element (and ancestors) as needing a new measure pass. This also implies arrange is invalid.
- `InvalidateArrange()` marks the element (and ancestors) as needing a new arrange pass, but does not necessarily require measure.
- `InvalidateVisual()` requests repaint but should not change layout.

**Rule of thumb**:
- **Scrolling** should be *Arrange + Render only* (offset changes typically require re-arranging children, not re-measuring).
- **Content/size-affecting property changes** require **Measure**.

## Measure

### Purpose

Measure calculates how large an element *wants* to be under a given constraint.

### Inputs / outputs

- Input: `availableSize` in **DIP**
- Output: `DesiredSize` in **DIP**

### Rules

- Measure must be **pure** with respect to layout state:
  - It must not mutate layout-affecting properties without checking for changes first (to avoid infinite invalidation loops).
  - It should not depend on previously arranged bounds.
- A re-measure can be skipped if:
  - the element is not measure-dirty, and
  - the constraint is unchanged.

### DPI rounding in Measure

Measure returns DIP sizes. The layout system may apply *layout rounding* to keep sizes stable across DPIs and avoid fractional edges that cause 1px seams.

The important principle is:
- Measure should not try to “manually pixel-snap” using parent coordinates.
- Prefer applying a consistent rounding policy at the layout system boundary (e.g., when assigning `DesiredSize`).

## Arrange

### Purpose

Arrange assigns the final position/size (`Bounds`) for each element.

### Inputs / outputs

- Input: `finalRect` in **DIP**
- Output: `Bounds` in **DIP**

### Rules

- Arrange may be skipped if:
  - the element is not arrange-dirty, and
  - the arranged bounds are identical.
- Arrange is responsible for placing children (e.g., content presenter, panels).

### DPI rounding in Arrange (pixel snapping)

The main source of visual artifacts (hairlines, 1px clipping) is inconsistent rounding between:
- parent’s computed child rect,
- the child’s own rounding,
- render-time clipping.

To avoid this:

- **Use a single dpiScale for the entire pass** (Window DPI).
- **Snap rectangle edges**, not just width/height:
  - snapping both left/right edges can shrink the size by 1px when rounding inward,
  - so choose inward/outward snapping intentionally depending on semantics.

Recommended semantics:

- **Bounds used for painting borders/background**: snap *edges* so strokes land on device pixels.
- **Viewport / clip rectangles**: snap **outward** so the clip does not become smaller than the intended viewport due to rounding.

## Render

### Purpose

Render draws the element using `Bounds` and current visual state.

### Rules

- Render must not perform layout. No Measure/Arrange calls inside Render.
- Render should draw using already-snapped geometry whenever possible.

### Clipping rules (1px artifacts)

Text, strokes, and antialiasing can extend half a pixel outside logical bounds. If an ancestor applies a clip exactly at the child bounds, that overhang can be clipped and look like “right/bottom 1px missing”.

Recommended approach:

- Content rendering inside a viewport should:
  1) compute the viewport rect in DIP,
  2) snap it to pixels (prefer outward snapping),
  3) apply clip,
  4) optionally expand the clip by **+1 device pixel** on the right/bottom when drawing 1px strokes/glyphs at the edge (if you know content may overhang).

This is the intent behind helpers like “expand clip by device pixels”.

## DPI propagation and caching

### DPI source of truth

- Window provides the effective DPI (`Dpi`, `DpiScale`).
- Children should use the same DPI for rounding decisions in Measure/Arrange/Render.

### Caching

Caches that depend on DPI (text measure cache, geometry cache, etc.) must be invalidated when:
- the element’s effective DPI changes,
- font-related properties change,
- wrap/constraint changes (for wrapped text).

## Scrolling: Layout expectations

### Offsets

- Store offsets in DIP conceptually, but you may keep internal values in pixels for stable snapping.
- Changing offsets should not require re-measure unless extent/viewport changes.

### What should update during scroll

1) Arrange children using `viewport - offset`.
2) Render the content under a viewport clip.
3) Update scrollbar ranges/values to match the current offset and viewport.

## Anti-patterns (cause performance problems or infinite invalidation)

- Setting layout-affecting properties during Measure/Arrange without comparing old/new values first.
- Triggering `InvalidateMeasure()` from Render.
- Recomputing DPI scale by walking the tree in hot paths without caching.
- Measuring on every scroll tick when only offsets changed.

