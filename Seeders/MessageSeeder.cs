using MXH_ASP.NET_CORE.Models;
using System;
using System.Collections.Generic;

namespace MXH_ASP.NET_CORE.Seeders
{
    public static class MessageSeeder
    {
        public static List<Message> Seed(List<User> users)
        {
            var messages = new List<Message>();
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                var sender = users[rand.Next(users.Count)];
                var receiver = users[rand.Next(users.Count)];
                if (sender.Id == receiver.Id) continue; // Không nhắn tin cho chính mình
                messages.Add(new Message
                {
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                    Content = $"Tin nhắn số {i + 1} từ {sender.FullName} đến {receiver.FullName}",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-rand.Next(1000))
                });
            }
            return messages;
        }
    }
}
