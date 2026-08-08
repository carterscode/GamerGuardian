using System.Windows;
using System.Windows.Controls;

namespace GamerGuardian.UI.Views;

/// <summary>
/// Windows services view: the 28-service catalog plus the Application Experience
/// scheduled tasks, and the two preset radios that both reflect and set state.
/// Handlers forward to <see cref="SettingsWindow"/>, which owns the draft.
/// </summary>
public partial class ServicesView : System.Windows.Controls.UserControl
{
    public ServicesView() => InitializeComponent();

    internal SettingsWindow? Owner { get; set; }

    private void ServicesPresetGaming_Checked(object sender, RoutedEventArgs e) =>
        Owner?.ServicesPresetGaming_Checked(sender, e);

    private void ServicesPresetDefault_Checked(object sender, RoutedEventArgs e) =>
        Owner?.ServicesPresetDefault_Checked(sender, e);
}
