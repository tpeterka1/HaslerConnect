using System.Runtime.InteropServices;

namespace HaslerConnect.modules.Railworks;

public class RWRailDriverLib
{
    private string dllPath = @"E:\Hry\Steam\steamapps\common\RailWorks\plugins\RailDriver64.dll"; // fallback
    private IntPtr railDriver;
    public RWRailDriverLib(string? programPath)
    {
        if (!string.IsNullOrEmpty(programPath))
        {
            dllPath = Path.Combine(programPath,
                Environment.Is64BitOperatingSystem ? "plugins\\RailDriver64.dll" : "plugins\\RailDriver.dll");
        }
        
        if (!File.Exists(dllPath)) {
            throw new FileNotFoundException("DLL not found: " + dllPath);
        }

        railDriver = NativeLibrary.Load(dllPath);
    }
    
    public T GetFunction<T>(string functionName) where T : Delegate
    {
        if (railDriver == IntPtr.Zero)
        {
            throw new Exception("RailDriver not loaded");
        }
        IntPtr funcPtr = NativeLibrary.GetExport(railDriver, functionName);
        return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
    }
    
    // Clean
    ~RWRailDriverLib()
    {
        if (railDriver != IntPtr.Zero)
            NativeLibrary.Free(railDriver);
    }
    
    // Functions
    public float GetCurrentControllerValue(int controllerID)
    {
        if (Environment.Is64BitOperatingSystem)
        {
            return GetFunction<GetCurrentControllerValue64>("GetCurrentControllerValue")(controllerID);
        }
        else
        {
            return GetFunction<GetCurrentControllerValue86>("GetCurrentControllerValue")(controllerID);
        }
    }

    public bool GetRailSimLocoChanged()
    {
        if (Environment.Is64BitOperatingSystem)
        {
            return GetFunction<GetRailSimLocoChanged64>("GetRailSimLocoChanged")();
        }
        else
        {
            return GetFunction<GetRailSimLocoChanged86>("GetRailSimLocoChanged")();
        }
    }
    
    public string GetLocoName()
    {
        if (Environment.Is64BitOperatingSystem)
        {
            IntPtr locoNamePtr = GetFunction<GetLocoName64>("GetLocoName")();
            return Marshal.PtrToStringAnsi(locoNamePtr) ?? string.Empty;
        }
        else
        {
            IntPtr locoNamePtr = GetFunction<GetLocoName86>("GetLocoName")();
            return Marshal.PtrToStringAnsi(locoNamePtr) ?? string.Empty;
        }
    }
    
    public string[] GetControllerList()
    {
        IntPtr controllerListPtr = GetFunction<GetControllerList>("GetControllerList")();
        String controllerList = Marshal.PtrToStringAnsi(controllerListPtr);
        return controllerList?.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
    }
    
    public bool GetRailSimConnected()
    {
        if (Environment.Is64BitOperatingSystem)
        {
            return GetFunction<GetRailSimConnected64>("GetRailSimConnected")();
        }
        else
        {
            return GetFunction<GetRailSimConnected86>("GetRailSimConnected")();
        }
    }
    
    public bool IsLocoSet()
    {
        if (Environment.Is64BitOperatingSystem)
        {
            return GetFunction<IsLocoSet64>("IsLocoSet")();
        }
        else
        {
            return GetFunction<IsLocoSet86>("IsLocoSet")();
        }
    }
    
    public void SetRailDriverConnected(bool connected)
    {
        if (Environment.Is64BitOperatingSystem)
        {
            GetFunction<SetRailDriverConnected64>("SetRailDriverConnected")(connected);
        }
        else
        {
            GetFunction<SetRailDriverConnected86>("SetRailDriverConnected")(connected);
        }
    }

    public void SetRailSimConnected(bool connected)
    {
        if (Environment.Is64BitOperatingSystem)
        {
            GetFunction<SetRailSimConnected64>("SetRailSimConnected")(connected);
        }
        else
        {
            GetFunction<SetRailSimConnected86>("SetRailSimConnected")(connected);
        }
    }
}

// x64 delegates
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate Single GetCurrentControllerValue64(int controlID);

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate bool GetRailSimLocoChanged64();

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate IntPtr GetLocoName64();

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate bool GetRailSimConnected64();

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate bool IsLocoSet64();

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate void SetRailDriverConnected64(bool connected);

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate void SetRailSimConnected64(bool connected);

// x86 delegates
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate Single GetCurrentControllerValue86(int controlID);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool GetRailSimLocoChanged86();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr GetLocoName86();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool GetRailSimConnected86();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool IsLocoSet86();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void SetRailDriverConnected86(bool connected);

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public delegate void SetRailSimConnected86(bool connected);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr GetControllerList();