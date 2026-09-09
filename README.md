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
├── Buildings/    # BuildingInstance, BuildingManager, BuildingStates
├── Camera/       # Mobile camera controller (Pan, Pinch-to-zoom, bounds)
├── Core/         # GameManager and Bootstrap initialization
├── Data/         # Data definitions (Buildings, Crops, Recipes, Items)
├── Economy/      # EconomyManager for centralized coin/gem/XP transactions
├── Farming/      # FarmManager, FieldInstance, CropGrowthSystem, FieldStates
├── Input/        # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/    # Reusable InventoryManager with type and capacity limits
├── Platforms/    # PlatformServiceFactory and Platform implementations
├── Player/       # PlayerProfile (Level, XP, Coins, Gems)
├── Save/         # LocalSaveSystem (Versioned JSON save v4, corruption recovery)
├── Services/     # Abstractions & GameTimeService (Auth, CloudSave, IAP, Ads, Analytics, UTC time)
├── Tests/        # Automated NUnit test suite & Assembly Definition
├── UI/           # Mobile UI components, BuildMenuUIController, BuildingInfoUIController, FarmingUIController, DevDebugToolsHandler
└── World/        # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager, WorldRenderer
```

---

## 3. Buildings & Construction System Architecture

### Construction Gameplay Loop
`Select Building → Preview Footprint → Place → Pay/Reserve Cost → Construct → Wait / Offline → Complete → Unlock Functionality + Award XP`

### Building Categories & Initial Catalog
- **Residential**: Small House (2x2, Pop: +5), Family House (3x2, Pop: +12)
- **Storage**: Barn (3x3, Storage Bonus: +20)
- **Community**: Town Hall (3x3, Progression center)
- **Decoration**: Pine Tree (1x1), Flower Bed (1x1), Small Fountain (2x2)

### Building States & Offline Construction
`BuildingState` enum (`Preview`, `UnderConstruction`, `Completed`, `Upgrading`, `Locked`).
Construction progress is evaluated on-demand from persistent UTC timestamps (`ConstructionStartUtcTicks` / `UpgradeStartUtcTicks`):
$$\text{Progress} = \frac{\text{CurrentUtcTicks} - \text{ConstructionStartUtcTicks}}{\text{ConstructionTimeSeconds} \times 10,000,000}$$
Completion is idempotent and awards configured XP rewards once upon completion.

### Upgrades & Town Capacities
- **Upgrades**: Increase building level, boost town population capacity, and expand barn inventory storage capacity.
- **Population & Storage**: Recalculated dynamically by `BuildingManager.RecalculateTownCapacities()`.

---

## 4. Farming and Crop System Architecture

### Gameplay Loop
`Prepare Field → Plant Seed → Wait / Offline Growth → Grow → Harvest → Receive Items + XP`

### Field States
`Empty` → `Planted` → `Growing` → `Ready` → `Harvested` → `Empty`.

### Crop Data & Definitions
- **Wheat**: Growth 10s | Seed: `seed_wheat` | Harvest: `crop_wheat` (x2) | XP: 5 | Level 1
- **Corn**: Growth 30s | Seed: `seed_corn` | Harvest: `crop_corn` (x2) | XP: 12 | Level 2
- **Carrot**: Growth 60s | Seed: `seed_carrot` | Harvest: `crop_carrot` (x2) | XP: 20 | Level 3
- **Sugarcane**: Growth 120s | Seed: `seed_sugarcane` | Harvest: `crop_sugarcane` (x3) | XP: 35 | Level 5
- **Tomato**: Growth 180s | Seed: `seed_tomato` | Harvest: `crop_tomato` (x3) | XP: 50 | Level 7

---

## 5. Development & Debug Tools
`DevDebugToolsHandler` provides developer convenience methods during testing:
- **Give Coins & Gems**: Grants 5,000 coins and 100 gems.
- **Give Materials**: Grants 50 wood and 50 stone raw materials.
- **Give Seeds**: Adds 20 seeds for all crop types to inventory.
- **Instant Complete All Constructions**: Instantly completes active constructions and upgrades.
- **Instant Grow All Fields**: Instantly advances all planted crops to `Ready` state.
- **Unlock All Land**: Unlocks all land expansion zones instantly.

---

## 6. How to Extend the Game
- **Adding a New Building**:
  1. Register a new `BuildingConfig` in `BuildingLibrary` (`Assets/Game/Data/GameConfig.cs`).
  2. Assign footprint dimensions, construction costs, materials, and population/storage bonuses.
- **Adding a New Crop**:
  1. Register a new `CropConfig` in `CropLibrary` (`Assets/Game/Data/GameConfig.cs`).

---

## 7. Running Automated Tests
Run all 24 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 8. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
