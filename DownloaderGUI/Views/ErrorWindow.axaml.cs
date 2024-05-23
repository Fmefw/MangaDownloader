using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace DownloaderGUI;

public partial class ErrorWindow : Window
{
    private readonly string colorFilePath = "Theme.config";

    public ErrorWindow(string errorMessage)
    {
        InitializeComponent();
        ErrorMessageTextBlock.Text = errorMessage;
        acrylicBorder = this.FindControl<ExperimentalAcrylicBorder>("acrylicBorder");
        InitializeColorFromFile();
    }

    public ErrorWindow()
    {
        InitializeComponent();
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