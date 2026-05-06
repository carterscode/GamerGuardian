namespace GamerGuardian.Services;

/// <summary>
/// Per-setting documentation: what mechanism the app actually uses to read/write
/// the setting, and a copy-pasteable PowerShell command users can run to verify
/// the value externally.
/// </summary>
public static class SettingDocs
{
    public static string MechanismFor(string settingId)
    {
        if (settingId.StartsWith("hdr:")) return "DisplayConfigSetDeviceInfo (CCD API)";
        if (settingId.StartsWith("refresh:")) return "ChangeDisplaySettingsEx (DEVMODE.dmDisplayFrequency)";
        if (settingId.StartsWith("resolution:")) return "ChangeDisplaySettingsEx (DEVMODE.dmPelsWidth/Height)";
        return settingId switch
        {
            "hags" => @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode (DWORD)",
            "memintegrity" => @"HKLM\SYSTEM\...\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity\Enabled (DWORD)",
            "gamemode" => @"HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled / AllowAutoGameMode",
            "gamedvr" => @"HKCU\System\GameConfigStore\GameDVR_Enabled  +  HKCU\...\GameDVR\AppCaptureEnabled",
            "mouseaccel" => @"user32.SystemParametersInfo SPI_SETMOUSE  +  HKCU\Control Panel\Mouse",
            "fso" => @"HKCU\System\GameConfigStore (GameDVR_FSEBehaviorMode + 3 related DWORDs)",
            "vrr" => @"HKCU\Software\Microsoft\DirectX\UserGpuPreferences\DirectXUserGlobalSettings (REG_SZ, VRROptimizeEnable=)",
            "sysresponse" => @"HKLM\SOFTWARE\...\Multimedia\SystemProfile\SystemResponsiveness (DWORD)",
            "netthrottle" => @"HKLM\SOFTWARE\...\Multimedia\SystemProfile\NetworkThrottlingIndex (DWORD)",
            "usbsuspend" => @"HKLM\SYSTEM\CurrentControlSet\Services\USB\DisableSelectiveSuspend (DWORD)",
            "gamestask" => @"HKLM\SOFTWARE\...\Multimedia\SystemProfile\Tasks\Games (Priority + Scheduling Category + SFIO Priority)",
            "powerplan" => @"powrprof.dll PowerSetActiveScheme  (verifiable via 'powercfg /getactivescheme')",
            _ => "(unknown)",
        };
    }

    public static string VerifyCommandFor(string settingId)
    {
        if (settingId.StartsWith("hdr:") || settingId.StartsWith("refresh:") || settingId.StartsWith("resolution:"))
            return "Open Settings → System → Display, or run: dxdiag";
        return settingId switch
        {
            "hags" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name HwSchMode).HwSchMode",
            "memintegrity" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled).Enabled",
            "gamemode" => @"(Get-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled).AutoGameModeEnabled",
            "gamedvr" => @"(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled).GameDVR_Enabled",
            "mouseaccel" => @"(Get-ItemProperty 'HKCU:\Control Panel\Mouse' -Name MouseSpeed).MouseSpeed",
            "fso" => @"(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode).GameDVR_FSEBehaviorMode",
            "vrr" => @"(Get-ItemProperty 'HKCU:\Software\Microsoft\DirectX\UserGpuPreferences' -Name DirectXUserGlobalSettings).DirectXUserGlobalSettings",
            "sysresponse" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name SystemResponsiveness).SystemResponsiveness",
            "netthrottle" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name NetworkThrottlingIndex).NetworkThrottlingIndex",
            "usbsuspend" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend).DisableSelectiveSuspend",
            "gamestask" => @"Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games'",
            "powerplan" => @"powercfg /getactivescheme",
            _ => "",
        };
    }
}
