using System.Diagnostics;

namespace HaslerConnect.modules.Railworks;

public class RailworksModule : GameModule
{
    private RWRailDriverLib? railDriver;
    private int speedControllerID = -1;
    
    public override void Initialize()
    {
        string? rwPath;
        Process? rwProcess = Process.GetProcessesByName("RailWorks").FirstOrDefault() ??
                             Process.GetProcessesByName("RailWorks64").FirstOrDefault() ??
                             Process.GetProcessesByName("RailWorksDX12_64").FirstOrDefault();
        if (rwProcess == null)
        {
            throw new Exception("RailWorks process not found!");
        }

        string exePath = rwProcess.MainModule.FileName;
        rwPath = Path.GetDirectoryName(exePath);

        railDriver = new RWRailDriverLib(rwPath);
        railDriver.SetRailSimConnected(true); // enable API
    }
    
    public override bool ReadyForRead()
    {
        if (railDriver != null)
        {
            return railDriver.GetRailSimConnected() && railDriver.IsLocoSet();
        }
        
        return false;
    }

    private int GetSpeedControllerID()
    {
        if (railDriver == null) return -1;
        if (!railDriver.GetRailSimConnected() || !railDriver.IsLocoSet()) return -1;
        
        string[] controllerList = railDriver.GetControllerList();
        int speedCtrlIndex = Array.IndexOf(controllerList, "SpeedometerKPH");
        return speedCtrlIndex;
    }
    
    public override int GetSpeed()
    {
        if (railDriver == null) return 0;
        if (!railDriver.GetRailSimConnected() || !railDriver.IsLocoSet()) return 0;
        
        if (speedControllerID == -1)
        {
            speedControllerID = GetSpeedControllerID();
            if (speedControllerID == -1) return 0;
        }

        //return (int)Math.Round(Math.Abs(railDriver.GetCurrentControllerValue(speedControllerID)));
        return (int)Math.Floor(Math.Abs(railDriver.GetCurrentControllerValue(speedControllerID)));
    }

    public override void Tick()
    {
        if (railDriver == null) return;

        railDriver.SetRailDriverConnected(true); // prevent RW from disconnecting RD interface

        if (railDriver.GetRailSimLocoChanged())
        {
            speedControllerID = -1;
        }
    }
}