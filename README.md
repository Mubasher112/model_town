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
├── Farming/      # FarmManager, FieldInstance, CropGrowthSystem, FieldStates
├── Input/        # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/    # Reusable InventoryManager with type and capacity limits
├── Platforms/    # PlatformServiceFactory and Platform implementations
├── Player/       # PlayerProfile (Level, XP, Coins, Gems)
├── Save/         # LocalSaveSystem (Versioned JSON save v3, corruption recovery)
├── Services/     # Abstractions & GameTimeService (Auth, CloudSave, IAP, Ads, Analytics, UTC time)
├── Tests/        # Automated NUnit test suite & Assembly Definition
├── UI/           # Mobile UI components, SafeAreaHandler, TileSelectionHandler, FarmingUIController, DevDebugToolsHandler
└── World/        # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager, WorldRenderer
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
- **ObjectFootprint**: Supports base dimensions (e.g. `1x1` fields, `2x2` or `3x2` buildings) and rotation angles (`0°`, `90°`, `180°`, `270°`).
- **PlacementValidator**: Evaluates placement validity returning `PlacementResult` (`Valid`, `InvalidOutOfBounds`, `InvalidOccupied`, `InvalidWater`, `InvalidLocked`, `InvalidNotBuildable`).
- **ObjectPlacementManager**: Manages instance placement, footprint occupation, object removal, and event triggers.

---

## 4. Farming and Crop System Architecture

### Gameplay Loop
`Prepare Field → Plant Seed → Wait / Offline Growth → Grow → Harvest → Receive Items + XP`

### Field States
Field instances (`FieldInstance`) manage a clean state machine:
- `Empty`: Field has no active crop planted.
- `Planted`: Seed planted, timestamp recorded (`PlantedUtcTicks`).
- `Growing`: Crop progressing toward maturity based on elapsed game time.
- `Ready`: Crop growth complete, ready for harvest.
- `Harvested`: Resources collected, field reset to `Empty`.

### Crop Data & Definitions
`CropLibrary` provides configurable crop settings:
- **Wheat**: Growth 10s | Seed: `seed_wheat` | Harvest: `crop_wheat` (x2) | XP: 5 | Level 1
- **Corn**: Growth 30s | Seed: `seed_corn` | Harvest: `crop_corn` (x2) | XP: 12 | Level 2
- **Carrot**: Growth 60s | Seed: `seed_carrot` | Harvest: `crop_carrot` (x2) | XP: 20 | Level 3
- **Sugarcane**: Growth 120s | Seed: `seed_sugarcane` | Harvest: `crop_sugarcane` (x3) | XP: 35 | Level 5
- **Tomato**: Growth 180s | Seed: `seed_tomato` | Harvest: `crop_tomato` (x3) | XP: 50 | Level 7

### Time & Offline Progression
`GameTimeService` tracks persistent UTC timestamps (`DateTime.UtcNow.Ticks`). Crop growth percentage is calculated on-demand from elapsed time:
$$\text{Progress} = \frac{\text{CurrentUtcTicks} - \text{PlantedUtcTicks}}{\text{GrowthTimeSeconds} \times 10,000,000}$$
When the player closes the game and returns later, crop growth continues seamlessly without requiring per-frame background timers or loops.

---

## 5. How to Open and Run the Game
1. Launch **Unity Hub** and select **Add project from disk**.
2. Select the repository root folder.
3. Open the main scene located under `Assets/Game/Scenes/Main.unity` (or initialize via `GameManager`).
4. Press **Play** in Unity Editor.
   - Touch/drag or left mouse drag to pan the camera.
   - Scroll wheel or pinch gesture to zoom in/out.
   - Tap empty soil fields to select seeds and plant crops.
   - Tap mature crops to harvest items and gain XP.

---

## 6. Development & Debug Tools
`DevDebugToolsHandler` provides developer convenience methods during testing:
- **Give Seeds**: Adds 20 seeds for all crop types to inventory.
- **Instant Grow All**: Instantly advances all planted crops to `Ready` state.
- **Reset Map**: Clears placed objects, fields, and roads.
- **Unlock All Land**: Unlocks all land expansion zones instantly.

---

## 7. How to Extend the Game
- **Adding a New Crop**:
  1. Register a new `CropConfig` in `CropLibrary` (`Assets/Game/Data/GameConfig.cs`).
  2. Add the corresponding seed item ID to `InventoryManager`.
- **Connecting to Production Buildings**:
  - Future factories/production facilities can query harvested crop items from `InventoryManager` (e.g. `crop_wheat`, `crop_corn`) as ingredients in `RecipeConfig`.

---

## 8. Running Automated Tests
Run all 18 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 9. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
