using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using DownloaderGUI.Views;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Prism.Commands;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace DownloaderGUI.ViewModels;

public class ChapterViewModel : ReactiveObject
{
    private bool _isChecked;

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    public string? ChapterNumber { get; set; } // Add any other properties you need
}

public class SearchViewModel : ReactiveObject //This manages the search for the downloads
{
    private ObservableCollection<ChapterViewModel>?
        _chapterText; //Private property for the list containing chapter numbers

    private string? _displayText; //Private field for the text "there are x chapters" etc.

    private string? _downloadText;

    private readonly MainWindowViewModel _mainWindowViewModel; //Declare the MainWindowViewModel

    private List<string>? _searchResults; //Private field for search results
    private string? _searchText; //Private field for the searchtext

    private string? _selectedItem;

    private readonly ObservableAsPropertyHelper<string> _selectedItemText; //wrap Observable into a Property

    private bool _showOptions; //Private property to show the listbox with the chapter numbers and other assets

    private bool _showStatus; //Show the status text and the big status bar

    private bool _statusBar; //Show the little status bar

    public SearchViewModel(MainWindowViewModel mainWindowViewModel)
    {
        ChapterText =
            new ObservableCollection<ChapterViewModel>(); //ChapterText is an observablecollection of ChapterViewModel
        SearchCommand = ReactiveCommand.CreateFromObservable(Search); //SearchCommand is equal to Observable Search
        SelectAllCommand = ReactiveCommand.Create(SelectAll); //SelectAllCommand method is called by SelectAll
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll); //DeselectAllCommand method is called by DeselectAll
        DownloadCommand = new DelegateCommand(async () => await MakeDownload()); //DownloadCommand is MakeDownload
        _mainWindowViewModel = mainWindowViewModel; //declare that _mainWindowViewModel is MainWindowViewModel

        _selectedItemText = this.WhenAnyValue(x => x.SelectedItem)
            .WhereNotNull()
            .Select(selectedItem => selectedItem.ToString())
            .ToProperty(this, x => x.SelectedItemText, out _selectedItemText);

