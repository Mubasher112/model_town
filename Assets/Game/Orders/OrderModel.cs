using System;
using System.Collections.Generic;
using Game.Data;

namespace Game.Orders
{
    public enum OrderState
    {
        Active,
        Completed,
        Expired
    }

    [Serializable]
    public class OrderInstance
    {
        public string OrderId;
        public string CustomerId;
        public OrderType Type = OrderType.Customer;
        public List<OrderRequirement> Requirements = new List<OrderRequirement>();
        public OrderReward Reward = new OrderReward();
        public OrderState State = OrderState.Active;
        public long CreationUtcTicks;
        public long ExpirationUtcTicks = 0;

        public OrderInstance() { }

        public OrderInstance(
            string orderId,
            string customerId,
            OrderType type,
            List<OrderRequirement> reqs,
            OrderReward reward,
            long creationUtcTicks,
            long expirationDurationSeconds = 0)
        {
            OrderId = orderId;
            CustomerId = customerId;
            Type = type;
            Requirements = reqs ?? new List<OrderRequirement>();
            Reward = reward ?? new OrderReward();
            State = OrderState.Active;
            CreationUtcTicks = creationUtcTicks;

            if (expirationDurationSeconds > 0)
            {
                ExpirationUtcTicks = creationUtcTicks + TimeSpan.FromSeconds(expirationDurationSeconds).Ticks;
            }
            else
            {
                ExpirationUtcTicks = 0;
            }
        }

        public bool IsExpired(long currentUtcTicks)
        {
            if (State == OrderState.Completed) return false;
            if (ExpirationUtcTicks > 0 && currentUtcTicks >= ExpirationUtcTicks)
            {
                return true;
            }
            return false;
        }

        public double GetRemainingSeconds(long currentUtcTicks)
        {
            if (ExpirationUtcTicks <= 0 || currentUtcTicks >= ExpirationUtcTicks) return 0.0;
            return TimeSpan.FromTicks(ExpirationUtcTicks - currentUtcTicks).TotalSeconds;
        }
    }

    [Serializable]
    public class OrderHistoryEntry
    {
        public string OrderId;
        public string CustomerId;
        public long CoinsEarned;
        public int XpEarned;
        public long CompletionUtcTicks;

        public OrderHistoryEntry() { }

        public OrderHistoryEntry(string orderId, string customerId, long coinsEarned, int xpEarned, long completionUtcTicks)
        {
            OrderId = orderId;
            CustomerId = customerId;
            CoinsEarned = coinsEarned;
            XpEarned = xpEarned;
            CompletionUtcTicks = completionUtcTicks;
        }
    }
}
