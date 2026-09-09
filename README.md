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
├── Input/        # MobileInputManager supporting Tap, Drag, Pinch, Long Press
├── Inventory/    # Reusable InventoryManager with type and capacity limits
├── Platforms/    # PlatformServiceFactory and Platform implementations
├── Player/       # PlayerProfile (Level, XP, Coins, Gems)
├── Save/         # LocalSaveSystem (Versioned JSON save, corruption recovery)
├── Services/     # Abstractions (Auth, CloudSave, IAP, Ads, Push Notifications, Analytics)
├── Tests/        # Automated NUnit test suite & Assembly Definition
├── UI/           # Responsive UI components & SafeAreaHandler for notches/Dynamic Island
└── World/        # Grid coordinates, tile management, terrain & placement rules
```

---

## 3. How to Open and Run the Game
1. Launch **Unity Hub** and select **Add project from disk**.
2. Select the repository root folder.
3. Open the main scene located under `Assets/Game/Scenes/Main.unity` (or initialize via `GameManager`).
4. Press **Play** in Unity Editor.
   - Use left mouse drag to pan the camera.
   - Use scroll wheel or pinch to zoom in/out.
   - Select tiles to interact with the world grid.

---

## 4. Running Automated Tests
The project contains automated unit tests covering Inventory, Economy, Player XP/Level progression, Save/Load serialization/corruption recovery, and World Grid coordinate/placement logic.

### In Unity Editor:
1. Open **Window > General > Test Runner**.
2. Select **EditMode** or **PlayMode** and click **Run All**.

### Via .NET CLI:
```bash
dotnet test ModelTown.Tests.csproj
```

---

## 5. Android Build Process
1. Open **File > Build Settings** in Unity.
2. Switch platform to **Android**.
3. Go to **Player Settings**:
   - **Package Name**: `com.company.modeltown`
   - **Minimum API Level**: Android 6.0 (API level 23)
   - **Target API Level**: Latest Installed / API 34
   - **Scripting Backend**: IL2CPP
   - **Target Architectures**: ARM64 enabled
   - **Default Orientation**: Landscape Left / Landscape Right
4. To build an APK for testing:
   - Click **Build** and choose destination `.apk`.
5. To build an AAB for Google Play Store release:
   - Check **Build App Bundle (Google Play)** in Build Settings and click **Build**.

---

## 6. iOS Build Process
1. Open **File > Build Settings** in Unity.
2. Switch platform to **iOS**.
3. Go to **Player Settings**:
   - **Bundle Identifier**: `com.company.modeltown`
   - **Target Minimum iOS Version**: 12.0
   - **Target Device**: iPhone + iPad
   - **Scripting Backend**: IL2CPP
   - **Architecture**: ARM64
   - **Default Orientation**: Landscape Left / Landscape Right
4. Click **Build** to generate the Xcode project directory.
5. Open the resulting `.xcodeproj` in Xcode on macOS.
6. Configure your Apple Developer Team, Provisioning Profile, and Signing Certificates in Xcode.
7. Build and deploy to device or archive for App Store upload.

---

## 7. Platform Abstractions & Cloud Services

All platform-dependent functionality is isolated behind interfaces in `Assets/Game/Services/`:
- `IAuthenticationService`
- `ICloudSaveService`
- `IPushNotificationService`
- `IInAppPurchaseService`
- `IAdsService`
- `IAnalyticsService`
- `IAchievementService`
- `ISocialService`

Core gameplay logic interacts only with these interfaces via `PlatformServiceFactory`, enabling seamless native integration (e.g., Google Play Games, Apple Game Center, Firebase, Unity IAP) without modifying gameplay scripts.
