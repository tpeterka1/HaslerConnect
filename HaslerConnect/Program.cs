using System.Diagnostics;
using System.IO.Ports;
using HaslerConnect.modules.Railworks;
using HaslerConnect.modules.TrainDriver2;
using HaslerConnect.modules.TrainSimWorld;

namespace HaslerConnect
{
    internal class Program
    {
        private static INIFile? config;
        private static string COMport = "COM1";
        private static int Baudrate = 9600;
        private static int SendFreqMs = 500;
        public static bool Debug = false;
        private static SerialPort? _serialPort;
        
        private static readonly Dictionary<string[], GameModule> gameProcessModules = new()
        {
            { ["RailWorks", "RailWorks64", "RailWorksDX12_64"], new RailworksModule() },
            { ["TrainSimWorld"], new TSWModule() },
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
            if (Debug) Console.WriteLine($"Current speed: {speed}");

            if (_serialPort == null)
            {
                Console.WriteLine("Serial port is null!");
                return;
            }

            if (!_serialPort.IsOpen) return;
            
            var speedStr = speed.ToString();
            speedStr = speedStr.Equals("") ? "0" : speedStr;
            _serialPort.WriteLine(speedStr);
        }

        private static void CreateConfig()
        {
            config = new INIFile("./config.ini");
            config.IniWriteValue("main", "COMport", "COM1");
            config.IniWriteValue("main", "Baudrate", "9600");
            config.IniWriteValue("main", "SendFreqMs", "500");
            config.IniWriteValue("main", "Debug", "false");
        }

        private static void LoadConfig()
        {
            config = new INIFile("./config.ini");

            COMport = config.IniReadValue("main", "COMport");
            if (COMport.Equals(""))
            {
                Console.WriteLine("Failed to parse COM port from config, specify it in config, or delete it if you want to regenerate it");
                COMport = "COM1";
            }
            var successBaud = int.TryParse(config.IniReadValue("main", "Baudrate"), out Baudrate);
            if (successBaud == false)
            {
                Console.WriteLine("Failed to parse Baudrate from config, specify a numerical value in config, or delete it if you want to regenerate it");
                Baudrate = 9600;
            }
            var successFreq = int.TryParse(config.IniReadValue("main", "SendFreqMs"), out SendFreqMs);
            if (successFreq == false)
            {
                Console.WriteLine("Failed to parse Send frequency from config, specify a numerical value in milliseconds (min. recommended 500) in config, or delete it if you want to regenerate it");
                SendFreqMs = 500;
            }
            var successDebug = bool.TryParse(config.IniReadValue("main", "Debug"), out Debug);
            if (successDebug == false) Debug = false;
        }
        
        static void Main(string[] args)
        {
            // Load config/create it if it doesn't exist
            if (!File.Exists("./config.ini"))
            {
                CreateConfig();
                Console.WriteLine("Created config, exiting...");
                Environment.Exit(0);
            }
            LoadConfig();
            Console.Write($"Config loaded with the following values:\n\tCOM port: {COMport}\n\tBaudrate: {Baudrate}\n\tSending frequency: {SendFreqMs}\n\tDebug: {Debug}\n\n");
            
            // Initialize serial interface
            _serialPort = new SerialPort(COMport, Baudrate);
            try
            {
                _serialPort.Open();
                if (Debug) Console.WriteLine("Serial port initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open serial port ({COMport}), exiting...");
                throw ex;
            }
            
            string chosenProcess = string.Empty;
            GameModule? activeModule = null;
            
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
                        Console.WriteLine($"Chosen game: {chosenProcess}");
                    }
                    Thread.Sleep(1000);
                }
                else
                {
                    if (Process.GetProcessesByName(chosenProcess).Length == 0)
                    {
                        Console.WriteLine($"Game closed: {chosenProcess}");
                        chosenProcess = string.Empty;
                        activeModule = null;
                        Console.WriteLine("Waiting for game...");
                    }
                    else
                    {
                        if (activeModule == null) continue;
                        activeModule.Tick();

                        if (activeModule.ReadyForRead())
                        {
                            int speed = activeModule.GetSpeed();
                            SendSpeedToCOM(speed);
                        }
                        
                        Thread.Sleep(SendFreqMs); // Reduce CPU usage
                    }
                }
            }
        }
    }
}
