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
├── Adventure/   # AdventureManager, ResourceGatheringService, AdventureNodeInstance
├── Buildings/   # BuildingInstance, BuildingManager, BuildingStates, BuildingAccessibilityService
├── Camera/      # Mobile camera controller (Pan, Pinch-to-zoom, bounds switching)
├── Core/        # GameManager and Bootstrap initialization
├── Data/        # Data definitions (Buildings, Crops, Recipes, Orders, Residents, Items, LandExpansions, AdventureData, ToolData)
├── Economy/     # EconomyManager for centralized coin/gem/XP transactions
├── Farming/     # FarmManager, FieldInstance, CropGrowthSystem, FieldStates
├── Input/       # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/   # Reusable InventoryManager with type and capacity limits
├── Orders/      # OrderManager, OrderInstance, OrderGenerator, OrderTypes, OrderHistory
├── Platforms/   # PlatformServiceFactory and Platform implementations
├── Player/      # PlayerProfile (Level, XP, Coins, Gems)
├── Production/  # ProductionManager, ProductionJob, ProductionBuildingInstance, ProductionStates
├── Residents/   # PopulationManager, HappinessManager, ResidentInstance, HappinessModifier, PopulationStats
├── Save/        # LocalSaveSystem (Versioned JSON save v9, corruption recovery)
├── Services/    # EnergyManager, ToolService, ExplorationService, GameTimeService, Abstractions
├── Tests/       # Automated NUnit test suite & Assembly Definition
├── UI/          # Mobile UI components, BuildMenuUIController, BuildingInfoUIController, FarmingUIController, ProductionUIController, OrdersUIController, TownOverviewUIController, LandExpansionUIController, AdventureUIController, DevDebugToolsHandler
└── World/       # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager, WorldRenderer, ResidentWorldRenderer
```

---

## 3. Core Gameplay Loop Integration

```
Explore & Mine → Collect Raw Resources → Process in Factories → Fulfill Orders & Build Town → Expand Land & Roads
```

1. **Adventure & Mining**: Unlock "Ancient Valley" (Level 8, 1,000 Coins, 10 Pop). Move around a 25x25 grid map, discover hidden areas, spend ⚡ energy and tool durability (Pickaxe, Axe, Shovel) to mine Stone, Wood, Clay, Iron Ore, and Rare Crystals.
2. **Farming & Production**: Plant crops and convert raw materials (e.g. Clay x3 → Brick x1) into manufactured goods.
3. **Construction**: Use gathered resources (e.g. Wood x10 + Stone x5) to build specialized buildings like the Small Workshop.
4. **Orders & Economy**: Deliver products to customers for coins/XP rewards.
5. **Land & Population**: Expand town boundaries, construct housing, and manage residents and happiness.

---

## 4. Adventure, Mining & Energy System

### Energy Regeneration & Clamping
- **Energy Pool**: Default capacity of 20 ⚡ energy.
- **UTC Offline Regeneration**: Regenerates 1 ⚡ every 5 minutes (300 seconds) using UTC timestamps. Energy recalculates dynamically when the app opens or after returning from background.

### Tools & Durability
- **Tool Inventory**: Pickaxe (Max 30), Axe (Max 30), Shovel (Max 25).
- **Gathering Validation**: Gathering checks energy, tool availability, tool durability, and inventory space before deducting costs.

### Exploration & Fog of War
- **Exploration Map**: 25x25 grid containing Stone Deposits, Trees, Clay Pits, Iron Ore Veins, Crystal Geodes, Boulders, Fallen Logs, and Special Locations (Ancient Ruins, Abandoned Mine, Hidden Grove).
- **Discovery Radius**: Player movement reveals nearby hidden cells within a 3-cell radius.

---

## 5. Save System Schema (Version 9)

The save system supports automatic migration up to `SaveDataVersion = 9` (`save_v9.json`):
- Player Profile & Unlocked Zone IDs
- Main World Objects, Fields, Buildings, Roads
- Production Building Queues & Customer Order History
- Resident Instances & Housing Assignments
- **Adventure Unlocked State & Player Coordinates**
- **Energy State (Current Energy, Last Regen UTC Ticks)**
- **Tool Durability Instances**
- **Discovered Adventure Grid Cells**
- **Adventure Node States & Respawn UTC Timestamps**
- **Discovered Special Locations**

---

## 6. Development & Debug Tools
`DevDebugToolsHandler` provides developer convenience methods during testing:
- **Unlock Adventure**: Unlocks Ancient Valley adventure area instantly.
- **Reveal Adventure Map**: Uncovers all fog of war on the adventure grid.
- **Clear Adventure Obstacles**: Permanently clears all boulders and logs.
- **Refill Adventure Energy**: Sets energy to max 20 ⚡.
- **Give Adventure Resources**: Grants Stone, Wood, Clay, Iron Ore, Rare Crystals, and Bricks.
- **Give Coins / Gems / Seeds / Materials**: Standard resource shortcuts.

---

## 7. Running Automated Tests
Run all 50 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 8. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
