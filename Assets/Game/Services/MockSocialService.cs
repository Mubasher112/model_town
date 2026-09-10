using System;
using System.Collections.Generic;
using Game.Data;
using Game.Save;

namespace Game.Services
{
    public class MockSocialService : ISocialService, ILeaderboardService
    {
        private bool _isOnline = true;
        private SocialProfile _myProfile;
        private readonly Dictionary<string, SocialProfile> _allMockProfiles = new Dictionary<string, SocialProfile>();
        private readonly Dictionary<string, FriendRelationship> _relationships = new Dictionary<string, FriendRelationship>();
        private readonly Dictionary<string, TownSnapshot> _snapshots = new Dictionary<string, TownSnapshot>();
        private readonly Dictionary<string, int> _apprecations = new Dictionary<string, int>();
        private readonly List<SocialGift> _pendingGifts = new List<SocialGift>();
        private readonly HashSet<string> _blockedPlayers = new HashSet<string>();

        public bool IsOnline => _isOnline;

        public MockSocialService(SocialProfile myProfile = null)
        {
            _myProfile = myProfile ?? new SocialProfile("p_my_id", "Mayor's Valley", 1, 2, 100, "avatar_default");
            _allMockProfiles[_myProfile.PlayerId] = _myProfile;

            InitializeMockPlayers();
        }

        private void InitializeMockPlayers()
        {
            AddMockPlayer(new SocialProfile("p_sunny", "Sunny Valley", 15, 32, 95, "avatar_farmer"));
            AddMockPlayer(new SocialProfile("p_green", "Green Farm", 11, 20, 88, "avatar_baker"));
            AddMockPlayer(new SocialProfile("p_happy", "Happy Town", 9, 14, 82, "avatar_explorer"));
            AddMockPlayer(new SocialProfile("p_river", "River Bend", 20, 50, 98, "avatar_default"));
        }

        private void AddMockPlayer(SocialProfile p)
        {
            _allMockProfiles[p.PlayerId] = p;

            // Create a mock town snapshot
            var snap = new TownSnapshot(p.PlayerId, p.DisplayName, p.Level, p.Population, p.Happiness)
            {
                Buildings = new List<SavedBuilding>
                {
                    new SavedBuilding { InstanceId = "b1", BuildingId = "small_house", X = 10, Y = 10, State = 1 },
                    new SavedBuilding { InstanceId = "b2", BuildingId = "town_hall", X = 15, Y = 10, State = 1 },
                    new SavedBuilding { InstanceId = "b3", BuildingId = "bakery", X = 10, Y = 15, State = 1 }
                },
                RoadTiles = new List<SavedRoadTile> { new SavedRoadTile(10, 9), new SavedRoadTile(11, 9), new SavedRoadTile(12, 9) }
            };
            _snapshots[p.PlayerId] = snap;
        }

        public void SetOnline(bool isOnline) => _isOnline = isOnline;

