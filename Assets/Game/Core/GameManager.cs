using System.IO;
using UnityEngine;
using Game.Player;
using Game.Economy;
using Game.Inventory;
using Game.World;
using Game.Save;
using Game.Platforms;
using Game.UI;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerProfile PlayerProfile { get; private set; }
        public EconomyManager EconomyManager { get; private set; }
        public InventoryManager InventoryManager { get; private set; }
        public WorldGrid WorldGrid { get; private set; }
        public LocalSaveSystem SaveSystem { get; private set; }
        public LocalMockPlatformServices PlatformServices { get; private set; }

        [SerializeField] private MobileUIManager uiManager;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "player_save.json");

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeGame()
        {
            PlatformServices = PlatformServiceFactory.CreatePlatformServices();
            SaveSystem = new LocalSaveSystem(SaveFilePath);

            LoadGame();

            if (uiManager != null)
            {
                uiManager.BindPlayerProfile(PlayerProfile);
            }
        }

        public void SaveGame()
        {
            var saveData = new SaveData
            {
                Version = LocalSaveSystem.CurrentSaveVersion,
                PlayerProfile = PlayerProfile,
                InventoryItems = InventoryManager.GetAllItems()
            };

            SaveSystem.Save(saveData);
        }

        public void LoadGame()
        {
            SaveData saveData = SaveSystem.Load();
            PlayerProfile = saveData.PlayerProfile ?? new PlayerProfile();
            EconomyManager = new EconomyManager(PlayerProfile);
            InventoryManager = new InventoryManager();

            if (saveData.InventoryItems != null)
            {
                foreach (var item in saveData.InventoryItems)
                {
                    InventoryManager.AddItem(item.ItemId, item.ItemName, item.Type, item.Quantity);
                }
            }

            WorldGrid = new WorldGrid(50, 50);

            if (saveData.PlacedObjects != null)
            {
                foreach (var obj in saveData.PlacedObjects)
                {
                    WorldGrid.PlaceObject(new Vector2Int(obj.X, obj.Y), obj.Width, obj.Height, obj.ObjectId);
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}
