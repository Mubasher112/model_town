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
├── Data/        # Data definitions (Buildings, Crops, Recipes, Orders, Residents, Items, LandExpansions, AdventureData, ToolData, SocialData, MarketData, QuestData)
├── Economy/     # EconomyManager, MarketManager for centralized coin/gem/XP & market transactions
├── Farming/     # FarmManager, FieldInstance, CropGrowthSystem, FieldStates
├── Input/       # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/   # Reusable InventoryManager with type and capacity limits
├── Orders/      # OrderManager, OrderInstance, OrderGenerator, OrderTypes, OrderHistory
├── Platforms/   # PlatformServiceFactory and Platform implementations
├── Player/      # PlayerProfile (Level, XP, Coins, Gems)
├── Production/  # ProductionManager, ProductionJob, ProductionBuildingInstance, ProductionStates
├── Quests/      # QuestManager (Main, Side, Daily, Milestone quest tracking)
├── Residents/   # PopulationManager, HappinessManager, ResidentInstance, HappinessModifier, PopulationStats
├── Save/        # LocalSaveSystem (Versioned JSON save v12, corruption recovery)
├── Services/    # EnergyManager, ToolService, ExplorationService, MockSocialService, ISocialService, ILeaderboardService
├── Social/      # SocialManager (TownMode transitions, Read-Only Visit Mode, Appreciations, Gifts)
├── Tests/       # Automated NUnit test suite & Assembly Definition
├── UI/          # Mobile UI components, BuildMenuUIController, BuildingInfoUIController, FarmingUIController, ProductionUIController, OrdersUIController, TownOverviewUIController, LandExpansionUIController, AdventureUIController, SocialUIController, MarketUIController, QuestUIController, DevDebugToolsHandler
└── World/       # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager, WorldRenderer, ResidentWorldRenderer
```

---

## 3. Core Gameplay Loop Integration

```
Build & Farm → Produce & Trade → Complete Orders → Complete Quests → Unlock & Expand
```

1. **Farming & Adventure**: Harvest Wheat, Corn, Carrots, Sugarcane, Tomatoes, or gather Wood, Stone, Clay, Ore, Crystals.
2. **Production & Market**: Process raw goods into manufactured products and trade at the Town Market.
3. **Orders & Deliveries**: Fulfill customer orders to earn coins, experience, and quest progress.
4. **Quests & Progression**: Follow the Main Quest Chain (Start Your Town → Grow a Crop → Start Production → Serve a Customer → Grow the Town), complete Side Quests, Daily Goals, and Milestones.
5. **Land & Population**: Expand town boundaries, construct housing/facilities, and interact with friends.

---

## 4. Quests, Goals & Progression System

### Quest Chains & Prerequisites
- **Main Quest Chain**: Guides early gameplay step-by-step (`Start Your Town` → `Grow Your First Crop` → `Start Production` → `Serve a Customer` → `Grow the Town`).
- **Chain Dependencies**: Subsequent quests stay locked until all prerequisite quests are completed and claimed.
- **Quest Categories**: Supports Main, Side, Daily, and Milestone quest categories.

### Event-Driven Objective Tracking
- **Objective Types**: Supports `Build`, `Upgrade`, `Plant`, `Harvest`, `Produce`, `Collect`, `Sell`, `Buy`, `Deliver`, `Population`, `Happiness`, and `Expansion` objective types.
- **Event Updates**: Gameplay actions trigger event updates (`OnGameplayEvent`) on active objectives rather than polling every frame.

### Atomic Reward Claiming & Storage Validation
- **Claim Flow**: `Completed` quests present a `CLAIM REWARD` action button.
- **Storage Protection**: If quest rewards include items and inventory is full, the claim is blocked with an informative message ("Storage is full! Free storage to claim reward.") without losing items.
- **Daily Reset**: Daily Goals reset every 24 hours based on UTC timestamps (`LastDailyResetUtcTicks`).

---

## 5. Save System Schema (Version 12)

The save system supports automatic migration up to `SaveDataVersion = 12` (`save_v12.json`):
- Player Profile & Unlocked Zone IDs
- World Objects, Fields, Buildings, Roads
- Production Building Queues & Order History
- Resident Instances & Housing Assignments
- Adventure Grid, Energy State, Tool Durabilities
- Local Social Profile, Cached Friends, Blocked Players, Appreciated Towns
- Market Stock Listings & Transaction History
- **Active Quests & Objective Progress**
- **Claimed Quest IDs Log**
- **Last UTC Daily Reset Ticks**

---

## 6. Development & Debug Tools
`DevDebugToolsHandler` provides quest debug shortcuts:
- **Dev Complete Quest Objective**: Instantly completes all objectives for a specified quest.
- **Dev Claim Quest Reward**: Instantly claims rewards for a completed quest.

---

## 7. Running Automated Tests
Run all 68 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 8. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
