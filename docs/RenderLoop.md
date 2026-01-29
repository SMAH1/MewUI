# Render Loop Concept

This document summarizes MewUI’s render-loop model after the recent changes. It is an internal guide for backend/platform behavior and scheduling.

---

## 1. Goals

- Keep the UI responsive by decoupling rendering from message processing.
- Support both:
  - **On-request rendering** (UI invalidation triggers render)
  - **Continuous rendering** (animation / max FPS scenarios)
- Allow backend-level vsync control and consistent behavior across Win32, X11, D2D, OpenGL, and GDI.

---

## 2. Modes

### 2.1 OnRequest (default)

- `Window.Invalidate()` / `RequestRender()` marks a window as needing render.
- Platform host waits for render requests or OS messages.
- When signaled, only **invalidated windows** render.
- This mirrors WPF-style “coalesced” rendering where multiple invalidations merge.

### 2.2 Continuous

- The host repeatedly renders **every window**, even if there is no invalidation.
- The render loop throttles by `TargetFps` (optional).  
  `TargetFps = 0` means “no limit”.
- This is intended for animations and profiling (Max FPS).

---

## 3. RenderLoopSettings

The loop is configured via `Application.Current.RenderLoopSettings`:

- `Mode` → `OnRequest` or `Continuous`
- `TargetFps` → frame cap (0 = unlimited)
- `VSyncEnabled` → backend present/swap behavior

These settings are read by the platform host and the graphics backend.

---

## 4. Backend Behavior

### 4.1 Direct2D

- Uses DXGI present options.
- `VSyncEnabled = false` → `PresentOptions = IMMEDIATELY`
- `VSyncEnabled = true` → default present (vsync controlled by DXGI/DWM)

### 4.2 OpenGL

- Uses platform swap interval (WGL/GLX).
- `VSyncEnabled = false` → `SwapInterval = 0`
- `VSyncEnabled = true` → default vsync (usually 1)

### 4.3 GDI

- GDI has no vsync; `VSyncEnabled` does not change behavior.
- Rendering still participates in on-request and continuous scheduling.

---

## 5. Rendering vs Message Loop

- **OnRequest**: render is triggered when the render-request flag is set.
- **Continuous**: render is performed every loop iteration (with throttling).
- In both modes, OS messages are still processed every loop.

The platform host avoids relying on WM_PAINT directly and uses “RenderIfNeeded” or “RenderNow” to draw without forcing WM_PAINT storms.

---

## 6. FPS & Diagnostics

- `Window.FrameRendered` fires at the end of each render frame.
- Sample and Gallery use this to compute and display FPS.
- In Continuous mode, `FrameRendered` should update every frame.

---

## 7. Design Notes

- The loop keeps rendering **separate from invalidation** to allow max-FPS mode.
- Render requests are **coalesced** in OnRequest mode to avoid redundant work.
- Continuous mode renders even without invalidation so animations can advance.

---

## 8. Future Extensions

- Retained/composition rendering layers could plug into the same loop.
- Animation scheduling can align to `TargetFps` for stable pacing.
- Per-window scheduling (e.g., only active window in continuous) can be added later.

