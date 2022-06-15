using System;
using RfidCheckout.Config;
using System.Threading;
using Microsoft.Azure.ServiceBus;
using RfidCheckout.Messages;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static System.Console;

namespace RfidCheckout.TagReader
{
    class TagReaderConsole
    {
        static async Task Main(string[] args)
        {
            WriteLine("Tag Reader Console");

            QueueClient queueClient = new(AccountDetails.ConnectionString, AccountDetails.QueueName);
            
            // Create a sample order
            RfidTag[] orderItems = {
                    new() { Product = "Ball", Price = 4.99 },
                    new() { Product = "Whistle", Price = 1.95 },
                    new() { Product = "Bat", Price = 12.99 },
                    new() { Product = "Bat", Price = 12.99 },
                    new() { Product = "Gloves", Price = 7.99 },
                    new() { Product = "Gloves", Price = 7.99 },
                    new() { Product = "Cap", Price = 9.99 },
                    new() { Product = "Cap", Price = 9.99 },
                    new() { Product = "Shirt", Price = 14.99 },
                    new() { Product = "Shirt", Price = 14.99 },
            };

            // Display the order data.
            ForegroundColor = ConsoleColor.Green;
            WriteLine("Order contains {0} items.", orderItems.Length);
            ForegroundColor = ConsoleColor.Yellow;
            
            double orderTotal = 0.0;
            foreach (RfidTag tag in orderItems)
            {
                WriteLine("{0} - ${1}", tag.Product, tag.Price);
                orderTotal += tag.Price;
            }
            ForegroundColor = ConsoleColor.Green;
            WriteLine("Order value = ${0}.", orderTotal);
            WriteLine();
            ResetColor();
            WriteLine("Press enter to scan...");
            ReadLine();

            Random random = new(DateTime.Now.Millisecond);


            // Send the order with random duplicate tag reads
            int sentCount = 0;
            int position = 0;

            WriteLine("Reading tags...");
            WriteLine();
            ForegroundColor = ConsoleColor.Cyan;

            // Comment in to create session id
            //var sessionId = Guid.NewGuid().ToString();
            //Console.WriteLine($"SessionId: { sessionId }");

            while (position < 10)
            {
                RfidTag rfidTag = orderItems[position];

                // Create a new  message from the order item RFID tag.
                var orderJson = JsonConvert.SerializeObject(rfidTag);
                var tagReadMessage = new Message(Encoding.UTF8.GetBytes(orderJson));

                // Comment in to set message id.
                //tagReadMessage.MessageId = rfidTag.TagId;

                // Comment in to set session id.
                //tagReadMessage.SessionId = sessionId;

                // Send the message
                await queueClient.SendAsync(tagReadMessage);
                WriteLine($"Sent: { orderItems[position].Product }");
                //Console.WriteLine($"Sent: { orderItems[position].Product } - MessageId: { tagReadMessage.MessageId }");

                // Randomly cause a duplicate message to be sent.
                if (random.NextDouble() > 0.4) position++;
                sentCount++;
                Thread.Sleep(100);
            }

            ForegroundColor = ConsoleColor.Green;
            WriteLine("{0} total tag reads.", sentCount);
            WriteLine();
            ResetColor(); 
            ReadLine();
        }
    }
}
