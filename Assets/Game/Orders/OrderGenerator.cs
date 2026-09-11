using System;
using System.Collections.Generic;
using Game.Data;

namespace Game.Orders
{
    public class OrderGenerator
    {
        public static OrderInstance GenerateOrderForLevel(int playerLevel, long currentUtcTicks, long expirationDurationSeconds = 0)
        {
            var templates = OrderTemplateLibrary.GetTemplatesForLevel(playerLevel);
            if (templates == null || templates.Count == 0)
            {
                templates = OrderTemplateLibrary.GetTemplatesForLevel(1);
            }

            var rand = new Random();
            var selectedTemplate = templates[rand.Next(templates.Count)];
            var customer = CustomerLibrary.GetRandomCustomer();

            string orderId = "order_" + Guid.NewGuid().ToString().Substring(0, 6);

            var reqs = new List<OrderRequirement>();
            foreach (var r in selectedTemplate.Requirements)
            {
                reqs.Add(new OrderRequirement(r.ItemId, r.Quantity));
            }

            var reward = new OrderReward(selectedTemplate.Reward.Coins, selectedTemplate.Reward.Xp);

            return new OrderInstance(
                orderId,
                customer.CustomerId,
                selectedTemplate.Type,
                reqs,
                reward,
                currentUtcTicks,
                expirationDurationSeconds
            );
        }
    }
}
