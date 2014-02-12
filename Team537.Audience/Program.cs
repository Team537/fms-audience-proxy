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
            
            var fmsEventListener = new TcpListener(IPAddress.Any, 8005);
            fmsEventListener.Start();

            Console.WriteLine("Waiting for connection");
            while (Console.ReadLine() != "q")
            {
                var client = fmsEventListener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(ThreadProc, client);
            }
            
            serviceHost.Stop();
        }

        private static void ThreadProc(object obj)
        {
            var client = (TcpClient)obj;
            var logger = new TraceLogger();
            var publishService = new FMSPublishService(logger);

            // Do your work here
            Console.Write("Client connected");

            var eventInfo = new FIRSTEventInfo();

            var matchInfo = new MatchInfo();
            matchInfo.Auto = true;
            matchInfo.AutoStartTime = 20; // Settings.Default.AutoTime;
            matchInfo.ManualStartTime = 25; // Settings.Default.ManualTime;
            matchInfo.TimeLeft = 90;
            matchInfo.MatchNumber = 999;
            matchInfo.BlueAllianceStation1.TeamId = 1;
            matchInfo.BlueAllianceStation2.TeamId = 2;
            matchInfo.BlueAllianceStation3.TeamId = 3;
            matchInfo.RedAllianceStation1.TeamId = 4;
            matchInfo.RedAllianceStation2.TeamId = 5;
            matchInfo.RedAllianceStation3.TeamId = 6;
            matchInfo.Score.IgnoreControllerCounts = true;
            matchInfo.ResetMatch();
            matchInfo.SetReady();
 
            // Receive until client closes connection, indicated by 0 return value
            var streamReader = new StreamReader(client.GetStream());
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                switch (line)
                {
                    case "auto-start":
                        publishService.MatchStartAuto(eventInfo.EventId, matchInfo);
                        break;

                    case "auto-end":
                        publishService.MatchEndAuto(eventInfo.EventId, matchInfo);
                        break;

                    case "tele-start":
                        publishService.MatchStartTeleop(eventInfo.EventId, matchInfo);
                        break;

                    case "tele-end":
                        publishService.MatchEndTeleop(eventInfo.EventId, matchInfo);
                        break;

                    case "warning1":
                        publishService.MatchTimerWarning1(eventInfo.EventId, matchInfo);
                        break;

                    case "warning2":
                        publishService.MatchTimerWarning2(eventInfo.EventId, matchInfo);
                        break;

                    default:
                        if (line.StartsWith("time-change"))
                        {
                            
                        }
                        else if (line.StartsWith("match-change"))
                        {
                            
                        }

                        break;
                }
            }
        }
    }
}