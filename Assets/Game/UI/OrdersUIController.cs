using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Orders;
using Game.Inventory;
using Game.Player;

namespace Game.UI
{
    public class OrdersUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject ordersPanel;
        [SerializeField] private Text notificationText;

        private OrderManager _orderManager;
        private InventoryManager _inventoryManager;
        private PlayerProfile _playerProfile;

        public void Initialize(OrderManager orderManager, InventoryManager inventoryManager, PlayerProfile playerProfile)
        {
            _orderManager = orderManager;
            _inventoryManager = inventoryManager;
            _playerProfile = playerProfile;

            if (_orderManager != null)
            {
                _orderManager.OnNotificationMessage += ShowNotification;
                _orderManager.OnOrdersUpdated += RefreshUI;
            }
        }

        private void OnDestroy()
        {
            if (_orderManager != null)
            {
                _orderManager.OnNotificationMessage -= ShowNotification;
                _orderManager.OnOrdersUpdated -= RefreshUI;
            }
        }

        public void OpenOrdersPanel()
        {
            if (ordersPanel != null) ordersPanel.SetActive(true);
            RefreshUI();
        }

        public void CloseOrdersPanel()
        {
            if (ordersPanel != null) ordersPanel.SetActive(false);
        }

        public void RefreshUI()
        {
            // Event-driven UI refresh hook
        }

        public void OnFulfillOrderClicked(string orderId)
        {
            if (_orderManager != null)
            {
                _orderManager.FulfillOrder(orderId);
                RefreshUI();
            }
        }

        public void ShowNotification(string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
            }
        }
    }
}
