using Avalonia.Controls;
using NWNBuilder.Core;

namespace NWNBuilder;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = BuildJson.LoadSample("hesper-salcedo");
    }
}
