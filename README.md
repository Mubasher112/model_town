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
├── Buildings/    # BuildingInstance, BuildingManager, BuildingStates, BuildingAccessibilityService
├── Camera/       # Mobile camera controller (Pan, Pinch-to-zoom, bounds)
├── Core/         # GameManager and Bootstrap initialization
├── Data/         # Data definitions (Buildings, Crops, Recipes, Orders, Residents, Items, LandExpansions)
├── Economy/      # EconomyManager for centralized coin/gem/XP transactions
├── Farming/      # FarmManager, FieldInstance, CropGrowthSystem, FieldStates
├── Input/        # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/    # Reusable InventoryManager with type and capacity limits
├── Orders/       # OrderManager, OrderInstance, OrderGenerator, OrderTypes, OrderHistory
├── Platforms/    # PlatformServiceFactory and Platform implementations
├── Player/       # PlayerProfile (Level, XP, Coins, Gems)
├── Production/   # ProductionManager, ProductionJob, ProductionBuildingInstance, ProductionStates
├── Residents/    # PopulationManager, HappinessManager, ResidentInstance, HappinessModifier, PopulationStats
├── Save/         # LocalSaveSystem (Versioned JSON save v8, corruption recovery)
├── Services/     # Abstractions, ITransportNetwork & GameTimeService (Auth, CloudSave, IAP, Ads, Analytics, UTC time)
├── Tests/        # Automated NUnit test suite & Assembly Definition
├── UI/           # Mobile UI components, BuildMenuUIController, BuildingInfoUIController, FarmingUIController, ProductionUIController, OrdersUIController, TownOverviewUIController, LandExpansionUIController, DevDebugToolsHandler
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
5. **Land Expansion & Roads**: Expand town boundaries by purchasing expansion zones with level and coin requirements. Place roads that auto-connect (Isolated, Straight, Turn, TJunction, Cross) to keep buildings accessible and boost town happiness.
6. **Residents & Happiness**: Auto-spawn eligible residents when completed housing becomes available, assign residents to houses, and maintain town happiness (`0–100%`).

---

## 4. Land Expansion & Road Infrastructure

### Land Expansion Zones
- **Zone Definitions**: `LandExpansionLibrary` defines expansion zones with bounds (`X`, `Y`, `Width`, `Height`), coin costs, unlock level requirements, and minimum town population requirements.
- **Unlocking Flow**: `LandExpansionManager.TryPurchaseExpansion(...)` validates coins, level, and population before deducting costs, marking tiles as unlocked, and notifying `MobileCameraController` to expand zoom/pan boundaries.

### Auto-Connecting Road Network
- **Adjacency Logic**: `RoadManager` evaluates 4 cardinal neighbors (North, East, South, West) for any placed road tile and determines its connection geometry (`Isolated`, `DeadEnd`, `Straight`, `Turn`, `TJunction`, `Cross`).
- **Tile Integration**: Placing a road updates `TileType.Road` in `WorldGrid` and recalculates neighbors dynamically.

### Building Road Accessibility
- **Outer Border Check**: `BuildingAccessibilityService` inspects all tiles directly adjacent to a building's footprint (considering rotation and dimensions).
- **Accessibility Status**: If at least one adjacent tile contains a road, the building is marked accessible (`IsAccessible = true`).
- **Happiness Impact**: Inaccessible buildings trigger a `-5` happiness penalty per building in `HappinessManager`.

---

## 5. Population, Residents & Happiness System

### Population Tracking & Housing Assignment
- **Housing Capacity**: Derived exclusively from **completed** residential buildings. Under-construction buildings contribute 0 capacity.
- **Resident Auto-Spawning**: When completed housing capacity increases, `PopulationManager` automatically spawns eligible residents from `ResidentLibrary`.
- **Safe Reassignment on House Removal**: When a residential building is removed, assigned residents become `Unassigned`. `PopulationManager` automatically attempts to reassign them to available houses in town without silently deleting residents.

### Happiness System & Modifiers
`HappinessManager` calculates a deterministic score:
$$\text{Happiness} = \text{Base (50)} + \text{Community Facility Bonuses} - \text{Housing Shortage Penalties} - \text{Inaccessible Building Penalties}$$
- **Rating Thresholds**:
  - `80–100%`: Excellent
  - `60–79%`: Good
  - `40–59%`: Average
  - `20–39%`: Low
  - `0–19%`: Critical
- **Shortage & Accessibility Penalties**:
  - `-10` score penalty per unhoused resident when population exceeds completed housing capacity.
  - `-5` score penalty per inaccessible building not connected to the road network.

---

## 6. Save System Schema (Version 8)

The save system supports automatic migration up to `SaveDataVersion = 8` (`save_v8.json`):
- Player Profile (Level, XP, Coins, Gems)
- Unlocked Land Expansion Zone IDs
- Saved Road Tiles
- Placed World Objects & Terrain
- Fields (State, Crop ID, Planted UTC Ticks)
- Buildings (State, Level, Construction Ticks, Dimensions, Rotation)
- Production Buildings & Queue Jobs
- Active Orders & Customer Order History
- Resident Instances & House Assignments

---

## 7. Development & Debug Tools
`DevDebugToolsHandler` provides developer convenience methods during testing:
- **Unlock Selected Expansion**: Purchases the selected land expansion zone instantly.
- **Unlock All Land**: Unlocks all land expansion zones instantly.
- **Place Test Road Grid**: Lays down a test road strip across the grid.
- **Add Resident**: Spawns a new resident into town.
- **Recalculate Pop & Happiness**: Re-evaluates town population stats, house assignments, and happiness scores.
- **Generate New Order**: Instantly generates a new customer order.
- **Fulfill First Order**: Auto-adds required items and fulfills the active order.
- **Give Coins & Gems**: Grants 5,000 coins and 100 gems.
- **Give Seeds / Goods**: Grants seeds and manufactured goods for testing.
- **Instant Complete All**: Completes active jobs and constructions immediately.

---

## 8. How to Extend Land Expansions & Roads
1. **Adding a New Expansion Zone**:
   - Add a `LandExpansionConfig` entry in `LandExpansionLibrary` (`Assets/Game/Data/LandExpansionData.cs`).
2. **Implementing Transportation Delivery**:
   - Implement `ITransportNetwork` and `IRoadAccessible` interfaces in `Assets/Game/Services/ITransportNetwork.cs` for vehicle pathfinding and goods delivery.

---

## 9. Running Automated Tests
Run all 42 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 10. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
