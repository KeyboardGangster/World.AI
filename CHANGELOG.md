# Changelog
## [0.3a] First Commit - 03-04-2023
### Added

 - Chunks
 - Noise-based world-generator
 - Noise-based biome-generator
 - Flood-fill-based biome-blending
 - Placeholder textures-generation for chunks.

## [0.3b] Switch to Terrain - 03-04-2023
### Changed
 - Generated heightmap is now used with Unity's Terrain instead of own chunks-implementation.

### Removed
 - Placeholder textures as Unity's Terrain now needs Terrain Layers instead.
 - Chunks since switching to Unity's Terrain makes them obsolete.

## [0.3c] Added Terrain Layers - 04-04-2023
### Added
 - Terrain Layers for biomes, for steep terrain and for under water.
 - Terrain-Layer-blending between biomes.

## [0.4] Trees Update - 16-04-2023
## Added

 - Several trees using Speedtree8-shaders. Trees can be assigned to biomes for generation.
 - Tree-LODs with simple billboards.
 - Entity-placer - currently only places trees.
 - Added toy-scale which increases world-size by scaling everything down. Entity-placement might need to be lowered for toy-scale as it bypassed safe LOD-settings leading to massive lag.
## Changed
 - World-scale now scales Terrain to normal scale. Previous world-scale functionality is now implemented via the added toy-scale.
 - Currently capped world-scale and toy-scale to 20 which corresponds to 20² * 20² = 160.000 km² of terrain. This is done so tree-placement/ general world-generation doesn't crash unity. World should be chunked into multiple Terrains in the future to prevent this limitation.
## Removed
 - Custom AnimationCurves for each biome used for biome-blending as the results were negligible and perfomance bad.
 - Several deprecated BiomeData parameters like name and feature. These will be reimplemented differently in the future when necessary.

#### Caution is advised when using world-scale and toy-scale. Massive worlds can lag and take several minutes to generate or crash unity entirely.
