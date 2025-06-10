using System.Diagnostics;
using HaslerConnect.modules.Railworks;
using HaslerConnect.TrainDriver2;

namespace HaslerConnect
{
    internal class Program
    {
        private static readonly Dictionary<string[], GameModule> gameProcessModules = new()
        {
            { ["RailWorks", "RailWorks64", "RailWorksDX12_64"], new RailworksModule() },
            //{ ["TrainSimWorld"], new TSWModule() },
            { ["TrainDriver2"], new TD2Module() },
            //{ ["SimRail"], new SimRailModule() }
        };
        
        private static (string processName, GameModule module)? FindRunningGame()
        {
            foreach (var entry in gameProcessModules)
            {
                var processName = entry.Key.FirstOrDefault(name =>
                    Process.GetProcessesByName(name).Length > 0);
                if (processName != null)
                    return (processName, entry.Value);
            }
            return null;
        }

        private static void SendSpeedToCOM(int speed)
        {
            // TODO: Implement way to send speed to arduino/pi
            Console.WriteLine($"Current speed: {speed}");
        }
        
        static void Main(string[] args)
        {
            //string[] processList = Array.Empty<string>();
            string chosenProcess = string.Empty;
            GameModule? activeModule = null;
            
            //foreach (var item in gameProcessModules)
            //{
            //    processList = processList.Concat(item.Key).ToArray();
            //}

            //Console.WriteLine("Waiting for game...");

            //while (string.IsNullOrEmpty(chosenProcess))
            //{
            //    foreach (string process in processList)
            //    {
            //        if (Process.GetProcessesByName(process).FirstOrDefault() != null)
            //        {
            //            chosenProcess = process;
            //            break;
            //        }
            //    }
            //}
            //
            //Console.WriteLine("Chosen process: " + chosenProcess);
            
            Console.WriteLine("Waiting for game...");

            while (true)
            {
                if (string.IsNullOrEmpty(chosenProcess))
                {
                    var found = FindRunningGame();
                    if (found.HasValue)
                    {
                        chosenProcess = found.Value.processName;
                        activeModule = found.Value.module;
                        activeModule.Initialize();
                        Console.WriteLine($"Chosen process: {chosenProcess}");
                    }
                    Thread.Sleep(1000);
                }
                else
                {
                    if (Process.GetProcessesByName(chosenProcess).Length == 0)
                    {
                        Console.WriteLine($"Process closed: {chosenProcess}");
                        chosenProcess = string.Empty;
                        activeModule = null;
                        Console.WriteLine("Waiting for game...");
                    }
                    else
                    {
                        activeModule.Tick();

                        if (activeModule.ReadyForRead())
                        {
                            int speed = activeModule.GetSpeed();
                            SendSpeedToCOM(speed);
                        }
                        
                        Thread.Sleep(500); // Reduce CPU usage
                    }
                }
            }
            
            
        }
    }
}
