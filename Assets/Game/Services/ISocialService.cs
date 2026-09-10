using System;
using System.Collections.Generic;
using Game.Data;

namespace Game.Services
{
    public interface ISocialService
    {
        bool IsOnline { get; }
        void SetOnline(bool isOnline);

        void GetMyProfile(Action<bool, SocialProfile, string> callback);
        void UpdateProfile(string displayName, string avatarId, Action<bool, string> callback);

        void SearchPlayers(string query, Action<bool, List<SocialProfile>, string> callback);
        void GetPlayerProfile(string targetPlayerId, Action<bool, SocialProfile, string> callback);

        void GetFriendsList(Action<bool, List<FriendRelationship>, string> callback);
        void SendFriendRequest(string targetPlayerId, Action<bool, string> callback);
        void AcceptFriendRequest(string targetPlayerId, Action<bool, string> callback);
        void RejectFriendRequest(string targetPlayerId, Action<bool, string> callback);
        void RemoveFriend(string targetPlayerId, Action<bool, string> callback);

        void VisitTown(string targetPlayerId, Action<bool, TownSnapshot, string> callback);
        void AppreciateTown(string targetPlayerId, Action<bool, string> callback);

        void SendGift(string targetPlayerId, string itemId, int quantity, Action<bool, string> callback);
        void GetPendingGifts(Action<bool, List<SocialGift>, string> callback);
        void ClaimGift(string giftId, Action<bool, string> callback);

        void BlockPlayer(string targetPlayerId, Action<bool, string> callback);
        void UnblockPlayer(string targetPlayerId, Action<bool, string> callback);
        void GetBlockedPlayers(Action<bool, List<string>, string> callback);

        void ReportPlayer(string targetPlayerId, string reason, string description, Action<bool, string> callback);
        void ShareContent(string message, Action<bool> callback = null);
    }

    public interface ILeaderboardService
    {
        void GetLeaderboard(string category, Action<bool, List<LeaderboardEntry>, string> callback);
    }
}
