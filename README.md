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
├── Data/        # Data definitions (Buildings, Crops, Recipes, Orders, Residents, Items, LandExpansions, AdventureData, ToolData, SocialData, MarketData)
├── Economy/     # EconomyManager, MarketManager for centralized coin/gem/XP & market transactions
├── Farming/     # FarmManager, FieldInstance, CropGrowthSystem, FieldStates
├── Input/       # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/   # Reusable InventoryManager with type and capacity limits
├── Orders/      # OrderManager, OrderInstance, OrderGenerator, OrderTypes, OrderHistory
├── Platforms/   # PlatformServiceFactory and Platform implementations
├── Player/      # PlayerProfile (Level, XP, Coins, Gems)
├── Production/  # ProductionManager, ProductionJob, ProductionBuildingInstance, ProductionStates
├── Residents/   # PopulationManager, HappinessManager, ResidentInstance, HappinessModifier, PopulationStats
├── Save/        # LocalSaveSystem (Versioned JSON save v11, corruption recovery)
├── Services/    # EnergyManager, ToolService, ExplorationService, MockSocialService, ISocialService, ILeaderboardService
├── Social/      # SocialManager (TownMode transitions, Read-Only Visit Mode, Appreciations, Gifts)
├── Tests/       # Automated NUnit test suite & Assembly Definition
├── UI/          # Mobile UI components, BuildMenuUIController, BuildingInfoUIController, FarmingUIController, ProductionUIController, OrdersUIController, TownOverviewUIController, LandExpansionUIController, AdventureUIController, SocialUIController, MarketUIController, DevDebugToolsHandler
└── World/       # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager, WorldRenderer, ResidentWorldRenderer
```

---

## 3. Core Gameplay Loop Integration

```
Farm & Mine → Produce Goods → Trade at Town Market → Earn Coins → Expand Town & Connect Socially
```

1. **Farming & Adventure**: Harvest Wheat, Corn, Carrots, Sugarcane, Tomatoes, or gather Wood, Stone, Clay, Ore, Crystals.
2. **Production**: Process raw goods into manufactured products (Flour, Bread, Animal Feed, Sugar, Milk, Bricks).
3. **Town Market & Economy**: Buy missing resources or sell surplus products at the Town Market.
4. **Orders & Delivery**: Fulfill customer orders via the Town Market delivery location.
5. **Expansion & Social**: Expand town land, build housing/facilities, and interact with friends.

---

## 4. Market & Trading Economy Architecture

### Town Market Building Integration
- **Town Market Building**: Placeable/constructible community building (`town_market`) that acts as the central interface for buying resources, selling products, viewing market stock, and tracking transaction history.

### Buy & Sell System Rules
- **Buy System**: Validates player level unlock, available market stock, inventory storage capacity, and coin balance before atomically deducting coins and adding items.
- **Sell System**: Validates player inventory ownership and level requirements before atomically removing items and adding coins.
- **Price Spread Safety**: Buy prices are set higher than sell prices (e.g., Buy Wheat: 10 Coins, Sell Wheat: 6 Coins) to prevent infinite currency creation loops.
- **Double-Tap Protection**: UI actions flag active transactions to prevent rapid double-tapping from executing duplicate purchases or sales.

### Timestamp-Based Offline Restocking
- **Stock Limits**: Items have configurable max stocks (e.g., Wheat max 100, Bread max 40).
- **UTC Offline Calculation**: Market stock regenerates over real-world time (e.g., +20 Wheat every 30 minutes) using UTC timestamps. Stock recalculates smoothly when the player returns offline.

---

## 5. Save System Schema (Version 11)

The save system supports automatic migration up to `SaveDataVersion = 11` (`save_v11.json`):
- Player Profile & Unlocked Zone IDs
- World Objects, Fields, Buildings, Roads
- Production Building Queues & Order History
- Resident Instances & Housing Assignments
- Adventure Grid, Energy State, Tool Durabilities
- Local Social Profile, Cached Friends, Blocked Players, Appreciated Towns
- **Market Stock Listings & Last Restock UTC Timestamps**
- **Market Transaction History Log**

---

## 6. Development & Debug Tools
`DevDebugToolsHandler` provides market debug shortcuts:
- **Restock All Market**: Instantly refills all market items to max stock.
- **Empty Market Stock**: Clears all market stock for testing stock depletion.
- **Clear Market History**: Resets transaction history logs.

---

## 7. Running Automated Tests
Run all 65 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 8. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
