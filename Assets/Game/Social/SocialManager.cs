using System;
using System.Collections.Generic;
using Game.Data;
using Game.Services;

namespace Game.Social
{
    public enum GameTownMode
    {
        OwnTown,
        FriendVisit,
        Adventure
    }

    public class SocialManager
    {
        private readonly ISocialService _socialService;
        private readonly SocialProfile _myProfile;

        private GameTownMode _currentTownMode = GameTownMode.OwnTown;
        private TownSnapshot _visitedTownSnapshot;

        private readonly List<FriendRelationship> _cachedFriends = new List<FriendRelationship>();
        private readonly HashSet<string> _claimedGiftIds = new HashSet<string>();
        private readonly HashSet<string> _appreciatedPlayerIds = new HashSet<string>();

        public GameTownMode CurrentTownMode => _currentTownMode;
        public bool IsVisitingFriend => _currentTownMode == GameTownMode.FriendVisit;
        public TownSnapshot VisitedTownSnapshot => _visitedTownSnapshot;
        public SocialProfile MyProfile => _myProfile;

        public SocialManager(ISocialService socialService, SocialProfile myProfile = null)
        {
            _socialService = socialService ?? new MockSocialService();
            _myProfile = myProfile ?? new SocialProfile("p_local_me", "Mayor's Valley", 1, 2, 100);
        }

        public void RefreshProfile(Action<bool, string> onComplete = null)
        {
            _socialService.GetMyProfile((success, profile, err) =>
            {
                if (success && profile != null)
                {
                    _myProfile.DisplayName = profile.DisplayName;
                    _myProfile.AvatarId = profile.AvatarId;
                    _myProfile.Level = profile.Level;
                    _myProfile.Population = profile.Population;
                    _myProfile.Happiness = profile.Happiness;
                    _myProfile.FriendsCount = profile.FriendsCount;
                    _myProfile.AppreciationsCount = profile.AppreciationsCount;
                }
                onComplete?.Invoke(success, err);
            });
        }

        public void UpdateMyProfile(string newDisplayName, string newAvatarId, Action<bool, string> onComplete)
        {
            _socialService.UpdateProfile(newDisplayName, newAvatarId, (success, err) =>
            {
                if (success)
                {
                    _myProfile.DisplayName = newDisplayName.Trim();
                    _myProfile.AvatarId = newAvatarId;
                }
                onComplete?.Invoke(success, err);
            });
        }

        public void RefreshFriendsList(Action<bool, List<FriendRelationship>, string> onComplete)
        {
            _socialService.GetFriendsList((success, list, err) =>
            {
                if (success && list != null)
                {
                    _cachedFriends.Clear();
                    _cachedFriends.AddRange(list);
                }
                onComplete?.Invoke(success, list, err);
            });
        }

        public void StartVisitingFriend(string targetPlayerId, Action<bool, TownSnapshot, string> onComplete)
        {
            _socialService.VisitTown(targetPlayerId, (success, snapshot, err) =>
            {
                if (success && snapshot != null)
                {
                    _visitedTownSnapshot = snapshot;
                    _currentTownMode = GameTownMode.FriendVisit;
                }
                onComplete?.Invoke(success, snapshot, err);
            });
        }

        public void ReturnToOwnTown()
        {
            _visitedTownSnapshot = null;
            _currentTownMode = GameTownMode.OwnTown;
        }

        public bool CanAppreciateTown(string targetPlayerId)
        {
            if (targetPlayerId == _myProfile.PlayerId) return false;
            return !_appreciatedPlayerIds.Contains(targetPlayerId);
        }

        public void AppreciateTown(string targetPlayerId, Action<bool, string> onComplete)
        {
            if (!CanAppreciateTown(targetPlayerId))
            {
                onComplete?.Invoke(false, "Cannot appreciate this town");
                return;
            }

            _socialService.AppreciateTown(targetPlayerId, (success, err) =>
            {
                if (success)
                {
                    _appreciatedPlayerIds.Add(targetPlayerId);
                }
                onComplete?.Invoke(success, err);
            });
        }

        public void LoadClaimedGifts(IEnumerable<string> giftIds)
        {
            if (giftIds == null) return;
            foreach (var id in giftIds) _claimedGiftIds.Add(id);
        }

        public void LoadAppreciatedPlayers(IEnumerable<string> playerIds)
        {
            if (playerIds == null) return;
            foreach (var id in playerIds) _appreciatedPlayerIds.Add(id);
        }

        public List<string> ExportClaimedGifts() => new List<string>(_claimedGiftIds);
        public List<string> ExportAppreciatedPlayers() => new List<string>(_appreciatedPlayerIds);
        public List<FriendRelationship> GetCachedFriends() => new List<FriendRelationship>(_cachedFriends);
    }
}
