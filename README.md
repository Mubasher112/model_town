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
├── Production/   # ProductionManager, ProductionJob, ProductionBuildingInstance, ProductionStates
├── Save/         # LocalSaveSystem (Versioned JSON save v5, corruption recovery)
├── Services/     # Abstractions & GameTimeService (Auth, CloudSave, IAP, Ads, Analytics, UTC time)
├── Tests/        # Automated NUnit test suite & Assembly Definition
├── UI/           # Mobile UI components, BuildMenuUIController, BuildingInfoUIController, FarmingUIController, ProductionUIController, DevDebugToolsHandler
└── World/        # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager, WorldRenderer
```

---

## 3. Production & Factory System Architecture

### Production Gameplay Loop
`Grow Crops → Harvest → Process Resources → Collect Products → Store Products`

### Production Buildings & Initial Catalog
- **Feed Mill**: Converts Wheat x2 → Animal Feed x1 (15s, 8 XP)
- **Bakery**: Converts Wheat x2 → Flour x1 (20s, 10 XP) or Flour x1 + Sugar x1 → Bread x1 (30s, 18 XP)
- **Sugar Mill**: Converts Sugarcane x2 → Sugar x1 (25s, 15 XP)
- **Dairy Factory**: Converts Animal Feed x2 → Milk x1 (35s, 20 XP)

### Production States & Queue Machine
`ProductionJobState` enum (`Queued`, `Producing`, `Ready`, `Blocked`).
- **Queue Behavior**: When a job starts producing, required ingredients are verified and consumed from inventory. Queued jobs wait sequentially.
- **Timestamp-Based Offline Production**: Production progress is evaluated on-demand from persistent UTC start timestamps:
$$\text{Progress} = \frac{\text{CurrentUtcTicks} - \text{StartUtcTicks}}{\text{ProductionTimeSeconds} \times 10,000,000}$$
When returning to the game, completed jobs advance to `Ready` state and unlock subsequent queued jobs automatically.

### Multi-Step Production Chains
Outputs from one factory serve as inputs for secondary products (e.g., `Wheat` → `Flour` + `Sugar` → `Bread`).

---

## 4. Buildings & Construction System Architecture

### Construction Gameplay Loop
`Select Building → Preview Footprint → Place → Pay/Reserve Cost → Construct → Wait / Offline → Complete → Unlock Functionality + Award XP`

### Building Categories & Catalog
- **Residential**: Small House (2x2, Pop: +5), Family House (3x2, Pop: +12)
- **Storage**: Barn (3x3, Storage Bonus: +20)
- **Community**: Town Hall (3x3, Progression center)
- **Production**: Feed Mill, Bakery, Sugar Mill, Dairy Factory
- **Decoration**: Pine Tree (1x1), Flower Bed (1x1), Small Fountain (2x2)

---

## 5. Farming and Crop System Architecture

### Gameplay Loop
`Prepare Field → Plant Seed → Wait / Offline Growth → Grow → Harvest → Receive Items + XP`

### Crop Catalog
- **Wheat**: Growth 10s | Seed: `seed_wheat` | Harvest: `crop_wheat` (x2) | XP: 5 | Level 1
- **Corn**: Growth 30s | Seed: `seed_corn` | Harvest: `crop_corn` (x2) | XP: 12 | Level 2
- **Carrot**: Growth 60s | Seed: `seed_carrot` | Harvest: `crop_carrot` (x2) | XP: 20 | Level 3
- **Sugarcane**: Growth 120s | Seed: `seed_sugarcane` | Harvest: `crop_sugarcane` (x3) | XP: 35 | Level 5
- **Tomato**: Growth 180s | Seed: `seed_tomato` | Harvest: `crop_tomato` (x3) | XP: 50 | Level 7

---

## 6. Development & Debug Tools
`DevDebugToolsHandler` provides developer convenience methods during testing:
- **Instant Complete All Production Jobs**: Instantly advances active production jobs to `Ready`.
- **Give Materials & Ingredients**: Grants 50 wood, stone, wheat, and sugarcane.
- **Give Coins & Gems**: Grants 5,000 coins and 100 gems.
- **Give Seeds**: Adds 20 seeds for all crop types to inventory.
- **Instant Complete All Constructions**: Instantly completes active constructions and upgrades.
- **Instant Grow All Fields**: Instantly advances all planted crops to `Ready` state.
- **Unlock All Land**: Unlocks all land expansion zones instantly.

---

## 7. How to Extend Production Recipes
1. Define a new `RecipeConfig` in `RecipeLibrary` (`Assets/Game/Data/GameConfig.cs`):
```csharp
new RecipeConfig(
    "recipe_cheese",
    "Cheese",
    "dairy_factory",
    "item_cheese",
    outputQuantity: 1,
    productionTimeSeconds: 40f,
    xpReward: 25,
    unlockLevel: 5,
    new List<RecipeIngredient> { new RecipeIngredient("item_milk", 2) }
)
```
2. Call `ProductionManager.StartProductionJob(buildingInstanceId, "recipe_cheese")`.

---

## 8. Running Automated Tests
Run all 30 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 9. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
