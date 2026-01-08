# AVPro Video Transparent Solution Documentation (Bilingual)
# AVPro 透明视频解决方案文档 (中英双语)

**Date/日期**: 2025-12-31
**Project/项目**: Tianjin Coral

---

## 1. Problem Context / 问题背景

We encountered several artifacts when playing transparent MOV videos (ProRes 4444) using AVProVideo in Unity UI.
我们在使用 AVProVideo 在 Unity UI 中播放透明 MOV 视频（ProRes 4444）时遇到了以下问题：

1.  **Black Borders (黑边)**: 
    -   *Cause*: Pre-multiplied alpha or compression artifacts left dark outlines around subjects.
    -   *原因*: 预乘 Alpha 或压缩伪影导致物体周围出现黑色轮廓。
2.  **Jagged Edges (边缘锯齿)**:
    -   *Cause*: Simple alpha clipping (`clip(col.a - cutoff)`) created hard, pixelated edges.
    -   *原因*: 简单的 Alpha 裁剪 (`clip`) 导致边缘生硬、有像素感。
3.  **Visible Seams/Gaps (接缝/空隙)**:
    -   *Cause*: When looping or chaining segmented videos, slight alpha erosion created visible gaps between segments.
    -   *原因*: 循环播放或拼接分段视频时，Alpha 的轻微腐蚀导致分段之间出现可见空隙。
4.  **UI Fade Failure (UI 淡入淡出失效)**:
    -   *Cause*: `CanvasGroup` alpha was applied too early in the shader, causing `smoothstep` to discard semi-transparent pixels entirely.
    -   *原因*: `CanvasGroup` 的透明度在 Shader 中应用得太早，导致 `smoothstep` 将半透明像素完全丢弃。

---

## 2. Solution Architecture / 解决方案架构

The solution consists of a modified Shader and a helper Script.
解决方案由修改后的 Shader 和一个辅助脚本组成。

### A. Modified Shader / 修改后的 Shader
**File**: `Assets/AVProVideo/Runtime/Shaders/Resources/AVProVideo-Internal-UI-Default-Transparent.shader`

This shader replaces the default transparency logic with a 3-stage process:
此 Shader 将默认的透明度逻辑替换为三个步骤的处理流程：

1.  **Edge Dilation (边缘扩张/填缝)**:
    -   Samples 8 neighboring pixels (up, down, left, right + 4 diagonals).
    -   If the current pixel is transparent but a neighbor is opaque, it adopts the neighbor's color and alpha.
    -   *Effect*: "Grows" the opaque area outward to fill gaps.
    -   采样 8 个相邻像素（上下左右 + 4个对角）。
    -   如果当前像素透明但邻居不透明，则采用邻居的颜色和 Alpha。
    -   *效果*: 向外“生长”不透明区域以填充空隙。

2.  **Alpha Smoothstep (Alpha 平滑切边)**:
    -   Uses `smoothstep(_AlphaSmoothMin, _AlphaSmoothMax, col.a)` instead of a hard cutoff.
    -   *Effect*: Removes black borders while keeping edges soft (anti-aliased).
    -   使用 `smoothstep` 代替硬裁剪。
    -   *效果*: 去除黑边，同时保持边缘柔和（抗锯齿）。

3.  **Deferred Vertex Color Application (延迟应用顶点颜色)**:
    -   Multiplies `i.color` (from `CanvasGroup`) *after* the smoothstep and dilation logic.
    -   *Effect*: Ensures UI fading works correctly without being clipped.
    -   在 smoothstep 和 dilation 逻辑*之后*才乘以 `i.color`（来自 `CanvasGroup`）。
    -   *效果*: 确保 UI 淡入淡出正常工作，不会被错误裁剪。

**Core Code / 核心代码**:
```hlsl
// 1. Edge Dilation (8-tap sampling) / 边缘扩张（8点采样）
if (_EdgeDilation > 0.0 && col.a < _AlphaSmoothMax) 
{
    // ... Sample c1 to c8 ...
    half4 maxNeighbor = ...; // Find strongest neighbor
    if (maxNeighbor.a > 0.1)
    {
        col.rgb = lerp(col.rgb, maxNeighbor.rgb, 0.8);
        col.a = max(col.a, maxNeighbor.a); // Boost alpha
    }
}

// 2. Smoothstep / 平滑切边
col.a = smoothstep(_AlphaSmoothMin, _AlphaSmoothMax, col.a);

// 3. Apply CanvasGroup Alpha LAST / 最后应用 CanvasGroup 透明度
col *= i.color; 

// 4. Final Clip / 最终裁剪
clip(col.a - 0.001);
```

### B. Helper Script / 辅助脚本
**File**: `Assets/Scripts/AVProTransparentShaderHelper.cs`

This script manages the shader properties at runtime, specifically for dynamic adjustments.
此脚本用于在运行时管理 Shader 属性，特别是用于动态调整。

