using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using DownloaderGUI.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace DownloaderGUI.Views
{
    public partial class MainWindow : Window
    {
        private TextBox _folderPathTextBox;
        public string SelectedItem { get; private set; }
        private ExperimentalAcrylicBorder acrylicBorder;
        private ExperimentalAcrylicBorder mainAcrylicBorder;
        private ToggleSwitch _sillySwitch;
        private string colorFilePath = "Theme.config";

        public MainWindow()
        {
            InitializeComponent(); //Initialize components at startup
            SetDefaultPath(); //Initialize SetDefaultPath at startup (so that the default path is always set)
            JSONstopwatch();

            comboBox = this.FindControl<ComboBox>("comboBox");
            acrylicBorder = this.FindControl<ExperimentalAcrylicBorder>("AcrylicBorder");
            mainAcrylicBorder = this.FindControl<ExperimentalAcrylicBorder>("MainAcrylicBorder");
            _sillySwitch = this.FindControl<ToggleSwitch>("sillySwitch");
            Closing += MainWindow_Closing;
            InitializeColorFromFile();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this); //Load the .axaml
            _folderPathTextBox = this.FindControl<TextBox>("FolderPathTextBox"); //_folderpathTextBox controls what's inside of the textbox with the download path
            StatusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
            StatusProgressBar = this.FindControl<ProgressBar>("StatusProgressBar");
        }

        private void SetDefaultPath() //Set the default download path
        {
            var defaultPath = DefaultPathManager.ReadDefaultPath();  //defaultPath is the output of ReadDefaultPath in DefaultPathManager.cs
            if (!string.IsNullOrEmpty(defaultPath))
            {
                _folderPathTextBox.Text = defaultPath; //_folderpathTextbox is equal to the defaultPath 
            }
        }

        private async void Button_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e) //What happens when you press the button for folder selection
        {
            var dialog = new OpenFolderDialog(); //Open the folder selection screen (they say OpenFolderDialog is obsolete but it works a least, not like other solutions, fucking Microsoft)
            var selectedFolder = await dialog.ShowAsync(this); //selectedfolder is equal to the selection
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                _folderPathTextBox.Text = selectedFolder; //the text on the _folderpathTextBox is now equal to the selected path
                DefaultPathManager.WriteDefaultPath(selectedFolder); //call WriteDefaultPath on DefaultPathManager with selectedFolder
                (DataContext as MainWindowViewModel)?.PopulateLibrary(); //Call populatelibrary (it works somehow)
            }
        }

        private async void JSONstopwatch()
        {
            Stopwatch ciao = new Stopwatch();
            ciao.Start(); //Start a stopwatch
            UpdateStatus("Connection to the servers to fetch manga titles"); //show text on textblock
            StatusProgressBar.IsIndeterminate = true; //The progressbar is indeterminate
            var mainWindowViewModel = new MainWindowViewModel();
            await MainWindowViewModel.GetJSON(new SearchViewModel(mainWindowViewModel)); //Call GetJSON method in MainWindowViewModel
            ciao.Stop(); //stop the stopwatch
            UpdateStatus(""); //Update the status to nothing (make the textblock disappear)
            StatusProgressBar.IsIndeterminate = false; //set the progressbar to determinate
            StatusProgressBar.Value = 0; //set the progress to 0 making it look like inactive
        }

        public void UpdateStatus(string status) //What does updatestatus mean
        {
            StatusTextBlock.Text = status; //the text on the textblock is "status"
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e) //When pressing a specific key on the textbox associated
        {
            if (e.Key == Key.Enter) //If "enter" is pressed
            {
                (DataContext as MainWindowViewModel)?.SearchViewModel?.SearchCommand?.Execute().Subscribe(); //Call the searchcommand in SearchViewModel
            }
        }

        private void ComboBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Tag is string tag)
                {
                    acrylicBorder.Material.TintColor = Avalonia.Media.Color.Parse(tag);
                    mainAcrylicBorder.Material.TintColor = Avalonia.Media.Color.Parse(tag);

                    SaveColorToFile(tag);
                }
            }
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
                    mainAcrylicBorder.Material.TintColor = Avalonia.Media.Color.Parse(savedColor);

                    foreach (var item in comboBox.Items)
                    {
                        if (item is ComboBoxItem comboBoxItem && comboBoxItem.Tag is string tag && tag == savedColor)
                        {
                            comboBox.SelectedItem = comboBoxItem;
                            break;
                        }
                    }
                }
            }
        }

        private void SaveColorToFile(string color)
        {
            File.WriteAllText(colorFilePath, color);
        }

        private void SillyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                var item = button.CommandParameter;

                if (item != null)
                {
                    Manga manga = item as Manga;

                    if (manga != null)
                    {
                        (DataContext as MainWindowViewModel)?.LibraryViewModel.ShowPdfList(manga, manga.Path);
                    }
                }
            }
        }

        private void PdfButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                var item = button.CommandParameter;
                if (item != null)
                {
                    PdfFileInfo fileInfo = item as PdfFileInfo;
                    if (fileInfo != null)
                    {
                        (DataContext as MainWindowViewModel)?.LibraryViewModel.OpenPdf(fileInfo.FilePath);
                    }
                }
            }
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_sillySwitch.IsChecked == true)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
    }
}
