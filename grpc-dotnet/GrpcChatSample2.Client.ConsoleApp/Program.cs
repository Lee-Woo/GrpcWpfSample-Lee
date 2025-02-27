﻿using Google.Protobuf.WellKnownTypes;
using GrpcChatSample.Common;

namespace GrpcChatSample2.Client.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter your name> ");
            var name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "anonymous";
            }

            Console.WriteLine($"Joined as {name}");
            Console.WriteLine("Press 'Esc' to exit.");

            using var chatServiceClient = new ChatServiceClient();
            var consoleLock = new object();

            // subscribe (asynchronous)
            _ = chatServiceClient.ChatLogs()
                .ForEachAsync((x) =>
                {
                    // if the user is writing something, wait until it finishes.
                    lock (consoleLock)
                    {
                        Console.WriteLine($"{x.At.ToDateTime().ToString("HH:mm:ss")} {x.FromName}: {x.Content}");
                    }
                });

            // write
            while (true)
            {
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.Escape) { break; } // exit

                // A key input starts writing mode
                lock (consoleLock)
                {
                    var content = key.KeyChar + Console.ReadLine();

                    chatServiceClient.Write(new ChatLog
                    {
                        FromName = name,
                        Content = content,
                        At = Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
                    }).Wait();
                }
            }
        }
    }
}