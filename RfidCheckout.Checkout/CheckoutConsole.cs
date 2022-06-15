using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json;
using RfidCheckout.Config;
using RfidCheckout.Messages;

namespace RfidCheckout.Checkout
{
    class CheckoutConsole
    {

        static int ReceivedCount = 0;
        static double BillTotal = 0.0;

        static bool UseMessageSessions = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Checkout Console");

            
            // Create a new management client
            var managementClient = new ManagementClient(AccountDetails.ConnectionString);

            // Delete the queue if it exists.
            if (await managementClient.QueueExistsAsync(AccountDetails.QueueName))
            {
                await managementClient.DeleteQueueAsync(AccountDetails.QueueName);
            }

            // Create a description for the queue.
            QueueDescription rfidCheckoutQueueDescription =
                new(AccountDetails.QueueName)
                {
                    // Comment in to require duplicate detection
                    //RequiresDuplicateDetection = true,
                    //DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(10),

                    // Comment in to require sessions
                    //RequiresSession = true
                };

            // Create a queue based on the queue description.
            await managementClient.CreateQueueAsync(rfidCheckoutQueueDescription);

            if (!UseMessageSessions)
            {
                
                // Create a queue client an register a message handler
                QueueClient queueClient = new(AccountDetails.ConnectionString, AccountDetails.QueueName);
                queueClient.RegisterMessageHandler(HandleMessage, new MessageHandlerOptions(HandleMessageExceptions));

                Console.WriteLine("Receiving tag read messages...");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.ReadLine();
                await queueClient.CloseAsync();

                // Bill the customer.
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine
                    ("Bill customer ${0} for {1} items.", BillTotal, ReceivedCount);

                Console.ReadLine();

            }
            else
            {

                // Create a session client
                var sessionClient = new SessionClient
                    (AccountDetails.ConnectionString, AccountDetails.QueueName);

                while (true)
                {
                    Console.WriteLine("Accepting a message session...");
                    Console.ForegroundColor = ConsoleColor.White;

                    // Accept a message session                    
                    var messageSession = await sessionClient.AcceptMessageSessionAsync();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Accepted session: { messageSession.SessionId }");
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    int receivedCount = 0;
                    double billTotal = 0.0;

                    while (true)
                    {
                        // Receive a message
                        var message = await messageSession.ReceiveAsync(TimeSpan.FromSeconds(1));

                        
                        if (message == null)
                        {
                            Console.WriteLine("Closing session...!");
                            await messageSession.CloseAsync();
                            break;
                        }
                        else
                        {
                            // Process the order message
                            var rfidJson = Encoding.UTF8.GetString(message.Body);
                            var rfidTag = JsonConvert.DeserializeObject<RfidTag>(rfidJson);
                            Console.WriteLine($"{ rfidTag.ToString() }");

                            receivedCount++;
                            billTotal += rfidTag.Price;

                            // Complete the message receive opperation
                            await messageSession.CompleteAsync(message.SystemProperties.LockToken);
                        }
                    }
                    // Bill the customer.
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine
                        ("Bill customer ${0} for {1} items.", billTotal, receivedCount);                   
                }



            }



        }


        private static async Task HandleMessage(Message message, CancellationToken cancellationToken)
        {
            // Process the order message
            var rfidJson = Encoding.UTF8.GetString(message.Body);
            var rfidTag = JsonConvert.DeserializeObject<RfidTag>(rfidJson);

            Console.WriteLine($"{ rfidTag.ToString() }");

            ReceivedCount++;
            BillTotal += rfidTag.Price;

        }

        private static async Task HandleMessageExceptions(ExceptionReceivedEventArgs arg)
        {
            throw new NotImplementedException();
        }


        //private static async Task HandleSession(IMessageSession session, Message message, CancellationToken cancellationToken)
        //{
        //    //throw new NotImplementedException();

        //    Console.WriteLine($"HandleSession: { session.SessionId }");
        //    await session.CompleteAsync(message.SystemProperties.LockToken);
        //    //await session.CloseAsync();

        //    //session.RegisterMessageHandler(HandleMessage, new MessageHandlerOptions(HandleMessageExceptions)
        //    //{
        //    //    MaxConcurrentCalls = 1,
        //    //    AutoComplete = true,
        //    //});


        //}




        //private static Task HandleSessionException(ExceptionReceivedEventArgs arg)
        //{
        //    throw new NotImplementedException();
        //}






    }
}
