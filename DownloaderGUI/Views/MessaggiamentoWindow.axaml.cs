using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace DownloaderGUI.Views;

public partial class MessaggiamentoWindow : Window
{
    private readonly string colorFilePath = "Theme.config";

    public MessaggiamentoWindow()
    {
        InitializeComponent();
        acrylicBorder = this.FindControl<ExperimentalAcrylicBorder>("acrylicBorder");
        InitializeColorFromFile();
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void InitializeColorFromFile()
    {
        var defaultColor = "#FF0044FF";

        if (!File.Exists(colorFilePath))
        {
            SaveColorToFile(defaultColor);
        }
        else
        {
            var savedColor = File.ReadAllText(colorFilePath);
            if (!string.IsNullOrWhiteSpace(savedColor)) acrylicBorder.Material.TintColor = Color.Parse(savedColor);
        }
    }

    private void SaveColorToFile(string color)
    {
        File.WriteAllText(colorFilePath, color);
    }
}