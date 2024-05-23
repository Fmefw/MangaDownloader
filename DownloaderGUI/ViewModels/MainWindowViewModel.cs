using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Path = System.IO.Path;

namespace DownloaderGUI.ViewModels;

public class MainWindowViewModel : ViewModelBase // Defining the view model class, inheriting from ViewModelBase
{
    private bool _isToggleSwitchChecked;
    private Control? _selectedTab; // Field to store the selected tab


    public MainWindowViewModel() // Constructor
    {
        SwitchTabCommand =
            ReactiveCommand.Create<SelectionChangedEventArgs>(SwitchTab); // Initializing the SwitchTabCommand
        PopulateLibrary();
        SearchViewModel = new SearchViewModel(this);
        SearchViewModel.JSONStopwatch();
        IsToggleSwitchChecked = ToggleSwitchStateManager.LoadState();

        this.WhenAnyValue(x => x.IsToggleSwitchChecked)
            .Skip(1) // Skip the initial value to avoid saving on ViewModel creation
            .Subscribe(ToggleSwitchStateManager.SaveState);
    }

    public SearchViewModel SearchViewModel { get; }
    public LibraryViewModel LibraryViewModel { get; } = new();

    public ReactiveCommand<SelectionChangedEventArgs, Unit> SwitchTabCommand { get; } // Reactive command to switch tabs

    public bool IsToggleSwitchChecked
    {
        get => _isToggleSwitchChecked;
        set => this.RaiseAndSetIfChanged(ref _isToggleSwitchChecked, value);
    }

    public Control? SelectedTab // Property to get or set the selected tab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    private void SwitchTab(SelectionChangedEventArgs args) // Method to handle tab switching
    {
        //
    }

    private static async Task<int> GetTotalPages(string jsonString)
    {
        var url = "https://dynasty-scans.com/series.json"; //URL where the JSON is located

        using (var client = new HttpClient()) //HTTP request to get the JSON
        {
            var response = await client.GetAsync(url); //Response is the JSON we're trying to download
            if (response.IsSuccessStatusCode) //If response is successful
            {
                jsonString = await response.Content.ReadAsStringAsync(); //Turning the JSON as a JSON String
                var jsonObject =
                    JObject.Parse(jsonString); //Analyzing the JSON String (I dont really know how this works)
                var currentPage = (int)jsonObject["current_page"]; //Getting the current page
                var totalPages = (int)jsonObject["total_pages"]; //Getting the last page
                return totalPages; //Return of the last page = how many pages there are in the JSON
            }

            //If anything goes wrong 
            Console.WriteLine("Error :(");
            return 0;
        }
    }

    private static async Task SaveJsonToFile(string jsonContent, string filePath)
    {
        await File.WriteAllTextAsync(filePath, jsonContent); //Saving the JSON on a file
    }

    public static async Task GetJSON(SearchViewModel searchViewModel)
    {
        var baseUrl = "https://dynasty-scans.com/series.json"; //Where are all the json located

        using (var client = new HttpClient()) //Using an HttpClient
        {
            try
            {
                var response = await client.GetAsync(baseUrl); //Downloading the file
                if (response.IsSuccessStatusCode) //If download went well
                {
                    var jsonString = await response.Content.ReadAsStringAsync(); //Reading the downloaded file
                    var totalPages = await GetTotalPages(jsonString); //Getting the numbers of pages

                    var directoryPath = @"./JSON"; //Path where JSON files are saved 
                    Directory.CreateDirectory(directoryPath); //Create the directory

                    for (var currentPage = 1;
                         currentPage <= totalPages;
                         currentPage++) //For every page in the JSON, download the file
                    {
                        var pageUrl = $"{baseUrl}?page={currentPage}"; //Getting the page URL
                        var jsonFileName =
                            $"pag{currentPage}.json"; //Creating the file name as pag[number of the page].json
                        var jsonFilePath = Path.Combine(directoryPath, jsonFileName); //Creating the path to the file

                        if (File.Exists(jsonFilePath)) //Check if the file already exists
                        {
                            var lastModified =
                                File.GetLastWriteTime(
                                    jsonFilePath); //Create a datetime to check when the file was created
                            if ((DateTime.Now - lastModified).TotalDays < 1) //If it's less than 1 day
                                continue; //Skip the download because the file is still usable
                        }

                        var pageResponse = await client.GetAsync(pageUrl); //Trying to save the file
                        if (pageResponse.IsSuccessStatusCode) //If successful
                        {
                            var jsonContent =
                                await pageResponse.Content.ReadAsStringAsync(); //Reading the downloaded JSON
                            await SaveJsonToFile(jsonContent, jsonFilePath); //Saving the JSON to his page file
                        }
                        //If anything goes wrong
                        //
                    }
                }
                //If anything goes REALLY wrong
                //
            }
            catch (Exception ex)
            {
                searchViewModel.ShowDialog("No internet connection or the servers may be offline");
            }
        }
    }

