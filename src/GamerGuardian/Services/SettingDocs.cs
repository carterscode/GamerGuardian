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
        if (settingId.StartsWith("service:"))
        {
            var name = settingId["service:".Length..];
            var def = ServiceCatalog.All.FirstOrDefault(d =>
                d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (def?.PolicyOverride is { } po)
                return $"reg add HKLM\\{po.PolicyKey} /v {po.PolicyValue} (Group Policy override; Windows reverts the standard sc.exe path)";
            return $"sc.exe stop / config (writes HKLM\\SYSTEM\\CurrentControlSet\\Services\\{name}\\Start)";
        }
        if (settingId.StartsWith("ai.app:"))
        {
            var pkg = settingId["ai.app:".Length..];
            return $"Get-AppxPackage / Remove-AppxPackage (package: {pkg})";
        }
        return settingId switch
        {
            "hags" => @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode (DWORD)",
            "memintegrity" => @"HKLM\SYSTEM\...\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity\Enabled (DWORD)",
            "gamemode" => @"HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled / AllowAutoGameMode",
            "gamedvr" => @"HKCU\System\GameConfigStore\GameDVR_Enabled  +  HKCU\...\GameDVR\AppCaptureEnabled",
            "mouseaccel" => @"user32.SystemParametersInfo SPI_SETMOUSE  +  HKCU\Control Panel\Mouse",
            "fso" => @"HKCU\System\GameConfigStore (GameDVR_FSEBehaviorMode + 3 related DWORDs)",
            "vrr" => @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\VRROptimizeEnable (DWORD)",
            "sysresponse" => @"HKLM\SOFTWARE\...\Multimedia\SystemProfile\SystemResponsiveness (DWORD)",
            "netthrottle" => @"HKLM\SOFTWARE\...\Multimedia\SystemProfile\NetworkThrottlingIndex (DWORD)",
            "usbsuspend" => @"HKLM\SYSTEM\CurrentControlSet\Services\USB\DisableSelectiveSuspend (DWORD)",
            "gamestask" => @"HKLM\SOFTWARE\...\Multimedia\SystemProfile\Tasks\Games (Priority + Scheduling Category + SFIO Priority)",
            "powerplan" => @"powrprof.dll PowerSetActiveScheme  (verifiable via 'powercfg /getactivescheme')",
            "ai.copilot" => @"HKLM + HKCU\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot\TurnOffWindowsCopilot  +  HKCU\...\Explorer\Advanced\ShowCopilotButton",
            "ai.recall" => @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\{AllowRecallEnablement, DisableAIDataAnalysis}",
            "ai.clicktodo" => @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\DisableClickToDo  +  HKCU\Software\Microsoft\Windows\Shell\ClickToDo\DisableClickToDo",
            "ai.edge" => @"HKLM\SOFTWARE\Policies\Microsoft\Edge\{HubsSidebarEnabled, CopilotPageContext, GenAILocalFoundationalModelSettings}",
            "ai.notepadpaint" => @"HKCU\Software\Microsoft\Notepad\RewriteEnabled  +  HKCU\...\Paint\{DisableCocreator, DisableImageCreator, DisableGenerativeErase}",
            _ => "(unknown)",
        };
    }

    /// <summary>
    /// A copy-pasteable PowerShell command that reproduces what the app does to
    /// apply this setting. Surfaced in <c>changes.log</c> so a user reading the
    /// log can manually re-apply, automate via a script, or build a rollback by
    /// reading the Before value and flipping the apply command.
    ///
    /// <para>For settings that take a parameter (service start type, refresh rate, etc.)
    /// the command embeds the <paramref name="rawDesired"/> value the app actually
    /// wrote. Pass an empty string for settings where the value is binary/toggle.</para>
    /// </summary>
    public static string ApplyCommandFor(string settingId, string rawDesired = "")
    {
        if (settingId.StartsWith("service:"))
        {
            var name = settingId["service:".Length..];
            var def = ServiceCatalog.All.FirstOrDefault(d =>
                d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (def?.PolicyOverride is { } po)
            {
                if (rawDesired == "(deleted)")
                    return $"Remove-ItemProperty -Path 'HKLM:\\{po.PolicyKey}' -Name {po.PolicyValue} -Force";
                return $"New-Item -Path 'HKLM:\\{po.PolicyKey}' -Force | Out-Null; " +
                       $"Set-ItemProperty -Path 'HKLM:\\{po.PolicyKey}' -Name {po.PolicyValue} -Value {(string.IsNullOrEmpty(rawDesired) ? po.DisabledValue.ToString() : rawDesired)} -Type DWord";
            }
            // sc.exe maps start= words back from registry Start= dword.
            string startWord = rawDesired switch
            {
                "0" => "boot",
                "1" => "system",
                "2" => "auto",
                "3" => "demand",
                "4" => "disabled",
                _ => "demand",
            };
            return $"sc.exe stop \"{name}\"; sc.exe config \"{name}\" start= {startWord}";
        }
        if (settingId.StartsWith("ai.app:"))
        {
            var pkg = settingId["ai.app:".Length..];
            return $"Get-AppxPackage -Name '{pkg}' | Remove-AppxPackage";
        }
        if (settingId.StartsWith("hdr:"))
            return "(no direct PowerShell equivalent; uses DisplayConfigSetDeviceInfo via the CCD API)";
        if (settingId.StartsWith("refresh:"))
            return "(no direct PowerShell equivalent; uses ChangeDisplaySettingsEx -- consider DisplaySettings.dll on a custom build)";
        if (settingId.StartsWith("resolution:"))
            return "(no direct PowerShell equivalent; uses ChangeDisplaySettingsEx)";

        return settingId switch
        {
            "hags" => $@"Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name HwSchMode -Value {OrDefault(rawDesired, "2")} -Type DWord",
            "memintegrity" => $@"Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled -Value {OrDefault(rawDesired, "1")} -Type DWord",
            "gamemode" => $@"Set-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled -Value {OrDefault(rawDesired, "1")} -Type DWord",
            "gamedvr" => $@"Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled -Value {OrDefault(rawDesired, "0")} -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR' -Name AppCaptureEnabled -Value {OrDefault(rawDesired, "0")} -Type DWord",
            "mouseaccel" => @"# Enhance pointer precision OFF (per-user). Use SystemParametersInfo SPI_SETMOUSE in a small EXE; PowerShell can't call it cleanly.",
            "fso" => @"Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode -Value 2 -Type DWord",
            "vrr" => $@"Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name VRROptimizeEnable -Value {OrDefault(rawDesired, "1")} -Type DWord",
            "sysresponse" => $@"Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name SystemResponsiveness -Value {OrDefault(rawDesired, "10")} -Type DWord",
            "netthrottle" => $@"Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name NetworkThrottlingIndex -Value {OrDefault(rawDesired, "4294967295")} -Type DWord",
            "usbsuspend" => $@"Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend -Value {OrDefault(rawDesired, "1")} -Type DWord",
            "gamestask" => @"# Apply the Games multimedia task profile (Priority/Scheduling Category/SFIO Priority). The app writes 4 DWORDs under HKLM\...\Tasks\Games -- see SettingDocs.MechanismFor for the path.",
            "powerplan" => $"powercfg /setactive {OrDefault(rawDesired, "(plan-guid)")}",
            "ai.copilot" => @"# Disable Windows Copilot:" + "\n" +
                           @"Set-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot' -Name TurnOffWindowsCopilot -Value 1 -Type DWord; " +
                           @"Set-ItemProperty 'HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot' -Name TurnOffWindowsCopilot -Value 1 -Type DWord; " +
                           @"Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name ShowCopilotButton -Value 0 -Type DWord",
            "ai.recall" => @"$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI'; New-Item $k -Force | Out-Null; " +
                          @"Set-ItemProperty $k -Name AllowRecallEnablement -Value 0 -Type DWord; " +
                          @"Set-ItemProperty $k -Name DisableAIDataAnalysis -Value 1 -Type DWord",
            "ai.clicktodo" => @"Set-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI' -Name DisableClickToDo -Value 1 -Type DWord; " +
                              @"Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\Shell\ClickToDo' -Name DisableClickToDo -Value 1 -Type DWord",
            "ai.edge" => @"$k='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; New-Item $k -Force | Out-Null; " +
                        @"Set-ItemProperty $k -Name HubsSidebarEnabled -Value 0 -Type DWord; " +
                        @"Set-ItemProperty $k -Name CopilotPageContext -Value 0 -Type DWord; " +
                        @"Set-ItemProperty $k -Name GenAILocalFoundationalModelSettings -Value 1 -Type DWord",
            "ai.notepadpaint" => @"Set-ItemProperty 'HKCU:\Software\Microsoft\Notepad' -Name RewriteEnabled -Value 0 -Type DWord; " +
                                @"$pt='HKCU:\Software\Microsoft\Windows\CurrentVersion\Paint'; New-Item $pt -Force | Out-Null; " +
                                @"Set-ItemProperty $pt -Name DisableCocreator -Value 1 -Type DWord; " +
                                @"Set-ItemProperty $pt -Name DisableImageCreator -Value 1 -Type DWord; " +
                                @"Set-ItemProperty $pt -Name DisableGenerativeErase -Value 1 -Type DWord",
            _ => "",
        };
    }

    private static string OrDefault(string raw, string fallback) =>
        string.IsNullOrEmpty(raw) ? fallback : raw;

    public static string VerifyCommandFor(string settingId)
    {
        if (settingId.StartsWith("ai.app:"))
        {
            var pkg = settingId["ai.app:".Length..];
            return $"Get-AppxPackage -Name '{pkg}'   # empty output = removed";
        }
        if (settingId.StartsWith("hdr:") || settingId.StartsWith("refresh:") || settingId.StartsWith("resolution:"))
            return "Open Settings -> System -> Display, or run: dxdiag";
        if (settingId.StartsWith("service:"))
        {
            var name = settingId["service:".Length..];
            var def = ServiceCatalog.All.FirstOrDefault(d =>
                d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (def?.PolicyOverride is { } po)
                return $"(Get-ItemProperty 'HKLM:\\{po.PolicyKey}' -Name {po.PolicyValue} -EA SilentlyContinue).{po.PolicyValue}";
            return $"sc qc \"{name}\"   # look for START_TYPE";
        }
        return settingId switch
        {
            "hags" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name HwSchMode).HwSchMode",
            "memintegrity" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled).Enabled",
            "gamemode" => @"(Get-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled).AutoGameModeEnabled",
            "gamedvr" => @"(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled).GameDVR_Enabled",
            "mouseaccel" => @"(Get-ItemProperty 'HKCU:\Control Panel\Mouse' -Name MouseSpeed).MouseSpeed",
            "fso" => @"(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode).GameDVR_FSEBehaviorMode",
            "vrr" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name VRROptimizeEnable).VRROptimizeEnable",
            "sysresponse" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name SystemResponsiveness).SystemResponsiveness",
            "netthrottle" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name NetworkThrottlingIndex).NetworkThrottlingIndex",
            "usbsuspend" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend).DisableSelectiveSuspend",
            "gamestask" => @"Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games'",
            "powerplan" => @"powercfg /getactivescheme",
            "ai.copilot" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot' -Name TurnOffWindowsCopilot -EA SilentlyContinue).TurnOffWindowsCopilot",
            "ai.recall" => @"$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI'; @{Allow=(Get-ItemProperty $k -Name AllowRecallEnablement -EA SilentlyContinue).AllowRecallEnablement; Disable=(Get-ItemProperty $k -Name DisableAIDataAnalysis -EA SilentlyContinue).DisableAIDataAnalysis}",
            "ai.clicktodo" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI' -Name DisableClickToDo -EA SilentlyContinue).DisableClickToDo",
            "ai.edge" => @"$k='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; @{Hubs=(Get-ItemProperty $k -Name HubsSidebarEnabled -EA SilentlyContinue).HubsSidebarEnabled; Ctx=(Get-ItemProperty $k -Name CopilotPageContext -EA SilentlyContinue).CopilotPageContext; Gen=(Get-ItemProperty $k -Name GenAILocalFoundationalModelSettings -EA SilentlyContinue).GenAILocalFoundationalModelSettings}",
            "ai.notepadpaint" => @"$np='HKCU:\Software\Microsoft\Notepad'; $pt='HKCU:\Software\Microsoft\Windows\CurrentVersion\Paint'; @{Rewrite=(Get-ItemProperty $np -Name RewriteEnabled -EA SilentlyContinue).RewriteEnabled; Cocreator=(Get-ItemProperty $pt -Name DisableCocreator -EA SilentlyContinue).DisableCocreator}",
            _ => "",
        };
    }
}
