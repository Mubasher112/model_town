using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Economy;
using Game.Data;
using Game.Inventory;

namespace Game.UI
{
    public class MarketUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject marketMainPanel;
        [SerializeField] private GameObject buyTabPanel;
        [SerializeField] private GameObject sellTabPanel;
        [SerializeField] private GameObject historyTabPanel;

        [Header("Item Detail / Quantity Modal")]
        [SerializeField] private GameObject quantityModalPanel;
        [SerializeField] private Text modalTitleText;
        [SerializeField] private Text modalPriceText;
        [SerializeField] private Text modalQuantityText;
        [SerializeField] private Text modalTotalPriceText;
        [SerializeField] private Text modalStockText;
        [SerializeField] private Text modalStorageText;
        [SerializeField] private Button confirmActionButton;
        [SerializeField] private Text confirmActionButtonText;

        [Header("Notifications")]
        [SerializeField] private Text notificationText;

        private MarketManager _marketManager;
        private InventoryManager _inventoryManager;

        private MarketItemConfig _selectedItemConfig;
        private TransactionType _selectedTransactionType = TransactionType.Buy;
        private int _selectedQuantity = 1;
        private bool _isProcessingTransaction = false;

        public void Initialize(MarketManager marketManager, InventoryManager inventoryManager)
        {
            _marketManager = marketManager;
            _inventoryManager = inventoryManager;
            CloseAllPanels();
        }

        public void OpenMarket()
        {
            if (_marketManager == null) return;

            CloseAllPanels();
            if (marketMainPanel != null) marketMainPanel.SetActive(true);
            OpenBuyTab();
        }

        public void OpenBuyTab()
        {
            _selectedTransactionType = TransactionType.Buy;
            if (buyTabPanel != null) buyTabPanel.SetActive(true);
            if (sellTabPanel != null) sellTabPanel.SetActive(false);
            if (historyTabPanel != null) historyTabPanel.SetActive(false);
        }

        public void OpenSellTab()
        {
            _selectedTransactionType = TransactionType.Sell;
            if (buyTabPanel != null) buyTabPanel.SetActive(false);
            if (sellTabPanel != null) sellTabPanel.SetActive(true);
            if (historyTabPanel != null) historyTabPanel.SetActive(false);
        }

        public void OpenHistoryTab()
        {
            if (buyTabPanel != null) buyTabPanel.SetActive(false);
            if (sellTabPanel != null) sellTabPanel.SetActive(false);
            if (historyTabPanel != null) historyTabPanel.SetActive(true);
        }

        public void SelectItemForTransaction(string itemId, TransactionType type)
        {
            var config = MarketLibrary.GetItemConfig(itemId);
            if (config == null || _marketManager == null || _inventoryManager == null) return;

            _selectedItemConfig = config;
            _selectedTransactionType = type;
            _selectedQuantity = 1;

            if (quantityModalPanel != null) quantityModalPanel.SetActive(true);
            UpdateModalDisplay();
        }

        public void OnClickQuantityPlus()
        {
            int maxQty = GetMaxAllowedQuantity();
            _selectedQuantity = Mathf.Min(maxQty, _selectedQuantity + 1);
            UpdateModalDisplay();
        }

        public void OnClickQuantityMinus()
        {
            _selectedQuantity = Mathf.Max(1, _selectedQuantity - 1);
            UpdateModalDisplay();
        }

        public void OnClickQuantityMax()
        {
            _selectedQuantity = GetMaxAllowedQuantity();
            UpdateModalDisplay();
        }

        private int GetMaxAllowedQuantity()
        {
            if (_selectedItemConfig == null || _marketManager == null || _inventoryManager == null) return 1;

            if (_selectedTransactionType == TransactionType.Buy)
            {
                var listing = _marketManager.GetListing(_selectedItemConfig.ItemId);
                int stock = listing != null ? listing.CurrentStock : 0;
                int freeStorage = _inventoryManager.MaxCapacity - _inventoryManager.CurrentCount;
                return Mathf.Max(1, Mathf.Min(stock, freeStorage));
            }
            else
            {
                int owned = _inventoryManager.GetQuantity(_selectedItemConfig.ItemId);
                return Mathf.Max(1, owned);
            }
        }

        private void UpdateModalDisplay()
        {
            if (_selectedItemConfig == null) return;

            long unitPrice = _selectedTransactionType == TransactionType.Buy ? _selectedItemConfig.BaseBuyPrice : _selectedItemConfig.BaseSellPrice;
            long totalPrice = unitPrice * _selectedQuantity;

            if (modalTitleText != null) modalTitleText.text = $"{_selectedTransactionType.ToString().ToUpper()} {_selectedItemConfig.Name}";
            if (modalPriceText != null) modalPriceText.text = $"Unit Price: {unitPrice} Coins";
            if (modalQuantityText != null) modalQuantityText.text = _selectedQuantity.ToString();
            if (modalTotalPriceText != null) modalTotalPriceText.text = $"Total: {totalPrice:N0} Coins";

            var listing = _marketManager.GetListing(_selectedItemConfig.ItemId);
            if (modalStockText != null) modalStockText.text = $"Market Stock: {listing?.CurrentStock ?? 0}";
            if (modalStorageText != null) modalStorageText.text = $"Storage: {_inventoryManager.CurrentCount} / {_inventoryManager.MaxCapacity}";

            if (confirmActionButtonText != null)
            {
                confirmActionButtonText.text = _selectedTransactionType == TransactionType.Buy ? "CONFIRM PURCHASE" : "CONFIRM SALE";
            }
        }

        public void OnClickExecuteTransaction()
        {
            if (_isProcessingTransaction || _selectedItemConfig == null || _marketManager == null) return;

            // Double-tap prevention flag
            _isProcessingTransaction = true;

            MarketOperationResult result;
            if (_selectedTransactionType == TransactionType.Buy)
            {
                result = _marketManager.BuyItem(_selectedItemConfig.ItemId, _selectedQuantity);
            }
            else
            {
                result = _marketManager.SellItem(_selectedItemConfig.ItemId, _selectedQuantity);
            }

            if (result == MarketOperationResult.Success)
            {
                if (quantityModalPanel != null) quantityModalPanel.SetActive(false);
            }
            else
            {
                ShowNotification($"Transaction failed: {result}");
            }

            _isProcessingTransaction = false;
        }

        private void ShowNotification(string msg)
        {
            if (notificationText != null)
            {
                notificationText.text = msg;
                CancelInvoke(nameof(ClearNotification));
                Invoke(nameof(ClearNotification), 3.0f);
            }
        }

        private void ClearNotification()
        {
            if (notificationText != null) notificationText.text = string.Empty;
        }

        public void CloseAllPanels()
        {
            if (marketMainPanel != null) marketMainPanel.SetActive(false);
            if (buyTabPanel != null) buyTabPanel.SetActive(false);
            if (sellTabPanel != null) sellTabPanel.SetActive(false);
            if (historyTabPanel != null) historyTabPanel.SetActive(false);
            if (quantityModalPanel != null) quantityModalPanel.SetActive(false);
        }
    }
}
