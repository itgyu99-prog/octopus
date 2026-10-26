# 우주 폭발 V2 에셋

생성 방식: 내장 image_gen 도구. 최종 선택본은 새로 생성한 세 번째 결과이며, 앞의 두 안개가 넓은 결과는 사용하지 않았습니다.

- 원본: `Art/Celestial/V2/cosmic-impact-v2.png`
- 런타임: `Assets/Resources/Vfx/CosmicImpactV2.bytes` (원본 PNG와 동일한 바이트)
- 소비 코드: `CosmosArt.Impact`, `CosmosVisual`.
- 연출: 0.46초 동안 빠르게 퍼진 뒤 사라지는 광선, 작은 중심광, 별도 빛 조각. 대상 몬스터의 크기로 이펙트 크기를 정합니다.
- 원본의 투명 알파를 보존합니다. 런타임에서는 공유 스프라이트를 한 번만 로드합니다.

최종 생성 프롬프트:

```text
A square 2D VFX texture: ONLY 120 extremely thin, straight white hairline rays radiating from a tiny brilliant white point at exact center. Fine fiber-optic spark burst, monochrome ivory white/silver. The rays have randomly uneven lengths, fade gently toward tips, and taper to subpixel hairlines. Mostly empty transparent space BETWEEN the rays, like spokes made of light. Tiny white core is less than 4 percent of canvas. Ray ends stay inside central 75 percent of square canvas. Genuine transparent background everywhere except the thin rays and very tiny central glow. NO CLOUD, NO FOG, NO COLORED HAZE, NO FILLED STAR SHAPE, NO TRIANGLES, NO POLYGON, NO BLOOM AROUND RAYS, NO RING, NO TEXT. Must look like a photographic long-exposure spark starburst composed of separate hairlines, not a fantasy illustration. A compact spark impact sprite for a game.
```
