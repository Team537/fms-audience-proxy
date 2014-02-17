using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Team537.Audience
{
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.ServiceModel;
    using System.Threading;

    using FMS.Contract;
    using FMS.Contract.Service;
    using FMS.Infrastructure.Services;
    using FMS.Match.Module.Services;

    using Microsoft.Practices.Composite.Logging;

    class Program
    {
        static void Main(string[] args)
        {
            var logger = new TraceLogger();


            var serviceHost = new AudienceHost(logger);
            serviceHost.Inititalize();
            serviceHost.Start("net.tcp://localhost:8003/PublishingService", "net.tcp://localhost:8004/SubscriptionService");

            var commandHandler = new CommandHandler();

            var cancellationToken = new CancellationTokenSource();

            Task.Factory.StartNew(
                () =>
                    {
                        var fmsEventListener = new TcpListener(IPAddress.Any, 8005);
                        fmsEventListener.Start();
                        while (!cancellationToken.Token.IsCancellationRequested)
                        {
                            Console.WriteLine("Waiting for connection");
                            var client = fmsEventListener.AcceptTcpClient();
                            Task.Factory.StartNew(() => ThreadProc(client, commandHandler), cancellationToken.Token);
                        }
                    }, 
                    cancellationToken.Token);

            Console.WriteLine("Waiting for command");

            var quit = false;
            while (!quit)
            {
                var input = Console.ReadLine();
                switch (input)
                {
                    case "q":
                        cancellationToken.Cancel();
                        quit = true;
                        break;
                    default:
                        lock (CommandHandler.LockObject)
                        {
                            commandHandler.SendCommand(input);
                        }

                        break;
                }

                Console.WriteLine("Waiting for command");
            }
            
            serviceHost.Stop();
        }

        private static void ThreadProc(TcpClient client, CommandHandler commandHandler)
        {
            // Do your work here
            Console.Write("Client connected");

            // Receive until client closes connection, indicated by 0 return value
            var streamReader = new StreamReader(client.GetStream());
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                Console.WriteLine("Received: {0}", line);

                lock (CommandHandler.LockObject)
                {
                    commandHandler.SendCommand(line);
                }
            }
        }
    }
}