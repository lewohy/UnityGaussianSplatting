# Sort-Free Gaussian Splatting Transparency Problem

## Summary

Sort-Free Gaussian Splatting (SortFreeGS) uses **Weighted Sum Rendering (WSR)** instead of traditional alpha blending in order to eliminate depth sorting.

Unlike alpha compositing, WSR does not explicitly model transmittance. Instead, all Gaussian contributions are accumulated using learned weights and normalized afterward.

As a result, **dark foreground objects may appear partially transparent when placed in front of very bright backgrounds.**

---

## Symptoms

The following artifacts may be observed easily when:
- foreground objects are dark,
- background objects are bright,
- and many splats overlap.

This has been observed as:
- Dark stems become partially transparent.
- Bright flowers or grass behind the stems become partialy visible.
- Thin dark structures lose their apparent opacity.

---

## Cause

SortFreeGS approximates alpha compositing using weighted accumulation:

```
AccumColor += Color * Contribution
AccumWeight += Contribution

FinalColor = AccumColor / AccumWeight
```

Unlike standard alpha blending,

```
C = c₁α₁ + c₂α₂(1−α₁) + ...
```

Weighted Sum Rendering does **not** explicitly represent accumulated transmittance.

Therefore, contributions from background splats remain in the weighted average instead of being completely occluded by foreground splats.

When the foreground is dark and the background is bright, this averaging causes the foreground to appear visually transparent.

---

## Investigation Results

The renderer implementation was verified by checking:

- Composite shader normalization
- Gamma/Linear conversion
- Background weight
- Depth correction
- View-dependent opacity (vi)
- Contribution computation
- Weight saturation
- Blend state

None of these modifications significantly removed the transparency artifact.

Debug visualization showed:

- `depthWeight` behaves correctly.
- `vi` is correctly evaluated and is relatively high on foreground stems.
- The final contribution remains relatively small compared to the accumulated background contribution.

This indicates that the artifact is **not caused by an implementation error in the renderer**, but rather by the learned weighting produced by the trained SortFreeGS model.

---

## Relation to the Original Paper

The original SortFreeGS paper explicitly reports this limitation:

> *"One type of failure that is easy to identify visually is apparent transparency of dark objects when in front of a very light background."*

The authors further state that this artifact is likely due to **suboptimal training**, not the rendering algorithm itself.

Therefore, this behavior is expected for some scenes.

---

## Possible Improvements

Potential directions for reducing this artifact include:

- Retraining with improved opacity supervision.
- Improving the view-dependent opacity predictor.
- Increasing the robustness of learned weights for dark foreground objects.
- Hybrid rendering approaches (partial depth sorting + WSR).
- Additional occlusion-aware loss functions during training.

These approaches require changes to the training pipeline rather than the Unity renderer.

---

## Conclusion

The transparency of dark foreground objects observed in this project is considered a known limitation of the current SortFreeGS model.

As long as the renderer faithfully implements the weighted accumulation described in the paper, this artifact should be attributed primarily to the learned model rather than to the rendering implementation.