using Avalonia.Controls;
using System.IO;

namespace DownloaderGUI;

public partial class ErrorWindow : Window
{
    private string colorFilePath = "Theme.config";

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

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void InitializeColorFromFile()
    {
        string defaultColor = "#FF0044FF";

        if (!File.Exists(colorFilePath))
        {
            SaveColorToFile(defaultColor);
        }
        else
        {
            string savedColor = File.ReadAllText(colorFilePath);
            if (!string.IsNullOrWhiteSpace(savedColor))
            {
                acrylicBorder.Material.TintColor = Avalonia.Media.Color.Parse(savedColor);
            }
        }
    }

    private void SaveColorToFile(string color)
    {
        File.WriteAllText(colorFilePath, color);
    }
}