        public void GetMyProfile(Action<bool, SocialProfile, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, null, "Offline"); return; }
            callback?.Invoke(true, _myProfile, null);
        }

        public void UpdateProfile(string displayName, string avatarId, Action<bool, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, "Offline"); return; }
            if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length < 3)
            {
                callback?.Invoke(false, "Display name must be at least 3 characters");
                return;
            }

            _myProfile.DisplayName = displayName.Trim();
            _myProfile.AvatarId = avatarId;
            callback?.Invoke(true, null);
        }

        public void SearchPlayers(string query, Action<bool, List<SocialProfile>, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, null, "Offline"); return; }
            var results = new List<SocialProfile>();
            if (string.IsNullOrWhiteSpace(query))
            {
                callback?.Invoke(true, results, null);
                return;
            }

            string q = query.Trim().ToLower();
            foreach (var p in _allMockProfiles.Values)
            {
                if (p.PlayerId == _myProfile.PlayerId) continue;
                if (_blockedPlayers.Contains(p.PlayerId)) continue;

                if (p.DisplayName.ToLower().Contains(q) || p.PlayerId.ToLower().Contains(q))
                {
                    results.Add(p);
                }
            }
            callback?.Invoke(true, results, null);
        }

        public void GetPlayerProfile(string targetPlayerId, Action<bool, SocialProfile, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, null, "Offline"); return; }
            if (_blockedPlayers.Contains(targetPlayerId))
            {
                callback?.Invoke(false, null, "Player is blocked");
                return;
            }

            if (_allMockProfiles.TryGetValue(targetPlayerId, out var p))
            {
                callback?.Invoke(true, p, null);
            }
            else
            {
                callback?.Invoke(false, null, "Player not found");
            }
        }

        public void GetFriendsList(Action<bool, List<FriendRelationship>, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, null, "Offline"); return; }
            var list = new List<FriendRelationship>(_relationships.Values);
            callback?.Invoke(true, list, null);
        }

        public void SendFriendRequest(string targetPlayerId, Action<bool, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, "Offline"); return; }
            if (targetPlayerId == _myProfile.PlayerId)
            {
                callback?.Invoke(false, "Cannot send friend request to yourself");
                return;
            }
            if (_blockedPlayers.Contains(targetPlayerId))
            {
                callback?.Invoke(false, "Player is blocked");
                return;
            }

            if (_relationships.TryGetValue(targetPlayerId, out var rel))
            {
                if (rel.Status == FriendStatus.Accepted) { callback?.Invoke(false, "Already friends"); return; }
                if (rel.Status == FriendStatus.PendingOutgoing) { callback?.Invoke(false, "Request already sent"); return; }
            }

            if (!_allMockProfiles.TryGetValue(targetPlayerId, out var targetP))
            {
                callback?.Invoke(false, "Player not found");
                return;
            }

            _relationships[targetPlayerId] = new FriendRelationship(targetPlayerId, targetP, FriendStatus.PendingOutgoing);
            callback?.Invoke(true, null);
        }

        public void AcceptFriendRequest(string targetPlayerId, Action<bool, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, "Offline"); return; }
            if (_relationships.TryGetValue(targetPlayerId, out var rel))
            {
                rel.Status = FriendStatus.Accepted;
                rel.TargetProfile.FriendsCount++;
                _myProfile.FriendsCount++;
                callback?.Invoke(true, null);
            }
            else
            {
                if (_allMockProfiles.TryGetValue(targetPlayerId, out var targetP))
                {
                    _relationships[targetPlayerId] = new FriendRelationship(targetPlayerId, targetP, FriendStatus.Accepted);
                    targetP.FriendsCount++;
                    _myProfile.FriendsCount++;
                    callback?.Invoke(true, null);
                }
                else
                {
                    callback?.Invoke(false, "No pending request found");
                }
            }
        }

        public void RejectFriendRequest(string targetPlayerId, Action<bool, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, "Offline"); return; }
            _relationships.Remove(targetPlayerId);
            callback?.Invoke(true, null);
        }

        public void RemoveFriend(string targetPlayerId, Action<bool, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, "Offline"); return; }
            if (_relationships.TryGetValue(targetPlayerId, out var rel) && rel.Status == FriendStatus.Accepted)
            {
                rel.TargetProfile.FriendsCount = Math.Max(0, rel.TargetProfile.FriendsCount - 1);
                _myProfile.FriendsCount = Math.Max(0, _myProfile.FriendsCount - 1);
            }
            _relationships.Remove(targetPlayerId);
            callback?.Invoke(true, null);
        }

        public void VisitTown(string targetPlayerId, Action<bool, TownSnapshot, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, null, "Offline"); return; }
            if (_blockedPlayers.Contains(targetPlayerId))
            {
                callback?.Invoke(false, null, "Cannot visit blocked player");
                return;
            }

            if (_snapshots.TryGetValue(targetPlayerId, out var snap))
            {
                callback?.Invoke(true, snap, null);
            }
            else
            {
                callback?.Invoke(false, null, "Town snapshot unavailable");
            }
        }

        public void AppreciateTown(string targetPlayerId, Action<bool, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, "Offline"); return; }
            if (targetPlayerId == _myProfile.PlayerId)
            {
                callback?.Invoke(false, "Cannot appreciate your own town");
                return;
            }
            if (_blockedPlayers.Contains(targetPlayerId))
            {
                callback?.Invoke(false, "Cannot appreciate blocked player's town");
                return;
            }

            if (!_apprecations.ContainsKey(targetPlayerId))
            {
                _apprecations[targetPlayerId] = 1;
                if (_allMockProfiles.TryGetValue(targetPlayerId, out var p))
                {
                    p.AppreciationsCount++;
                }
                callback?.Invoke(true, null);
            }
            else
            {
                callback?.Invoke(false, "Already appreciated this town today");
            }
        }

        public void SendGift(string targetPlayerId, string itemId, int quantity, Action<bool, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, "Offline"); return; }
            if (targetPlayerId == _myProfile.PlayerId)
            {
                callback?.Invoke(false, "Cannot send gift to yourself");
                return;
            }
            if (_blockedPlayers.Contains(targetPlayerId))
            {
                callback?.Invoke(false, "Cannot send gift to blocked player");
                return;
            }

            _pendingGifts.Add(new SocialGift("g_" + Guid.NewGuid().ToString().Substring(0, 5), _myProfile.PlayerId, _myProfile.DisplayName, itemId, quantity));
            callback?.Invoke(true, null);
        }

        public void GetPendingGifts(Action<bool, List<SocialGift>, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, null, "Offline"); return; }
            callback?.Invoke(true, new List<SocialGift>(_pendingGifts), null);
        }

        public void ClaimGift(string giftId, Action<bool, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, "Offline"); return; }
            int idx = _pendingGifts.FindIndex(g => g.GiftId == giftId);
            if (idx >= 0)
            {
                _pendingGifts.RemoveAt(idx);
                callback?.Invoke(true, null);
            }
            else
            {
                callback?.Invoke(false, "Gift not found");
            }
        }

        public void BlockPlayer(string targetPlayerId, Action<bool, string> callback)
        {
            _blockedPlayers.Add(targetPlayerId);
            _relationships.Remove(targetPlayerId);
            callback?.Invoke(true, null);
        }

        public void UnblockPlayer(string targetPlayerId, Action<bool, string> callback)
        {
            _blockedPlayers.Remove(targetPlayerId);
            callback?.Invoke(true, null);
        }

        public void GetBlockedPlayers(Action<bool, List<string>, string> callback)
        {
            callback?.Invoke(true, new List<string>(_blockedPlayers), null);
        }

        public void ReportPlayer(string targetPlayerId, string reason, string description, Action<bool, string> callback)
        {
            callback?.Invoke(true, null);
        }

        public void ShareContent(string message, Action<bool> callback = null)
        {
            callback?.Invoke(true);
        }

        public void GetLeaderboard(string category, Action<bool, List<LeaderboardEntry>, string> callback)
        {
            if (!_isOnline) { callback?.Invoke(false, null, "Offline"); return; }
            var list = new List<LeaderboardEntry>
            {
                new LeaderboardEntry(1, "p_river", "River Bend", 20, 5000),
                new LeaderboardEntry(2, "p_sunny", "Sunny Valley", 15, 3200),
                new LeaderboardEntry(3, "p_green", "Green Farm", 11, 2100)
            };
            callback?.Invoke(true, list, null);
        }
    }
}
