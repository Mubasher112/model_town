using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Social;
using Game.Data;
using Game.Services;

namespace Game.UI
{
    public class SocialUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject myProfilePanel;
        [SerializeField] private GameObject friendsListPanel;
        [SerializeField] private GameObject searchPanel;
        [SerializeField] private GameObject publicProfilePanel;
        [SerializeField] private GameObject visitBannerPanel;
        [SerializeField] private GameObject reportModalPanel;

        [Header("My Profile Screen")]
        [SerializeField] private Text myDisplayNameText;
        [SerializeField] private Text myAvatarText;
        [SerializeField] private Text myLevelText;
        [SerializeField] private Text myPopulationText;
        [SerializeField] private Text myHappinessText;
        [SerializeField] private Text myFriendsCountText;

        [Header("Edit Profile Form")]
        [SerializeField] private Text nameInputFieldText;
        [SerializeField] private Text selectedAvatarText;

        [Header("Public Profile Screen")]
        [SerializeField] private Text publicDisplayNameText;
        [SerializeField] private Text publicLevelText;
        [SerializeField] private Text publicPopulationText;
        [SerializeField] private Text publicHappinessText;
        [SerializeField] private Text publicFriendsCountText;
        [SerializeField] private Text publicAppreciationsText;
        [SerializeField] private Button addFriendButton;
        [SerializeField] private Button visitButton;
        [SerializeField] private Button appreciateButton;

        [Header("Visit Mode Banner")]
        [SerializeField] private Text visitingTownTitleText;

        [Header("Search Screen")]
        [SerializeField] private Text searchInputFieldText;
        [SerializeField] private Text searchStatusText;

        [Header("Notification")]
        [SerializeField] private Text notificationText;

        private SocialManager _socialManager;
        private ISocialService _socialService;
        private SocialProfile _selectedPublicProfile;

        public void Initialize(SocialManager socialManager, ISocialService socialService)
        {
            _socialManager = socialManager;
            _socialService = socialService;
            CloseAllPanels();
        }

        public void OpenMyProfile()
        {
            if (_socialManager == null) return;

            CloseAllPanels();
            if (myProfilePanel != null) myProfilePanel.SetActive(true);

            var p = _socialManager.MyProfile;
            if (myDisplayNameText != null) myDisplayNameText.text = p.DisplayName;
            if (myAvatarText != null) myAvatarText.text = p.AvatarId;
            if (myLevelText != null) myLevelText.text = $"Level {p.Level}";
            if (myPopulationText != null) myPopulationText.text = $"Population: {p.Population}";
            if (myHappinessText != null) myHappinessText.text = $"Happiness: {p.Happiness}%";
            if (myFriendsCountText != null) myFriendsCountText.text = $"Friends: {p.FriendsCount}";
        }

        public void OnClickSaveProfile()
        {
            if (_socialManager == null || nameInputFieldText == null) return;

            string newName = nameInputFieldText.text;
            string newAvatar = selectedAvatarText != null ? selectedAvatarText.text : "avatar_default";

            _socialManager.UpdateMyProfile(newName, newAvatar, (success, err) =>
            {
                if (success)
                {
                    ShowNotification("Profile updated!");
                    OpenMyProfile();
                }
                else
                {
                    ShowNotification($"Error: {err}");
                }
            });
        }

        public void OpenFriendsList()
        {
            if (_socialManager == null) return;

            CloseAllPanels();
            if (friendsListPanel != null) friendsListPanel.SetActive(true);

            _socialManager.RefreshFriendsList((success, friends, err) =>
            {
                if (!success)
                {
                    ShowNotification($"Friends offline/unavailable: {err}");
                }
            });
        }

        public void OpenSearchPanel()
        {
            CloseAllPanels();
            if (searchPanel != null) searchPanel.SetActive(true);
            if (searchStatusText != null) searchStatusText.text = "Type display name or ID to search...";
        }

        public void OnClickExecuteSearch()
        {
            if (_socialService == null || searchInputFieldText == null) return;

            string query = searchInputFieldText.text;
            _socialService.SearchPlayers(query, (success, results, err) =>
            {
                if (success)
                {
                    if (searchStatusText != null)
                    {
                        searchStatusText.text = results.Count > 0 ? $"Found {results.Count} players." : "No players found.";
                    }
                }
                else
                {
                    if (searchStatusText != null) searchStatusText.text = $"Search error: {err}";
                }
            });
        }

