using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace EasyBluetoothAudio.Views;

/// <summary>
/// Presents one independently controlled Bluetooth audio source.
/// </summary>
public partial class DeviceSourceView : UserControl
{
    /// <summary>
    /// Initializes the source row and its ViewModel bindings.
    /// </summary>
    public DeviceSourceView()
    {
        InitializeComponent();
    }
}
