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
        if (settingId.StartsWith("drr:")) return "SetDisplayConfig (CCD API; DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE + SDC_VIRTUAL_REFRESH_RATE_AWARE)";
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
            "gamedvr" => @"HKCU\System\GameConfigStore\GameDVR_Enabled  +  HKCU\...\GameDVR\AppCaptureEnabled  +  HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR\AllowGameDVR (policy lock)",
            "mouseaccel" => @"user32.SystemParametersInfo SPI_SETMOUSE  +  HKCU\Control Panel\Mouse",
            "fso" => @"HKCU\System\GameConfigStore (GameDVR_FSEBehaviorMode + 3 related DWORDs)",
            "vrr" => @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\VRROptimizeEnable (DWORD)",
            "sysresponse" => @"HKLM\SOFTWARE\...\Multimedia\SystemProfile\SystemResponsiveness (DWORD)",
            "netthrottle" => @"HKLM\SOFTWARE\...\Multimedia\SystemProfile\NetworkThrottlingIndex (DWORD)",
            "usbsuspend" => @"HKLM\SYSTEM\CurrentControlSet\Services\USB\DisableSelectiveSuspend (DWORD)",
            "gamestask" => @"HKLM\SOFTWARE\...\Multimedia\SystemProfile\Tasks\Games (Priority + Scheduling Category + SFIO Priority)",
            "powerplan" => @"powrprof.dll PowerSetActiveScheme  (verifiable via 'powercfg /getactivescheme')",
            "cpuplan" => @"powrprof.dll PowerDuplicateScheme + PowerWriteAC/DCValueIndex (build a Balanced-clone tuned plan) + PowerSetActiveScheme  (verifiable via 'powercfg /query <guid> SUB_PROCESSOR')",
            "ai.copilot" => @"HKLM + HKCU\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot\TurnOffWindowsCopilot  +  HKCU\...\Explorer\Advanced\ShowCopilotButton  +  HKCU\...\Shell\BrandedKey\AppAumid  +  HKCU\...\BackgroundAccessApplications\Microsoft.Copilot_8wekyb3d8bbwe\DisabledByUser",
            "ai.recall" => @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\{AllowRecallEnablement, DisableAIDataAnalysis, TurnOffSavingSnapshots}",
            "ai.clicktodo" => @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\DisableClickToDo  +  HKCU\Software\Microsoft\Windows\Shell\ClickToDo\DisableClickToDo",
            "ai.edge" => @"HKLM\SOFTWARE\Policies\Microsoft\Edge\{HubsSidebarEnabled, CopilotPageContext, GenAILocalFoundationalModelSettings, ComposeInlineEnabled, AllowBrowsingWithCopilot}",
            "ai.notepadpaint" => @"HKCU\Software\Microsoft\Notepad\RewriteEnabled  +  HKCU\...\Paint\{DisableCocreator, DisableImageCreator, DisableGenerativeErase}  +  HKCU\...\Applets\Paint\View\IsSignedUpForTargetingService  +  HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint\DisableImageCreator",
            "ai.settingssearch" => @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search\BingSearchEnabled (authoritative)  +  HKCU\...\SearchSettings\IsDynamicSearchBoxEnabled  +  HKCU\SOFTWARE\Policies\Microsoft\Windows\Explorer\DisableSearchBoxSuggestions (best-effort)",
            "ai.actions" => @"HKLM\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\{1853569164, 4098520719}\EnabledState (DWORD; 1 = force-disabled, 2 = force-enabled, absent = server default)",
            "ai.inputinsights" => @"HKCU\Software\Microsoft\InputPersonalization\RestrictImplicitTextCollection  +  HKCU\Software\Microsoft\input\Settings\InsightsEnabled",
            "ai.office" => @"HKCU\Software\Microsoft\Office\16.0\{Word\Options\EnableCopilot, Excel\Options\EnableCopilot, OneNote\Options\Copilot\CopilotEnabled}  +  HKLM\SOFTWARE\Policies\Microsoft\office\16.0\common\ai\training\general\disabletraining",
            "privacy.advertisingid" => @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo\Enabled (DWORD)",
            "privacy.tailoredexp" => @"HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy\TailoredExperiencesWithDiagnosticDataEnabled (DWORD)",
            "privacy.cdp" => @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\EnableCdp (DWORD; absent = Windows default ON)",
            "privacy.activityhistory" => @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\{EnableActivityFeed, PublishUserActivities, UploadUserActivities} (DWORD)",
            "powerthrottling" => @"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling\PowerThrottlingOff (DWORD; absent = Windows default)",
            "faststartup" => @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\HiberbootEnabled (DWORD; reboot required)",
            "visualfx" => @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\VisualFXSetting (DWORD; 2=best perf) + HKCU\Control Panel\Desktop\UserPreferencesMask (REG_BINARY)",
            "network.nagle" => @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID}\{TcpAckFrequency, TCPNoDelay} (DWORD; per active adapter)",
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
        if (settingId.StartsWith("drr:"))
            return "(no PowerShell equivalent; uses SetDisplayConfig with the BOOST_REFRESH_RATE path flag)";

        return settingId switch
        {
            "hags" => $@"Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name HwSchMode -Value {OrDefault(rawDesired, "2")} -Type DWord",
            "memintegrity" => $@"Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled -Value {OrDefault(rawDesired, "1")} -Type DWord",
            "gamemode" => $@"Set-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled -Value {OrDefault(rawDesired, "1")} -Type DWord",
            "gamedvr" => @"Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled -Value 0 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR' -Name AppCaptureEnabled -Value 0 -Type DWord; $p='HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR'; New-Item $p -Force | Out-Null; Set-ItemProperty $p -Name AllowGameDVR -Value 0 -Type DWord   # reverse: set HKCU values to 1 and Remove-ItemProperty $p -Name AllowGameDVR",
            "mouseaccel" => @"# Enhance pointer precision OFF (per-user). Use SystemParametersInfo SPI_SETMOUSE in a small EXE; PowerShell can't call it cleanly.",
            "fso" => @"Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode -Value 2 -Type DWord",
            "vrr" => $@"Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name VRROptimizeEnable -Value {OrDefault(rawDesired, "1")} -Type DWord",
            "sysresponse" => $@"Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name SystemResponsiveness -Value {OrDefault(rawDesired, "10")} -Type DWord",
            "netthrottle" => $@"Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name NetworkThrottlingIndex -Value {OrDefault(rawDesired, "4294967295")} -Type DWord",
            "usbsuspend" => $@"Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend -Value {OrDefault(rawDesired, "1")} -Type DWord",
            "gamestask" => @"# Apply the Games multimedia task profile (Priority/Scheduling Category/SFIO Priority). The app writes 4 DWORDs under HKLM\...\Tasks\Games -- see SettingDocs.MechanismFor for the path.",
            "powerplan" => $"powercfg /setactive {OrDefault(rawDesired, "(plan-guid)")}",
            // Template: the actual GUIDs/values are runtime-resolved and shown in
            // changes.log / the Apply Results window. The exact-overrides docs
            // obligation is met by SettingDocsCatalog and docs/CPU-AWARE-POWER-PLANS.md.
            "cpuplan" => "powercfg -duplicatescheme SCHEME_BALANCED  # -> <new-guid>; " +
                         "powercfg -setacvalueindex <new-guid> SUB_PROCESSOR <setting-guid> <value>  (repeat per override; actual GUIDs/values in changes.log); " +
                         "powercfg -setactive <new-guid>",
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
                                @"Set-ItemProperty $pt -Name DisableGenerativeErase -Value 1 -Type DWord; " +
                                @"$pv='HKCU:\Software\Microsoft\Windows\CurrentVersion\Applets\Paint\View'; New-Item $pv -Force | Out-Null; " +
                                @"Set-ItemProperty $pv -Name IsSignedUpForTargetingService -Value 0 -Type DWord; " +
                                @"$hk='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint'; New-Item $hk -Force | Out-Null; " +
                                @"Set-ItemProperty $hk -Name DisableImageCreator -Value 1 -Type DWord",
            "ai.settingssearch" => @"Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name BingSearchEnabled -Value 0 -Type DWord; " +
                                  @"$ss='HKCU:\Software\Microsoft\Windows\CurrentVersion\SearchSettings'; New-Item $ss -Force | Out-Null; " +
                                  @"Set-ItemProperty $ss -Name IsDynamicSearchBoxEnabled -Value 0 -Type DWord; " +
                                  @"$pe='HKCU:\SOFTWARE\Policies\Microsoft\Windows\Explorer'; New-Item $pe -Force | Out-Null; " +
                                  @"Set-ItemProperty $pe -Name DisableSearchBoxSuggestions -Value 1 -Type DWord",
            "ai.actions" => @"$r='HKLM:\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8'; " +
                           @"foreach($id in 1853569164, 4098520719) { New-Item -Path ""$r\$id"" -Force | Out-Null; " +
                           @"Set-ItemProperty -Path ""$r\$id"" -Name EnabledState -Value 1 -Type DWord }",
            "ai.inputinsights" => @"Set-ItemProperty 'HKCU:\Software\Microsoft\InputPersonalization' -Name RestrictImplicitTextCollection -Value 1 -Type DWord; " +
                                 @"Set-ItemProperty 'HKCU:\Software\Microsoft\input\Settings' -Name InsightsEnabled -Value 0 -Type DWord",
            "ai.office" => @"Set-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Word\Options' -Name EnableCopilot -Value 0 -Type DWord; " +
                          @"Set-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Excel\Options' -Name EnableCopilot -Value 0 -Type DWord; " +
                          @"$on='HKCU:\Software\Microsoft\Office\16.0\OneNote\Options\Copilot'; New-Item $on -Force | Out-Null; " +
                          @"Set-ItemProperty $on -Name CopilotEnabled -Value 0 -Type DWord; " +
                          @"$tr='HKLM:\SOFTWARE\Policies\Microsoft\office\16.0\common\ai\training\general'; New-Item $tr -Force | Out-Null; " +
                          @"Set-ItemProperty $tr -Name disabletraining -Value 1 -Type DWord",
            "privacy.advertisingid" => @"Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo' -Name Enabled -Value 0 -Type DWord",
            "privacy.tailoredexp" => @"Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Privacy' -Name TailoredExperiencesWithDiagnosticDataEnabled -Value 0 -Type DWord",
            "privacy.cdp" => @"$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'; Set-ItemProperty $k -Name EnableCdp -Value 0 -Type DWord   # reverse: Remove-ItemProperty $k -Name EnableCdp",
            "privacy.activityhistory" => @"$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'; foreach($n in 'EnableActivityFeed','PublishUserActivities','UploadUserActivities'){ Set-ItemProperty $k -Name $n -Value 0 -Type DWord }   # reverse: Remove-ItemProperty for each",
            "powerthrottling" => @"$k='HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling'; New-Item $k -Force | Out-Null; Set-ItemProperty $k -Name PowerThrottlingOff -Value 1 -Type DWord   # reverse: Remove-ItemProperty $k -Name PowerThrottlingOff",
            "faststartup" => @"Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power' -Name HiberbootEnabled -Value 0 -Type DWord   # reboot required; reverse: set to 1",
            "visualfx" => @"Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects' -Name VisualFXSetting -Value 2 -Type DWord; Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name UserPreferencesMask -Value ([byte[]](0x90,0x12,0x03,0x80,0x10,0,0,0)) -Type Binary   # reverse: VisualFXSetting=0; sign out to fully apply",
            "network.nagle" => @"# Per adapter interface key (repeat for each active adapter GUID):" + "\n" +
                              @"$if='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\<GUID>'; Set-ItemProperty $if -Name TcpAckFrequency -Value 1 -Type DWord; Set-ItemProperty $if -Name TCPNoDelay -Value 1 -Type DWord   # reverse: Remove-ItemProperty both per adapter",
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
        if (settingId.StartsWith("drr:"))
            return "Settings -> System -> Display -> Advanced display -> 'Choose a refresh rate' (Dynamic = DRR on)";
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
            "gamedvr" => @"@{Capture=(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled -EA SilentlyContinue).GameDVR_Enabled; Policy=(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR' -Name AllowGameDVR -EA SilentlyContinue).AllowGameDVR}",
            "mouseaccel" => @"(Get-ItemProperty 'HKCU:\Control Panel\Mouse' -Name MouseSpeed).MouseSpeed",
            "fso" => @"(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode).GameDVR_FSEBehaviorMode",
            "vrr" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name VRROptimizeEnable).VRROptimizeEnable",
            "sysresponse" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name SystemResponsiveness).SystemResponsiveness",
            "netthrottle" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name NetworkThrottlingIndex).NetworkThrottlingIndex",
            "usbsuspend" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend).DisableSelectiveSuspend",
            "gamestask" => @"Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games'",
            "powerplan" => @"powercfg /getactivescheme",
            "cpuplan" => @"powercfg /getactivescheme; powercfg /query SCHEME_CURRENT SUB_PROCESSOR",
            "ai.copilot" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot' -Name TurnOffWindowsCopilot -EA SilentlyContinue).TurnOffWindowsCopilot",
            "ai.recall" => @"$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI'; @{Allow=(Get-ItemProperty $k -Name AllowRecallEnablement -EA SilentlyContinue).AllowRecallEnablement; Disable=(Get-ItemProperty $k -Name DisableAIDataAnalysis -EA SilentlyContinue).DisableAIDataAnalysis; Snap=(Get-ItemProperty $k -Name TurnOffSavingSnapshots -EA SilentlyContinue).TurnOffSavingSnapshots}",
            "ai.clicktodo" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI' -Name DisableClickToDo -EA SilentlyContinue).DisableClickToDo",
            "ai.edge" => @"$k='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; @{Hubs=(Get-ItemProperty $k -Name HubsSidebarEnabled -EA SilentlyContinue).HubsSidebarEnabled; Ctx=(Get-ItemProperty $k -Name CopilotPageContext -EA SilentlyContinue).CopilotPageContext; Gen=(Get-ItemProperty $k -Name GenAILocalFoundationalModelSettings -EA SilentlyContinue).GenAILocalFoundationalModelSettings; Compose=(Get-ItemProperty $k -Name ComposeInlineEnabled -EA SilentlyContinue).ComposeInlineEnabled; Browse=(Get-ItemProperty $k -Name AllowBrowsingWithCopilot -EA SilentlyContinue).AllowBrowsingWithCopilot}",
            "ai.notepadpaint" => @"$np='HKCU:\Software\Microsoft\Notepad'; $pt='HKCU:\Software\Microsoft\Windows\CurrentVersion\Paint'; @{Rewrite=(Get-ItemProperty $np -Name RewriteEnabled -EA SilentlyContinue).RewriteEnabled; Cocreator=(Get-ItemProperty $pt -Name DisableCocreator -EA SilentlyContinue).DisableCocreator}",
            "ai.settingssearch" => @"@{Bing=(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name BingSearchEnabled -EA SilentlyContinue).BingSearchEnabled; Dynamic=(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\SearchSettings' -Name IsDynamicSearchBoxEnabled -EA SilentlyContinue).IsDynamicSearchBoxEnabled; Disable=(Get-ItemProperty 'HKCU:\SOFTWARE\Policies\Microsoft\Windows\Explorer' -Name DisableSearchBoxSuggestions -EA SilentlyContinue).DisableSearchBoxSuggestions}",
            "ai.actions" => @"$r='HKLM:\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8'; @{A=(Get-ItemProperty $r\1853569164 -Name EnabledState -EA SilentlyContinue).EnabledState; B=(Get-ItemProperty $r\4098520719 -Name EnabledState -EA SilentlyContinue).EnabledState}",
            "ai.inputinsights" => @"@{Restrict=(Get-ItemProperty 'HKCU:\Software\Microsoft\InputPersonalization' -Name RestrictImplicitTextCollection -EA SilentlyContinue).RestrictImplicitTextCollection; Insights=(Get-ItemProperty 'HKCU:\Software\Microsoft\input\Settings' -Name InsightsEnabled -EA SilentlyContinue).InsightsEnabled}",
            "ai.office" => @"@{Word=(Get-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Word\Options' -Name EnableCopilot -EA SilentlyContinue).EnableCopilot; Excel=(Get-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Excel\Options' -Name EnableCopilot -EA SilentlyContinue).EnableCopilot; OneNote=(Get-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\OneNote\Options\Copilot' -Name CopilotEnabled -EA SilentlyContinue).CopilotEnabled; Training=(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\office\16.0\common\ai\training\general' -Name disabletraining -EA SilentlyContinue).disabletraining}",
            "privacy.advertisingid" => @"(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo' -Name Enabled -EA SilentlyContinue).Enabled",
            "privacy.tailoredexp" => @"(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Privacy' -Name TailoredExperiencesWithDiagnosticDataEnabled -EA SilentlyContinue).TailoredExperiencesWithDiagnosticDataEnabled",
            "privacy.cdp" => @"(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name EnableCdp -EA SilentlyContinue).EnableCdp",
            "privacy.activityhistory" => @"$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'; @{Feed=(Get-ItemProperty $k -Name EnableActivityFeed -EA SilentlyContinue).EnableActivityFeed; Publish=(Get-ItemProperty $k -Name PublishUserActivities -EA SilentlyContinue).PublishUserActivities; Upload=(Get-ItemProperty $k -Name UploadUserActivities -EA SilentlyContinue).UploadUserActivities}",
            "powerthrottling" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling' -Name PowerThrottlingOff -EA SilentlyContinue).PowerThrottlingOff",
            "faststartup" => @"(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power' -Name HiberbootEnabled -EA SilentlyContinue).HiberbootEnabled",
            "visualfx" => @"(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects' -Name VisualFXSetting -EA SilentlyContinue).VisualFXSetting",
            "network.nagle" => @"Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces' | ForEach-Object { [pscustomobject]@{ If=$_.PSChildName; Ack=(Get-ItemProperty $_.PSPath -Name TcpAckFrequency -EA SilentlyContinue).TcpAckFrequency; NoDelay=(Get-ItemProperty $_.PSPath -Name TCPNoDelay -EA SilentlyContinue).TCPNoDelay } }",
            _ => "",
        };
    }
}