**Features / 功能**:
1.  **Dynamic Edge Dilation (动态边缘扩张)**:
    -   Uses an `AnimationCurve` to adjust dilation strength based on video playback progress.
    -   Useful for videos that need tighter edges at the start/end but softer edges in the middle.
    -   使用 `AnimationCurve` 根据视频播放进度调整扩张强度。
    -   适用于需要在开始/结束时边缘紧致，但在中间时边缘柔和的视频。

2.  **Optimization (性能优化)**:
    -   Uses `Graphic.material` (instanced) because Unity UI `Graphic` components **do not** support `MaterialPropertyBlock`.
    -   Caches property IDs (`Shader.PropertyToID`) for zero-allocation updates.
    -   使用 `Graphic.material`（实例化），因为 Unity UI `Graphic` 组件**不支持** `MaterialPropertyBlock`。
    -   缓存属性 ID 以实现无 GC 分配的更新。

---

## 3. Configuration Guide / 配置指南

Attach `AVProTransparentShaderHelper` to the GameObject with the AVPro Display component.
将 `AVProTransparentShaderHelper` 挂载到带有 AVPro Display 组件的物体上。

### Inspector Parameters / 面板参数

| Parameter (参数) | Recommended (推荐值) | Description (说明) |
| :--- | :--- | :--- |
| **Src Blend** | `One` | Source blend mode (usually One for premultiplied). <br> 源混合模式（预乘 Alpha 通常用 One）。 |
| **Dst Blend** | `OneMinusSrcAlpha` | Destination blend mode. <br> 目标混合模式。 |
| **Alpha Smooth Min** | `0.9` - `0.95` | Lower bound of alpha. Pixels below this become transparent. <br> Alpha 下界。低于此值的像素变透明。 |
| **Alpha Smooth Max** | `1.0` | Upper bound. Pixels above this are fully opaque. <br> Alpha 上界。高于此值的像素完全不透明。 |
| **Auto Adjust Edge Dilation** | `Checked` / `勾选` | Enable dynamic curve control. <br> 启用动态曲线控制。 |
| **Dilation Curve** | `U-Shape` / `U型` | Curve mapping time (0-1) to dilation strength (0-1). <br> 映射时间 (0-1) 到扩张强度 (0-1) 的曲线。 |
| **Boundary Value** | `2.0` - `3.0` | Max dilation (at curve peak). Use high values to close gaps at loops. <br> 最大扩张值（曲线峰值）。用于闭合循环处的空隙。 |
| **Median Value** | `0.0` - `0.5` | Min dilation (at curve trough). Keep low to avoid blurry edges in middle. <br> 最小扩张值（曲线谷底）。保持较低以避免中间画面模糊。 |

---

## 4. Troubleshooting History / 排错历史记录

### Issue 1: `Graphic.GetMaterialProperties` Compile Error
-   **Error**: `CS1061: 'Graphic' does not contain a definition for 'GetMaterialProperties'`.
-   **Solution**: Reverted to using `Graphic.material`. Unlike `MeshRenderer`, Unity's UI system does not support `MaterialPropertyBlock` for per-instance overrides without material instantiation.
-   **错误**: `Graphic` 组件没有 `GetMaterialProperties` 方法。
-   **解决**: 回退到使用 `Graphic.material`。与 `MeshRenderer` 不同，Unity UI 系统不支持 `MaterialPropertyBlock`。

### Issue 2: CanvasGroup Fade Not Working
-   **Symptom**: Changing `CanvasGroup` alpha caused the video to disappear instantly instead of fading.
-   **Diagnosis**: Shader multiplied `i.color` (0.5) before `smoothstep(0.9, 1.0)`. Since 0.5 < 0.9, the result was 0.
-   **Solution**: Moved `col *= i.color` to the end of the fragment shader.
-   **症状**: 改变 `CanvasGroup` 透明度导致视频直接消失而不是渐变。
-   **诊断**: Shader 在 `smoothstep(0.9, 1.0)` 之前乘以了 `i.color` (0.5)。因为 0.5 < 0.9，结果变为 0。
-   **解决**: 将 `col *= i.color` 移至片元着色器的末尾。

### Issue 3: Edge Dilation Ineffective
-   **Symptom**: Gaps remained visible even with dilation enabled.
-   **Diagnosis**: 4-tap sampling (cross shape) missed diagonal gaps. Also, alpha wasn't boosted enough.
-   **Solution**: Upgraded to 8-tap sampling (box shape) and forced alpha to `max(current, neighbor)`.
-   **症状**: 即使开启扩张，缝隙依然可见。
-   **诊断**: 4点采样（十字形）漏掉了对角线缝隙。且 Alpha 增强不足。
-   **解决**: 升级为 8点采样（盒形）并强制 Alpha 取 `max(当前, 邻居)`。
