using MXH_ASP.NET_CORE.Models;
using System;
using System.Collections.Generic;

namespace MXH_ASP.NET_CORE.Seeders
{
    public static class FriendshipSeeder
    {
        public static List<Friendship> Seed(List<User> users)
        {
            var friendships = new List<Friendship>();
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                var requester = users[rand.Next(users.Count)];
                var addressee = users[rand.Next(users.Count)];
                if (requester.Id == addressee.Id) continue; // Không kết bạn với chính mình
                friendships.Add(new Friendship
                {
                    RequesterId = requester.Id,
                    AddresseeId = addressee.Id,
                    Status = FriendshipStatus.Accepted,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-rand.Next(1000))
                });
            }
            return friendships;
        }
    }
}