    public static async Task<string> SearchJSON(string? searchName)
    {
        var folderPath = @".\JSON"; //Path where the JSON files arestored
        var numeroFiles = CountFile(folderPath); //Figuring out how many files there are in the JSON folder
        string lettoreJSON = null; //Creating the variable lettereJSON
        for (var i = 1; i <= numeroFiles; i++) //Iterate through all files inclusive of numeroFiles
        {
            var file = Path.Combine(folderPath,
                $"pag{i}.json"); //Creating the path to the pag.json file determinated by the for cycle
            try
            {
                lettoreJSON +=
                    await File.ReadAllTextAsync(file); //Adding to lettereJSON the new content, keeping the old one
            }
            catch //If anything oges wrong
            {
                //Everything's fucked
            }
        }

        var delimiter = ','; //Delimeter used to split the string later
        var splitLettore =
            lettoreJSON.Split(delimiter); //Splitting lettereJSON in an array every time the delimiter is found
        string permalink = null; //Creating permalink and setting it as null


        var matchingEntries = new List<string>(); //Declare the matchingEntries list

        for (var i = 0;
             i < splitLettore.GetLength(0);
             i++) //For every item in the array [GetLenght(0) = how many item there are in 1 dimension, array]
            if (searchName != null)
                if (splitLettore[i].ToLower()
                    .Contains(searchName.ToLower())) //If splitLettore[i] contains the name we're searching
                    matchingEntries.Add(splitLettore[i]); //add the entries found to matching entries list
        if (matchingEntries.Count > 0) //if there's at least a matching entry
            return string.Join(",", matchingEntries); //return the list of all entries
        return "404";
    }

    private static int CountFile(string cartellaPath)
    {
        try //Trying to get all the files in a directory
        {
            var files = Directory.GetFiles(
                cartellaPath); //Getting the names of the files in the Directory given by cartellaPath
            var numeroFile = files.Length; //Counting how many files there are
            return numeroFile; //Return how many files there are in a folder
        }
        catch (Exception ex) //If anything goes wrong
        {
            Console.WriteLine($"There was a problem while counting files: {ex.Message}");
            return 0; //Return 0
        }
    }

    public static async Task<string> linkGenerator(string nome)
    {
        var folderPath = @".\JSON"; //Path where the JSON files arestored

        //Teoricamente tutte le folder devono essere cambiate altrimenti si rimane sempre nella cartella del programma

        var numeroFiles = CountFile(folderPath); //Figuring out how many files there are in the JSON folder
        string lettoreJSON = null; //Creating the variable lettereJSON
        for (var i = 1; i <= numeroFiles; i++) //Iterate through all files inclusive of numeroFiles
        {
            var file = Path.Combine(folderPath,
                $"pag{i}.json"); //Creating the path to the pag.json file determinated by the for cycle
            try
            {
                lettoreJSON +=
                    await File.ReadAllTextAsync(file); //Adding to lettereJSON the new content, keeping the old one
            }
            catch (Exception ex) //If anything oges wrong
            {
                Console.WriteLine($"Error processing file {file}: {ex.Message}"); //Everything's fucked
            }
        }

        var delimiter = ','; //Delimeter used to split the string later
        var splitLettore =
            lettoreJSON.Split(delimiter); //Splitting lettereJSON in an array every time the delimiter is found
        string permalink = null; //Creating permalink and setting it as null

        for (var i = 0;
             i < splitLettore.GetLength(0);
             i++) //For every item in the array [GetLenght(0) = how many item there are in 1 dimension, array]
            if (splitLettore[i].Contains(nome)) //If splitLettore[i] contains the name we're searching
            {
                i++; //Add 1 to i
                permalink = splitLettore[i]; //permalink is equal to the next element
                //return permalink; //Return permalink and exit the method
            }

        //Keep searching
        if (permalink != "404") //If permalink not = "404"
        {
            var startIndex = permalink.IndexOf(':') + 2; //Search the index after ':'
            var endIndex = permalink.LastIndexOf('"'); //Find the last part of the index before `"`
            var output =
                permalink.Substring(startIndex, endIndex - startIndex); //Output is created subtrating useless stuff
            return output; //Return output
        }

        return "errore"; //Error occured and return error
    }

    public void PopulateLibrary() //Method to populate the library
    {
        var downloadPath = DefaultPathManager.ReadDefaultPath(); //Get the download path
        var mangaPaths = Directory.GetDirectories(downloadPath, "*Manga"); //Look for every folder that ends in Manga
        var configFileFound = false; //Declare a bool to handle empty folders

        foreach (var path in mangaPaths) //For each folder
        {
            var configFile = Path.Combine(path, "info.config"); //Look for info.config file

            if (File.Exists(configFile))
            {
                configFileFound = true; //If a config file is found, declare the bool true
                var configContent = File.ReadAllText(configFile); //Read the content of the file
                var entries = configContent.Split(','); //Split the entries with a comma
                var powerBook = new Manga(); //Declare new entry in observablecollection manga
                foreach (var entry in entries) //Set name and author
                {
                    var keyValue = entry.Split(':');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim();
                        var value = keyValue[1].Trim();

                        if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                            powerBook.Name = value;
                        else if (key.Equals("Author", StringComparison.OrdinalIgnoreCase)) powerBook.Author = value;
                    }

                    powerBook.Path = path;
                }

                if (!string.IsNullOrEmpty(powerBook.Name) && !string.IsNullOrEmpty(powerBook.Author) &&
                    !string.IsNullOrEmpty(powerBook
                        .Path) && //If name, author arent empty and the name doesnt exist already
                    LibraryViewModel.mangaNames.Add(powerBook.Name))
                    LibraryViewModel.MangaLibrary.Add(powerBook); //Add new entry to the library
            }
        }

        if (!configFileFound) //if no config file is found
        {
            LibraryViewModel.MangaLibrary.Clear(); //clear the Library
            LibraryViewModel.mangaNames
                .Clear(); //Clear the hashset in case another folder may get updated in the future
        }

        LibraryViewModel.UpdateFilteredMangaLibrary(); //Update the library
    }
}