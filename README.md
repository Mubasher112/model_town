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
├── Data/         # Data definitions (Buildings, Crops, Recipes, Orders, Residents, Items)
├── Economy/      # EconomyManager for centralized coin/gem/XP transactions
├── Farming/      # FarmManager, FieldInstance, CropGrowthSystem, FieldStates
├── Input/        # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/    # Reusable InventoryManager with type and capacity limits
├── Orders/       # OrderManager, OrderInstance, OrderGenerator, OrderTypes, OrderHistory
├── Platforms/    # PlatformServiceFactory and Platform implementations
├── Player/       # PlayerProfile (Level, XP, Coins, Gems)
├── Production/   # ProductionManager, ProductionJob, ProductionBuildingInstance, ProductionStates
├── Residents/    # PopulationManager, HappinessManager, ResidentInstance, HappinessModifier, PopulationStats
├── Save/         # LocalSaveSystem (Versioned JSON save v7, corruption recovery)
├── Services/     # Abstractions & GameTimeService (Auth, CloudSave, IAP, Ads, Analytics, UTC time)
├── Tests/        # Automated NUnit test suite & Assembly Definition
├── UI/           # Mobile UI components, BuildMenuUIController, BuildingInfoUIController, FarmingUIController, ProductionUIController, OrdersUIController, TownOverviewUIController, DevDebugToolsHandler
└── World/        # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager, WorldRenderer, ResidentWorldRenderer
```

---

## 3. Core Gameplay Loop Integration

```
Farm Crops → Harvest → Process Resources → Fulfill Orders → Earn Coins + XP → Build & Expand Town → Move In Residents & Boost Happiness
```

1. **Farming**: Plant crops (Wheat, Corn, Carrot, Sugarcane, Tomato) and harvest yield into inventory.
2. **Production**: Process raw crops in factories (Feed Mill, Bakery, Sugar Mill, Dairy Factory) into manufactured goods (Animal Feed, Flour, Bread, Sugar, Milk).
3. **Orders**: Deliver products to customers (Emma, John, Maya, Bob, Alex) to claim coin and XP rewards.
4. **Buildings & Housing**: Construct residential houses (Small House: +2 pop, Family House: +4 pop), community buildings (Town Hall, Fountain), and storage (Barn).
5. **Residents & Happiness**: Auto-spawn eligible residents when completed housing becomes available, assign residents to houses, and maintain town happiness (`0–100%`).

---

## 4. Population, Residents & Happiness System

### Population Tracking & Housing Assignment
- **Housing Capacity**: Derived exclusively from **completed** residential buildings. Under-construction buildings contribute 0 capacity.
- **Resident Auto-Spawning**: When completed housing capacity increases, `PopulationManager` automatically spawns eligible residents from `ResidentLibrary`.
- **Safe Reassignment on House Removal**: When a residential building is removed, assigned residents become `Unassigned`. `PopulationManager` automatically attempts to reassign them to available houses in town without silently deleting residents.

### Happiness System & Modifiers
`HappinessManager` calculates a deterministic score:
$$\text{Happiness} = \text{Base (50)} + \text{Community Facility Bonuses} - \text{Housing Shortage Penalties}$$
- **Rating Thresholds**:
  - `80–100%`: Excellent
  - `60–79%`: Good
  - `40–59%`: Average
  - `20–39%`: Low
  - `0–19%`: Critical
- **Housing Shortage Penalty**: Applies a -10 score penalty for each unhoused resident when population exceeds housing capacity.

---

## 5. Development & Debug Tools
`DevDebugToolsHandler` provides developer convenience methods during testing:
- **Add Resident**: Spawns a new resident into town.
- **Recalculate Pop & Happiness**: Re-evaluates town population stats, house assignments, and happiness scores.
- **Set Happiness Score**: Overrides town happiness score for testing.
- **Generate New Order**: Instantly generates a new customer order.
- **Fulfill First Order**: Auto-adds required items and fulfills the active order.
- **Give Coins & Gems**: Grants 5,000 coins and 100 gems.
- **Give Materials & Manufactured Goods**: Grants 50 wood, stone, wheat, sugarcane, flour, sugar, and bread.
- **Give Seeds**: Adds 20 seeds for all crop types to inventory.
- **Instant Complete All Productions & Constructions**: Instantly completes active jobs and constructions.
- **Instant Grow All Fields**: Instantly advances all planted crops to `Ready` state.
- **Unlock All Land**: Unlocks all land expansion zones instantly.

---

## 6. How to Extend Residents & Community Buildings
1. **Adding a New Resident Type**:
   - Register a `ResidentDefinition` in `ResidentLibrary` (`Assets/Game/Data/ResidentData.cs`).
2. **Adding a New Community Building**:
   - Add a `BuildingConfig` with `Category = BuildingCategory.Community` and specify `HappinessBonus` in `BuildingLibrary` (`Assets/Game/Data/GameConfig.cs`).

---

## 7. Running Automated Tests
Run all 38 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 8. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