        public void OpenPublicProfile(string targetPlayerId)
        {
            if (_socialService == null) return;

            _socialService.GetPlayerProfile(targetPlayerId, (success, profile, err) =>
            {
                if (success && profile != null)
                {
                    _selectedPublicProfile = profile;
                    CloseAllPanels();
                    if (publicProfilePanel != null) publicProfilePanel.SetActive(true);

                    if (publicDisplayNameText != null) publicDisplayNameText.text = profile.DisplayName;
                    if (publicLevelText != null) publicLevelText.text = $"Level {profile.Level}";
                    if (publicPopulationText != null) publicPopulationText.text = $"Population: {profile.Population}";
                    if (publicHappinessText != null) publicHappinessText.text = $"Happiness: {profile.Happiness}%";
                    if (publicFriendsCountText != null) publicFriendsCountText.text = $"Friends: {profile.FriendsCount}";
                    if (publicAppreciationsText != null) publicAppreciationsText.text = $"Likes: {profile.AppreciationsCount}";
                }
                else
                {
                    ShowNotification($"Cannot view profile: {err}");
                }
            });
        }

        public void OnClickSendFriendRequest()
        {
            if (_selectedPublicProfile == null || _socialService == null) return;

            _socialService.SendFriendRequest(_selectedPublicProfile.PlayerId, (success, err) =>
            {
                if (success) ShowNotification("Friend request sent!");
                else ShowNotification($"Cannot send request: {err}");
            });
        }

        public void OnClickVisitTown()
        {
            if (_selectedPublicProfile == null || _socialManager == null) return;

            _socialManager.StartVisitingFriend(_selectedPublicProfile.PlayerId, (success, snapshot, err) =>
            {
                if (success && snapshot != null)
                {
                    CloseAllPanels();
                    if (visitBannerPanel != null) visitBannerPanel.SetActive(true);
                    if (visitingTownTitleText != null)
                    {
                        visitingTownTitleText.text = $"VISITING {snapshot.DisplayName.ToUpper()} (Read-Only)";
                    }
                    ShowNotification($"Now visiting {snapshot.DisplayName}'s town.");
                }
                else
                {
                    ShowNotification($"Cannot visit town: {err}");
                }
            });
        }

        public void OnClickReturnToOwnTown()
        {
            if (_socialManager != null)
            {
                _socialManager.ReturnToOwnTown();
            }
            CloseAllPanels();
            ShowNotification("Returned to your town.");
        }

        public void OnClickAppreciateTown()
        {
            if (_selectedPublicProfile == null || _socialManager == null) return;

            _socialManager.AppreciateTown(_selectedPublicProfile.PlayerId, (success, err) =>
            {
                if (success) ShowNotification("Appreciated town! 👍");
                else ShowNotification($"Cannot appreciate: {err}");
            });
        }

        public void OnClickBlockPlayer()
        {
            if (_selectedPublicProfile == null || _socialService == null) return;

            _socialService.BlockPlayer(_selectedPublicProfile.PlayerId, (success, err) =>
            {
                if (success)
                {
                    ShowNotification("Player blocked.");
                    CloseAllPanels();
                }
            });
        }

        public void OnClickReportPlayer()
        {
            if (_selectedPublicProfile == null || _socialService == null) return;

            _socialService.ReportPlayer(_selectedPublicProfile.PlayerId, "Inappropriate Content", "Reported from profile menu", (success, err) =>
            {
                if (success) ShowNotification("Report submitted. Thank you.");
            });
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
            if (myProfilePanel != null) myProfilePanel.SetActive(false);
            if (friendsListPanel != null) friendsListPanel.SetActive(false);
            if (searchPanel != null) searchPanel.SetActive(false);
            if (publicProfilePanel != null) publicProfilePanel.SetActive(false);
            if (visitBannerPanel != null) visitBannerPanel.SetActive(false);
            if (reportModalPanel != null) reportModalPanel.SetActive(false);
        }
    }
}
