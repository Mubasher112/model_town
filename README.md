# ModelTown - Cross-Platform Township-Style Mobile Game Foundation

ModelTown is an original mobile city-building and farming simulation game foundation built in Unity. The codebase is designed from day one to support **BOTH Android and iOS** from a single shared codebase.

---

## 1. Prerequisites & Unity Version
- **Unity Version**: Unity 2022.3.20f1 LTS
- **Target Framework**: .NET Standard 2.1 / .NET 8 for CLI test execution
- **Supported Platforms**:
  - Android (Phones, Tablets, ARM64)
  - iOS (iPhone, iPad)

---

## 2. Project Architecture & Directory Structure

```
Assets/Game/
├── Camera/       # Mobile camera controller (Pan, Pinch-to-zoom, bounds)
├── Core/         # GameManager and Bootstrap initialization
├── Data/         # ScriptableObject/Data definitions (Buildings, Crops, Recipes, Items)
├── Economy/      # EconomyManager for centralized coin/gem/XP transactions
├── Input/        # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/    # Reusable InventoryManager with type and capacity limits
├── Platforms/    # PlatformServiceFactory and Platform implementations
├── Player/       # PlayerProfile (Level, XP, Coins, Gems)
├── Save/         # LocalSaveSystem (Versioned JSON save, corruption recovery)
├── Services/     # Abstractions (Auth, CloudSave, IAP, Ads, Push Notifications, Analytics)
├── Tests/        # Automated NUnit test suite & Assembly Definition
├── UI/           # Mobile UI components, SafeAreaHandler, TileSelectionHandler, DevDebugToolsHandler
└── World/        # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager
```

---

## 3. World & Map System Architecture

### Grid & Terrain System
- **Grid Coordinates**: Integer grid system (`Vector2Int`) supporting configurable map dimensions (default 30x30, expandable to 50x50, 100x100+).
- **Tile Types**: Configurable via `TerrainConfigLibrary` (`Grass`, `Soil`, `Water`, `Road`, `Rock`, `Tree`, `Locked`, `Building`, `Obstacle`).
- **Data-Driven Attributes**: Tiles determine buildable, walkable, and occupied status from configuration rather than hardcoded conditionals.

### Coordinate Conversions
Bi-directional helper methods on `WorldGrid`:
- `GridToWorld`: Converts grid coordinates `(x, y)` to 2.5D isometric world coordinates.
- `WorldToGrid`: Converts 2.5D world positions back to integer grid coordinates.
- `WorldToScreen` / `ScreenToGrid`: Map screen touch/click inputs directly to grid tiles under camera pan/zoom settings.

### Object Placement & Footprints
- **ObjectFootprint**: Supports base dimensions (e.g. `2x2`, `3x2`) and rotation angles (`0°`, `90°`, `180°`, `270°`).
- **PlacementValidator**: Evaluates placement validity returning `PlacementResult` (`Valid`, `InvalidOutOfBounds`, `InvalidOccupied`, `InvalidWater`, `InvalidLocked`, `InvalidNotBuildable`).
- **ObjectPlacementManager**: Manages instance placement, footprint occupation, object removal, and event triggers.

### Roads & Land Expansion
- **RoadManager**: Handles placing and removing road tiles, updating underlying grid tile types to `TileType.Road`.
- **LandExpansionManager**: Defines land expansion zones (e.g. `zone_start`, `zone_north`, `zone_east`). Manages locked/unlocked tile states and developer unlock overrides.

---

## 4. How to Open and Run the Game
1. Launch **Unity Hub** and select **Add project from disk**.
2. Select the repository root folder.
3. Open the main scene located under `Assets/Game/Scenes/Main.unity` (or initialize via `GameManager`).
4. Press **Play** in Unity Editor.
   - Touch/drag or left mouse drag to pan the camera.
   - Scroll wheel or pinch gesture to zoom in/out.
   - Tap tiles to inspect coordinates, terrain type, buildable, and locked status.

---

## 5. Development & Debug Tools
`DevDebugToolsHandler` provides developer convenience methods during testing:
- **Reset Map**: Clears placed objects and roads.
- **Unlock All Land**: Unlocks all land expansion zones instantly.
- **Place Test Object**: Places a test 2x2 structure at a target tile.
- **Remove Object**: Removes any placed object at selected tile.

---

## 6. How to Extend the World System
- **Adding a New Tile Type**:
  1. Add an entry to the `TileType` enum in `Assets/Game/World/WorldGrid.cs`.
  2. Register a new `TerrainTypeConfig` in `TerrainConfigLibrary`.
- **Adding a New Placeable Object**:
  1. Define a `BuildingConfig` in `Assets/Game/Data/GameConfig.cs` or as a `ScriptableObject`.
  2. Instantiate an `ObjectFootprint(width, height)`.
  3. Call `ObjectPlacementManager.TryPlaceObject(...)`.

---

## 7. Running Automated Tests
Run unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 8. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
