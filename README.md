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
├── Data/        # Data definitions (Buildings, Crops, Recipes, Orders, Residents, Items, LandExpansions, AdventureData, ToolData, SocialData)
├── Economy/     # EconomyManager for centralized coin/gem/XP transactions
├── Farming/     # FarmManager, FieldInstance, CropGrowthSystem, FieldStates
├── Input/       # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/   # Reusable InventoryManager with type and capacity limits
├── Orders/      # OrderManager, OrderInstance, OrderGenerator, OrderTypes, OrderHistory
├── Platforms/   # PlatformServiceFactory and Platform implementations
├── Player/      # PlayerProfile (Level, XP, Coins, Gems)
├── Production/  # ProductionManager, ProductionJob, ProductionBuildingInstance, ProductionStates
├── Residents/   # PopulationManager, HappinessManager, ResidentInstance, HappinessModifier, PopulationStats
├── Save/        # LocalSaveSystem (Versioned JSON save v10, corruption recovery)
├── Services/    # EnergyManager, ToolService, ExplorationService, MockSocialService, ISocialService, ILeaderboardService
├── Social/      # SocialManager (TownMode transitions, Read-Only Visit Mode, Appreciations, Gifts)
├── Tests/       # Automated NUnit test suite & Assembly Definition
├── UI/          # Mobile UI components, BuildMenuUIController, BuildingInfoUIController, FarmingUIController, ProductionUIController, OrdersUIController, TownOverviewUIController, LandExpansionUIController, AdventureUIController, SocialUIController, DevDebugToolsHandler
└── World/       # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager, WorldRenderer, ResidentWorldRenderer
```

---

## 3. Core Gameplay Loop Integration

```
Explore & Mine → Process Resources → Build Town & Expand → Connect Socially & Visit Friends
```

1. **Adventure & Mining**: Unlock "Ancient Valley", mine Stone, Wood, Clay, Ore, Crystals using tools and ⚡ energy.
2. **Farming & Production**: Process raw resources into manufactured products (Flour, Bread, Bricks).
3. **Construction & Expansion**: Expand land, build housing, community facilities, and workshops.
4. **Social & Friends**: Search players, send/accept friend requests, send daily gifts, appreciate towns, and visit friend towns in read-only snapshot mode without editing their world.

---

## 4. Social & Friends Architecture

### Service Interfaces & Offline Resiliency
- **`ISocialService`**: Asynchronous interface for `GetMyProfile`, `UpdateProfile`, `SearchPlayers`, `GetFriendsList`, `SendFriendRequest`, `AcceptFriendRequest`, `RejectFriendRequest`, `RemoveFriend`, `VisitTown`, `AppreciateTown`, `SendGift`, `BlockPlayer`, `ReportPlayer`.
- **`MockSocialService`**: In-memory local social provider for offline testing and development with pre-configured mock player towns (`Sunny Valley`, `Green Farm`, `Happy Town`, `River Bend`).
- **Offline Behavior**: When `ISocialService.IsOnline == false`, social requests fail gracefully with user-friendly messages while local single-player town gameplay, farming, and adventure remain 100% functional.

### Read-Only Town Visit Mode
- **Snapshot Representation**: `TownSnapshot` encapsulates building positions, road grids, population, and happiness stats without exposing private tokens or auth credentials.
- **`GameTownMode.FriendVisit`**: Switching town mode disables building placement, road editing, crop harvesting, and inventory changes, displaying a "VISITING [Player Name] (Read-Only)" UI banner with a single-tap "Return to My Town" option.

---

## 5. Save System Schema (Version 10)

The save system supports automatic migration up to `SaveDataVersion = 10` (`save_v10.json`):
- Player Profile & Unlocked Zone IDs
- World Objects, Fields, Buildings, Roads
- Production Building Queues & Order History
- Resident Instances & Housing Assignments
- Adventure Grid, Energy State, Tool Durabilities
- **Local Social Profile (Display Name, Avatar ID)**
- **Cached Friend Relationships**
- **Blocked Player IDs**
- **Claimed Gift IDs & Appreciated Town IDs**

---

## 6. Development & Debug Tools
`DevDebugToolsHandler` provides social debug shortcuts:
- **Simulate Offline / Online**: Toggles network availability for social services.
- **Send Mock Friend Request**: Sends a friend request to `p_sunny`.
- **Accept All Requests**: Auto-accepts incoming friend requests.
- **Visit Mock Town**: Loads Sunny Valley in Read-Only Visit Mode.
- **Return From Visit**: Returns to player's home town.

---

## 7. Running Automated Tests
Run all 58 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 8. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
