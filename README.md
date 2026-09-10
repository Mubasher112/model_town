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
├── Data/         # Data definitions (Buildings, Crops, Recipes, Orders, Items)
├── Economy/      # EconomyManager for centralized coin/gem/XP transactions
├── Farming/      # FarmManager, FieldInstance, CropGrowthSystem, FieldStates
├── Input/        # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/    # Reusable InventoryManager with type and capacity limits
├── Orders/       # OrderManager, OrderInstance, OrderGenerator, OrderTypes, OrderHistory
├── Platforms/    # PlatformServiceFactory and Platform implementations
├── Player/       # PlayerProfile (Level, XP, Coins, Gems)
├── Production/   # ProductionManager, ProductionJob, ProductionBuildingInstance, ProductionStates
├── Save/         # LocalSaveSystem (Versioned JSON save v6, corruption recovery)
├── Services/     # Abstractions & GameTimeService (Auth, CloudSave, IAP, Ads, Analytics, UTC time)
├── Tests/        # Automated NUnit test suite & Assembly Definition
├── UI/           # Mobile UI components, BuildMenuUIController, BuildingInfoUIController, FarmingUIController, ProductionUIController, OrdersUIController, DevDebugToolsHandler
└── World/        # WorldGrid, PlacementValidator, ObjectPlacementManager, RoadManager, LandExpansionManager, WorldRenderer
```

---

## 3. Core Gameplay Loop Integration

```
Farm Crops → Harvest → Process Resources → Fulfill Orders → Earn Coins + XP → Upgrade & Expand Town
```

1. **Farming**: Plant crops (Wheat, Corn, Carrot, Sugarcane, Tomato) and harvest yield into inventory.
2. **Production**: Process raw crops in factories (Feed Mill, Bakery, Sugar Mill, Dairy Factory) into manufactured goods (Animal Feed, Flour, Bread, Sugar, Milk).
3. **Orders**: Deliver products to customers (Emma, John, Maya, Bob, Alex) to claim coin and XP rewards.
4. **Progression & Expansion**: Earn XP to level up, unlock new crops, recipes, and buildings, and spend coins to build structures or unlock land expansion zones.

---

## 4. Orders and Delivery System Architecture

### Order Types & Customer Profiles
- **Order Types**: `Customer`, `Town`, `Delivery`.
- **Customers**: Initial sample customers include `Emma` (Farmer), `John` (Baker), `Maya` (Shopkeeper), `Bob` (Builder), and `Alex` (Resident).

### Atomic Validation & Fulfillment
- **Atomic Validation**: Order requirements (e.g., `Bread x2` + `Sugar x1`) are fully verified against current inventory before any items are deducted.
- **Atomic Fulfillment**: All required items are removed, coin and XP rewards are credited to `PlayerProfile` and `EconomyManager`, the order is logged in history, and a replacement order is generated.
- **Failed Fulfillment**: If any requested item quantity is missing, no inventory is removed, no rewards are granted, and an explicit notification (`Not enough products`) is displayed.

### Order Generator & Expiration
- **Order Generator**: Dynamically generates orders matching player level and unlocked products.
- **Timestamp Expiration**: Orders track creation and expiration UTC timestamps (`CreationUtcTicks`, `ExpirationUtcTicks`). Expired orders are automatically pruned and replaced.

---

## 5. Development & Debug Tools
`DevDebugToolsHandler` provides developer convenience methods during testing:
- **Generate New Order**: Instantly generates a new level-appropriate customer order.
- **Fulfill First Order**: Automatically adds required order items to inventory and fulfills the active order.
- **Give Coins & Gems**: Grants 5,000 coins and 100 gems.
- **Give Materials & Manufactured Goods**: Grants 50 wood, stone, wheat, sugarcane, flour, sugar, and bread.
- **Give Seeds**: Adds 20 seeds for all crop types to inventory.
- **Instant Complete All Production Jobs**: Instantly completes active production jobs.
- **Instant Complete All Constructions**: Instantly completes active constructions and upgrades.
- **Instant Grow All Fields**: Instantly advances all planted crops to `Ready` state.
- **Unlock All Land**: Unlocks all land expansion zones instantly.

---

## 6. How to Extend Orders
1. Define a new `OrderTemplateConfig` in `OrderTemplateLibrary` (`Assets/Game/Data/GameConfig.cs`):
```csharp
new OrderTemplateConfig(
    "order_bread_milk",
    OrderType.Customer,
    minLevel: 4,
    new List<OrderRequirement> { new OrderRequirement("item_bread", 2), new OrderRequirement("item_milk", 1) },
    new OrderReward(coins: 200, xp: 40)
)
```
2. The `OrderGenerator` will automatically include the new template when generating orders for players at or above Level 4.

---

## 7. Running Automated Tests
Run all 30 automated unit tests directly via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 8. Android & iOS Build Processes
- **Android**: Configure `com.company.modeltown`, API 23+ minimum, IL2CPP ARM64, Landscape orientation. Build APK or AAB bundle.
- **iOS**: Configure `com.company.modeltown`, iOS 12.0+ minimum, iPhone + iPad, IL2CPP ARM64, Landscape orientation. Export to Xcode.
