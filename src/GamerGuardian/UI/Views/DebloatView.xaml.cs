using System.Windows.Controls;

namespace GamerGuardian.UI.Views;

/// <summary>Presentation-only view. Rows are supplied by SettingsWindow via the
/// bound ObservableCollections; this class owns no logic.</summary>
public partial class 
DebloatView
 : System.Windows.Controls.UserControl
{
    public 
DebloatView
() => InitializeComponent();
}