        this.WhenAnyValue(x => x.SelectedItemText) //When SelectedItemText has a value
            .Where(text => !string.IsNullOrEmpty(text))
            .Subscribe(async selectedItemText => //Do a lot of stuff
            {
                var generatedPermalink =
                    await MainWindowViewModel
                        .linkGenerator(selectedItemText); //generatedPermalink is the output of linkgenerator
                DisplayText = "Looking for available chapters...";
                var chapterMaxNumber =
                    await ChapterJSON(generatedPermalink); //chapterMaxNumber is the max number from ChapterJSON
                DisplayText = $@"Currently there are {chapterMaxNumber} chaptes available for '{selectedItemText}'";
                var chapterNumbers =
                    GenerateChapterNumbers(chapterMaxNumber); //Generate a series of entries with GenerateChapterNumbers

                var chapter = new ObservableCollection<ChapterViewModel>(); //Declare new chapter list
                foreach (var chapterNumber in chapterNumbers) //For each entry
                    chapter.Add(new ChapterViewModel
                    {
                        ChapterNumber = $"Chapter {chapterNumber}",
                        IsChecked = false
                    });
                ChapterText = chapter; //List chaptertext is equal to chapter
                ShowOptions = true; //Show everything
            });
    }

    public string? SearchText //Public property to access the searchtext (and link it to the TextBox)
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public List<string>? SearchResults //Public property to access search results
    {
        get => _searchResults;
        set => this.RaiseAndSetIfChanged(ref _searchResults, value);
    }

    public string? DisplayText //Public property
    {
        get => _displayText;
        set => this.RaiseAndSetIfChanged(ref _displayText, value);
    }

    public string? DownloadText
    {
        get => _downloadText;
        set => this.RaiseAndSetIfChanged(ref _downloadText, value);
    }

    public string SelectedItemText => _selectedItemText.Value;

    public string? SelectedItem
    {
        get => _selectedItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedItem, value);

            ClearSearchResults();
        }
    }

    public bool ShowOptions
    {
        get => _showOptions;
        set => this.RaiseAndSetIfChanged(ref _showOptions, value);
    }

    public bool ShowStatus
    {
        get => _showStatus;
        set => this.RaiseAndSetIfChanged(ref _showStatus, value);
    }

    public bool StatusBar
    {
        get => _statusBar;
        set => this.RaiseAndSetIfChanged(ref _statusBar, value);
    }

    public ObservableCollection<ChapterViewModel>? ChapterText
    {
        get => _chapterText;
        set => this.RaiseAndSetIfChanged(ref _chapterText, value);
    }

    public ReactiveCommand<Unit, List<string>>
        SearchCommand { get; } //Set a command, to associate it with the search button

    public ReactiveCommand<Unit, Unit> SelectAllCommand { get; } //Set a command associated with select all button
    public ReactiveCommand<Unit, Unit> DeselectAllCommand { get; } //Set a command associated with deselect all button

    public ICommand DownloadCommand { get; } //Set an ICommand to make the download


    private IObservable<List<string>> Search() //This is where the list is actually created
    {
        return Observable.Start(() => //Using Observable.Start to run the asynchronous operation
        {
            ShowOptions = false;
            DisplayText = "";
            var searchResult = MainWindowViewModel.SearchJSON(SearchText).Result;

            if (searchResult == "404") // If the output of SearchJSON is "404", handle the error
                return new List<string>(); //Send an empty list (we can work on this)
            //Io qui aggiungerei una schermata di errore, anche solo con una scritta                    
            var searchResults = new List<string>(); //Declare the string searchResults

            var mangas =
                searchResult.Split(new[] { "},{" },
                    StringSplitOptions.RemoveEmptyEntries); //Split the search results into individual entries

            foreach (var manga in mangas) //for every entry
            {
                var nameIndex =
                    manga.IndexOf("\"name\":\""); //Find the index of the "name" property within the manga entry
                if (nameIndex != -1)
                {
                    var name = manga.Substring(nameIndex +
                                               "\"name\":\""
                                                   .Length); //Extract the substring starting from the "name" property
                    var endIndex = name.IndexOf("\""); //Find the end of the value by locating the next double quote
                    if (endIndex != -1)
                    {
                        var cleanedManga =
                            name.Substring(0,
                                endIndex); //Extract the value of the "name" property and remove any surrounding quotes
                        cleanedManga = cleanedManga.Replace("\"", ""); //Clean the quotation marks
                        searchResults.Add(cleanedManga); //Add the entry in searchResults
                    }
                }
            }

            SearchResults =
                searchResults; //SearchResults (the list declared at the start of the file) is equal to searchResults

            return searchResults;
        });
    }

    private void ClearSearchResults() //Clear search results
    {
        var clearedResults = new List<string>();
        SearchResults = clearedResults;
    }

    private async Task<string[]> SeriesJSONDownload(string permalink)
    {
        var baseUrl = $"https://dynasty-scans.com/series/{permalink}.json"; //Where is the manga JSON located
        string jsonResponse = null;
        using (var client = new HttpClient()) //Using an HttpClient
        {
            try
            {
                jsonResponse = await client.GetStringAsync(baseUrl);
                //Console.WriteLine(jsonResponse);
            }
            catch (HttpRequestException e)
            {
                ShowDialog(e.Message);
            }
        }

        var delimiter = ','; //Delimeter used to split the string later
        var splitLettore = jsonResponse.Split(delimiter);

        return splitLettore;
    }

    private async Task<int?> ChapterJSON(string permalink)
    {
        int? capitoli = null; //Declare the capitoli int
        string risultato = null;
        var splitLettore = await SeriesJSONDownload(permalink); //Call the downloaded JSON from SeriesJSONDownload

        while (string.IsNullOrEmpty(risultato))
            for (var i = splitLettore.GetLength(0) - 1; i >= 0; i--)
                if (splitLettore[i].IndexOf("\"permalink\":") >= 0 && splitLettore[i].IndexOf("_ch") >= 0)
                {
                    //Console.WriteLine(splitLettore[i]);
                    risultato = splitLettore[i];
                    break;
                }

        var parts = risultato.Split('_');
        var chapterPart = parts[parts.Length - 1]; //Get the last part after splitting

        var chapterNumber = chapterPart.Substring(2); //Remove the 'ch' shit
        var sillian = chapterNumber.Split('"');

        capitoli = int.Parse(sillian[0]);

        return capitoli;
    }

    private async Task<string> AuthorJSON(string permalink)
    {
        string author = null; //Declare author string
        var splitLettore = await SeriesJSONDownload(permalink); //Call the json download

        for (var i = 0;
             i < splitLettore.GetLength(0);
             i++) //For every item in the array [GetLenght(0) = how many item there are in 1 dimension, array]
            if (splitLettore[i].Contains("Author")) //If splitLettore[i] contains author
            {
                i++; //Select the next item
                author = splitLettore[i]; //We found our author name
                break; //stop searching
            }

        //Keep searching
        return author;
    }

    private ObservableCollection<int?>
        GenerateChapterNumbers(int? max) //Generate a number for each chapter, going from 1 to [i]
    {
        var chapters = new ObservableCollection<int?>();
        for (var i = 1; i <= max; i++) chapters.Add(i); // Add chapter number directly to the collection
        return chapters;
    }

    private void SelectAll()
    {
        foreach (var chapter in ChapterText) chapter.IsChecked = true; //Check all chapters
    }

    private void DeselectAll()
    {
        foreach (var chapter in ChapterText) chapter.IsChecked = false; //Uncheck all chapters
    }

    private async Task MakeDownload()
    {
        var selectedChapters = ChapterText.Where(chapter => chapter.IsChecked); //Select only checked chapters
        var chapterNumbers = selectedChapters.Select(chapter =>
        {
            var numericPart =
                Regex.Match(chapter.ChapterNumber, @"\d+")
                    .Value; //Just get the number from selectedchapters in an array
            return int.Parse(numericPart);
        }).ToArray();

        if (chapterNumbers.Length > 0) //If something is selected
        {
            var selectedItemText = SelectedItemText;
            var generatedPermalink =
                await MainWindowViewModel.linkGenerator(selectedItemText); //Generate the permalink (again)

            try //Set some stuff to prepare for download and call downloadfileasync
            {
                ShowOptions = false;
                DisplayText = "";
                ShowStatus = true;
                StatusBar = true;
                await DownloadFileAsync(selectedItemText, chapterNumbers, generatedPermalink);
            }
            catch (Exception ex) //if something goes wrong set stuff up for our error dialog
            {
                DownloadText = "";
                ShowStatus = false;
                StatusBar = false;
                ShowDialog(ex.Message);
            }
        }
        //Do nothing
    }

    public async Task DownloadFileAsync(string nomeManga, int[] capitoli, string linkparziale)
    {
        var pathUtente = DefaultPathManager.ReadDefaultPath(); //pathutente is the selected path from the settings
        var ciao = $"{nomeManga} Manga";
        var pathbella = $"{pathUtente}/{ciao}";
        var ciao2 = "Zip"; //Path to the Zip folder
        var path2 = $"{pathUtente}/{ciao}/{ciao2}"; //set directories up to prepare for download
        if (!Directory.Exists(pathbella)) //If the Manga folder doesn't exists
        {
            Directory.CreateDirectory(pathbella); //Make the folder
            DownloadText = $@"Creating directory {pathbella}...";
            if (!Directory.Exists(path2)) //If the Zip folder doens't exists
            {
                Directory.CreateDirectory(path2); //Make the folder
                DownloadText = $@"Creating directory {path2}...";
            }
            else //If it does exist
            {
                DownloadText = $@"Directory {path2} already exists";
            }
        }
        else //If the Manga folder exists
        {
            DownloadText = $@"Directory {pathbella} already exists";

            if (!Directory.Exists(path2)) //If the Zip folder doesn't exists
            {
                Directory.CreateDirectory(path2); //Make the folder
                DownloadText = $@"Creating directory {path2}...";
            }
            else
            {
                DownloadText = $@"Directory {path2} already exists";
            }
        }

        var fileUrlInizio = $"https://dynasty-scans.com/chapters/{linkparziale}_ch";
        const string fileUrlFine = "/download";

        var CreatoreUrl = new List<string>();
        Array.Sort(capitoli);
        //foreach (var capitolo in capitoli) //sostituito il ciclo for con un foreach, spero funzioni
        for (var capitolo = 0;
             capitolo < capitoli.GetLength(0);
             capitolo++) //get the correct download url keeping in mind the numbers
            if (capitoli[capitolo] < 10)
            {
                var numero = "0" + capitoli[capitolo];
                var urlFinito = fileUrlInizio + numero + fileUrlFine;
                CreatoreUrl.Add(urlFinito);
            }
            else
            {
                var urlFinito = fileUrlInizio + capitoli[capitolo] + fileUrlFine;
                CreatoreUrl.Add(urlFinito);
            }

        var fileUrl = CreatoreUrl.ToArray(); //create an array of download urls


        var cartellaManga = $@"{nomeManga} Manga/Zip/";
        using (var client = new WebClient()) //Creating a new WebClient
        {
            //foreach (var i in capitoli) //anche qui ho cabiamo in un foreach
            for (var i = 0; i < capitoli.GetLength(0); i++)
                try
                {
                    var path = pathUtente + $"/{nomeManga} Manga/Zip/Capitolo{capitoli[i]}.zip";
                    var fileName = $"Chapter {capitoli[i]}.zip"; //Creationg the file name
                    if (!File.Exists(path)) //If it doesn't I'll download it
                    {
                        DownloadText = $"Downloading {fileName}...";
                        var filePath =
                            Path.Combine(pathUtente, cartellaManga,
                                fileName); //Dovrebbe essere piu efficente e piu pulito

                        await client.DownloadFileTaskAsync(new Uri(fileUrl[i]), filePath); //Download the file
                    }
                    else
                    {
                        DownloadText = $"{fileName} already exists";
                    }
                }
                catch (Exception ex) //If any error happen
                {
                    var fileName = $"Chapter {capitoli[i]}.zip"; //File name
                    var filePath =
                        Path.Combine(pathUtente, cartellaManga, fileName); //Dovrebbe essere piu efficente e piu pulito

                    if (File.Exists(filePath)) //If the download fails and the file exists
                        File.Delete(filePath); //Delete the file since it's corrupted

                    throw;
                }
        }


        path2 = Path.Combine(pathUtente, cartellaManga);
        var zipFiles = Directory.GetFiles(path2, "*.zip");
        var extractionPath = new List<string>();
        DownloadText = "Extracting zip files...";
        foreach (var zipFile in zipFiles)
        {
            var depression =
                Path.Combine(path2,
                    Path.GetFileNameWithoutExtension(
                        zipFile)); //depression (the output path) is a folder having the same zip name
            Directory.CreateDirectory(depression); //create depression
            await Task.Run(() => ZipFile.ExtractToDirectory(zipFile, depression)); //extract files to depression

            File.Delete(zipFile); //delete files
            extractionPath.Add(depression); //add depression to the extraction paths
        }

        DownloadText = "Creating PDFs...";
        foreach (var path in extractionPath)
        {
            var outputPath = "";
            if (Directory.Exists(path))
            {
                var PallePNG =
                    Directory.GetFiles(path, "*.png", SearchOption.AllDirectories); //get an array of png files

                if (PallePNG.Length == 0) //if there are no png files, convert everything to png
                {
                    await ConvertPNG(path);
                    PallePNG = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
                    //goto PDFMaker;
                }
                //string[] PallePNG = maremmaMaiala.ToArray();

                if (PallePNG.Length > 0) //with converted pngs
                    try
                    {
                        var folderName = new DirectoryInfo(path).Name;
                        outputPath = Path.Combine(path, $"../../{folderName}.pdf"); //Set the pdf output path
                        Array.Sort(PallePNG); //Set the files in an array

                        using (var document = new PdfDocument()) //Create a pdf document starting from pngs
                        {
                            foreach (var pngFile in PallePNG)
                                try
                                {
                                    using (var fs = new FileStream(pngFile, FileMode.Open))
                                    {
                                        var page = document.AddPage();
                                        var gfx = XGraphics.FromPdfPage(page);
                                        var image = XImage.FromStream(fs);

                                        double width = page.Width; //Calculate the width and height of the image
                                        double height = page.Height;

                                        var scaleX =
                                            width / image
                                                .PixelWidth; //Calculate the scaling factors to fit the image within the page
                                        var scaleY = height / image.PixelHeight;
                                        var scale = Math.Min(scaleX, scaleY);

                                        gfx.DrawImage(image, 0, 0, width, height); //Draw the image onto the PDF page
                                    }
                                }
                                catch (Exception ex)
                                {
                                    DownloadText = "";
                                    ShowStatus = false;
                                    StatusBar = false;
                                }

                            document.Save(outputPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        DownloadText = "";
                        ShowStatus = false;
                        StatusBar = false;
                    }
            }
        }

        var infoFilePath = $"{pathbella}/info.config"; //Set the file path for the info file

        if (!File.Exists(infoFilePath)) //if doesn't exist
        {
            var selectedItemText = SelectedItemText;
            var author = await AuthorJSON(linkparziale);
            var cleanedAuthor = author.Replace("\"name\":", "").Replace("\"", ""); //Clean stuff up

            File.WriteAllText(infoFilePath,
                $"Name:{nomeManga},Author:{cleanedAuthor}"); //Create the file with name and author
            File.SetAttributes(infoFilePath,
                File.GetAttributes(infoFilePath) | FileAttributes.Hidden); //Make the file hidden
        }

        //Do nothing
        _mainWindowViewModel
            .PopulateLibrary(); //Call the method to populate the library and set stuff up for completion
        DownloadText = "";
        ShowStatus = false;
        StatusBar = false;
        OperationCompleted();
    }

    private static async Task ConvertPNG(string path)
    {
        try
        {
            // Array di estensioni di file immagine supportate
            string[] estensioniImmagine = { "*.jpg", "*.jpeg", "*.bmp" };

            // Cerca tutti i file immagine nella directory e nelle sue sottodirectory
            var fileImmagini = estensioniImmagine
                .SelectMany(ext => Directory.GetFiles(path, ext, SearchOption.AllDirectories)).ToArray();
            Array.Sort(fileImmagini);

            for (var i = 0; i < fileImmagini.Length; i++)
            {
                var outputDirectory = Path.Combine(path, "Converted");
                if (!Directory.Exists(outputDirectory)) // Se la cartella Converted non esiste
                    Directory.CreateDirectory(outputDirectory); // Creala

                var originalFileName =
                    Path.GetFileNameWithoutExtension(fileImmagini[i]); // Nome del file originale senza estensione
                var outputFileName = originalFileName + ".png"; // Nuovo nome del file con estensione .png
                var outputFilePath =
                    Path.Combine(outputDirectory, outputFileName); // Percorso completo del file di output

                using (var image = Image.Load(fileImmagini[i])) // Carica l'immagine dal percorso corrente
                {
                    image.Save(outputFilePath, new PngEncoder()); // Salva l'immagine come PNG nel percorso di output
                }

                var originalExtension = Path.GetExtension(fileImmagini[i]).ToLower();
                if (originalExtension == ".jpg" || originalExtension == ".jpeg" || originalExtension == ".bmp")
                    File.Delete(fileImmagini[i]);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante la conversione delle immagini: {ex.Message}");
        }
    }

    public void ShowDialog(string errorMessage) //Show ErrorWindow
    {
        var showDialog = new ErrorWindow(errorMessage);
        showDialog.Show();
    }

    private void OperationCompleted() //Show MessaggiamentoWindow 
    {
        var completed = new MessaggiamentoWindow();
        completed.Show();
    }

    public async void JSONStopwatch()
    {
        var ciao = new Stopwatch();
        ciao.Start(); //Start a stopwatch
        DownloadText = "Connection to the servers to fetch manga titles";
        StatusBar = true;
        await MainWindowViewModel.GetJSON(this); //Call GetJSON method in MainWindowViewModel
        ciao.Stop();
        DownloadText = $"Done! Elapsed time: {(int)(ciao.ElapsedMilliseconds * 0.001)} seconds";
        StatusBar = false;

        var tcs = new TaskCompletionSource<bool>();

        var blahaj = new Timer(2000); // Set the timer for 5000 milliseconds = 5 seconds
        blahaj.Elapsed += (sender, e) =>
        {
            blahaj.Stop();
            tcs.SetResult(true);
        };
        blahaj.Start();

        await tcs.Task;

        DownloadText = "";
    }
}