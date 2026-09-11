using System;
using System.Collections.Generic;
using Game.Save;

namespace Game.Data
{
    public enum FriendStatus
    {
        None,
        PendingIncoming,
        PendingOutgoing,
        Accepted,
        Blocked,
        Removed
    }

    public enum OnlinePresence
    {
        Offline,
        Online,
        RecentlyActive,
        Unknown
    }

    [Serializable]
    public class AvatarDefinition
    {
        public string AvatarId;
        public string Name;
        public string IconPath;

        public AvatarDefinition() { }

        public AvatarDefinition(string avatarId, string name, string iconPath)
        {
            AvatarId = avatarId;
            Name = name;
            IconPath = iconPath;
        }
    }

    public static class AvatarLibrary
    {
        private static readonly Dictionary<string, AvatarDefinition> _avatars = new Dictionary<string, AvatarDefinition>
        {
            { "avatar_default", new AvatarDefinition("avatar_default", "Mayor", "Avatars/mayor_default") },
            { "avatar_farmer", new AvatarDefinition("avatar_farmer", "Farmer Alex", "Avatars/farmer") },
            { "avatar_baker", new AvatarDefinition("avatar_baker", "Baker Sam", "Avatars/baker") },
            { "avatar_explorer", new AvatarDefinition("avatar_explorer", "Explorer Maya", "Avatars/explorer") }
        };

        public static AvatarDefinition GetAvatar(string avatarId)
        {
            return _avatars.TryGetValue(avatarId, out var def) ? def : _avatars["avatar_default"];
        }

        public static List<AvatarDefinition> GetAllAvatars()
        {
            return new List<AvatarDefinition>(_avatars.Values);
        }
    }

    [Serializable]
    public class SocialProfile
    {
        public string PlayerId;
        public string DisplayName = "Mayor's Valley";
        public string AvatarId = "avatar_default";
        public int Level = 1;
        public int Population = 0;
        public int Happiness = 100;
        public long JoinUtcTicks;
        public long LastActiveUtcTicks;
        public OnlinePresence Presence = OnlinePresence.Online;
        public int FriendsCount = 0;
        public int AppreciationsCount = 0;

        public SocialProfile() { }

        public SocialProfile(string playerId, string displayName, int level, int population, int happiness, string avatarId = "avatar_default")
        {
            PlayerId = playerId;
            DisplayName = displayName;
            Level = level;
            Population = population;
            Happiness = happiness;
            AvatarId = avatarId;
            JoinUtcTicks = DateTime.UtcNow.Ticks;
            LastActiveUtcTicks = DateTime.UtcNow.Ticks;
        }
    }

    [Serializable]
    public class FriendRelationship
    {
        public string TargetPlayerId;
        public SocialProfile TargetProfile;
        public FriendStatus Status = FriendStatus.None;
        public long RequestSentUtcTicks;

        public FriendRelationship() { }

        public FriendRelationship(string targetPlayerId, SocialProfile targetProfile, FriendStatus status)
        {
            TargetPlayerId = targetPlayerId;
            TargetProfile = targetProfile;
            Status = status;
            RequestSentUtcTicks = DateTime.UtcNow.Ticks;
        }
    }

    [Serializable]
    public class TownSnapshot
    {
        public int SnapshotVersion = 1;
        public string PlayerId;
        public string DisplayName;
        public int TownLevel;
        public int Population;
        public int Happiness;
        public long CreatedUtcTicks;

        public List<SavedPlacedObject> PlacedObjects = new List<SavedPlacedObject>();
        public List<SavedRoadTile> RoadTiles = new List<SavedRoadTile>();
        public List<SavedBuilding> Buildings = new List<SavedBuilding>();
        public List<SavedField> Fields = new List<SavedField>();

        public TownSnapshot() { }

        public TownSnapshot(string playerId, string displayName, int level, int population, int happiness)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            TownLevel = level;
            Population = population;
            Happiness = happiness;
            CreatedUtcTicks = DateTime.UtcNow.Ticks;
        }
    }

    [Serializable]
    public class SocialGift
    {
        public string GiftId;
        public string SenderPlayerId;
        public string SenderName;
        public string ItemId;
        public int Quantity;
        public long SentUtcTicks;

        public SocialGift() { }

        public SocialGift(string giftId, string senderPlayerId, string senderName, string itemId, int quantity)
        {
            GiftId = giftId;
            SenderPlayerId = senderPlayerId;
            SenderName = senderName;
            ItemId = itemId;
            Quantity = quantity;
            SentUtcTicks = DateTime.UtcNow.Ticks;
        }
    }

    [Serializable]
    public class HelpRequest
    {
        public string RequestId;
        public string RequesterPlayerId;
        public string RequesterName;
        public string NeededItemId;
        public int Quantity;
        public bool IsFulfilled;

        public HelpRequest() { }

        public HelpRequest(string requestId, string requesterPlayerId, string requesterName, string neededItemId, int quantity)
        {
            RequestId = requestId;
            RequesterPlayerId = requesterPlayerId;
            RequesterName = requesterName;
            NeededItemId = neededItemId;
            Quantity = quantity;
            IsFulfilled = false;
        }
    }

    [Serializable]
    public class ReportData
    {
        public string ReportId;
        public string ReporterPlayerId;
        public string TargetPlayerId;
        public string Reason;
        public string Description;
        public long UtcTicks;

        public ReportData() { }

        public ReportData(string reportId, string reporterPlayerId, string targetPlayerId, string reason, string description)
        {
            ReportId = reportId;
            ReporterPlayerId = reporterPlayerId;
            TargetPlayerId = targetPlayerId;
            Reason = reason;
            Description = description;
            UtcTicks = DateTime.UtcNow.Ticks;
        }
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int Rank;
        public string PlayerId;
        public string DisplayName;
        public int Level;
        public long Score;

        public LeaderboardEntry() { }

        public LeaderboardEntry(int rank, string playerId, string displayName, int level, long score)
        {
            Rank = rank;
            PlayerId = playerId;
            DisplayName = displayName;
            Level = level;
            Score = score;
        }
    }
}
