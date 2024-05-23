using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ReactiveUI;

namespace DownloaderGUI.ViewModels;

public class PdfFileInfo
{
    public string FileName { get; set; } // Name of the PDF file
    public string FilePath { get; set; } // Full path of the PDF file
}

public class Manga : ViewModelBase
{
    public string? Name { get; set; } //public property for the manga name
    public string? Author { get; set; } //public property for the author
    public string? Path { get; set; }

    public ObservableCollection<PdfFileInfo> PdfFiles { get; set; } = new();
}

public class LibraryViewModel : ViewModelBase
{
    private ObservableCollection<Manga>
        _filteredMangaLibrary; //create an observable collection with the filtered mangas from the query

    private string? _searchQuery; //private field for the search query
    public HashSet<string> mangaNames;

    public LibraryViewModel()
    {
        MangaLibrary = new ObservableCollection<Manga>();
        FilteredMangaLibrary = new ObservableCollection<Manga>(MangaLibrary); // Initialize FilteredMangaLibrary here
        mangaNames = new HashSet<string>(); //New hashset to make sure there arent two identical entries in the library
    }

    public string? SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                UpdateFilteredMangaLibrary();
                this.RaisePropertyChanged();
            }
        }
    }

    public ObservableCollection<Manga> FilteredMangaLibrary
    {
        get => _filteredMangaLibrary;
        set => this.RaiseAndSetIfChanged(ref _filteredMangaLibrary, value);
    }

    public ObservableCollection<Manga>
        MangaLibrary { get; } //Set an observable collection of mangas called "MangaLibrary" 

    public void UpdateFilteredMangaLibrary()
    {
        if (string.IsNullOrEmpty(SearchQuery)) //if the search query is empty
            // Show all mangas in alphabetical order of their names
            FilteredMangaLibrary = new ObservableCollection<Manga>(MangaLibrary.OrderBy(m => m.Name));
        else
            // Filter and sort the manga library based on the search query
            FilteredMangaLibrary = new ObservableCollection<Manga>(
                MangaLibrary.Where(m =>
                        m.Name?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                        m.Author?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true)
                    .OrderBy(m => m.Name)); // Sort the filtered result alphabetically by manga name

        // Notify the UI that the filtered manga library has been updated
        this.RaisePropertyChanged(nameof(FilteredMangaLibrary));
    }

    public void ShowPdfList(Manga manga, string path)
    {
        if (Directory.Exists(path))
        {
            if (manga.PdfFiles.Count == 0)
            {
                manga.PdfFiles.Clear(); // Clear the existing list of PDF files

                // Get all PDF files in the specified directory
                var pdfFiles = Directory.EnumerateFiles(path, "*.pdf")
                    .Select(filePath => new PdfFileInfo
                    {
                        FileName = Path.GetFileNameWithoutExtension(filePath),
                        FilePath = Path.Combine(path, filePath)
                    });

                // Add each PDF file info to the PdfFiles collection
                foreach (var pdf in pdfFiles) manga.PdfFiles.Add(pdf);

                // Sort the PdfFiles collection using the custom comparer
                var sortedPdfFiles = manga.PdfFiles.OrderBy(x => x.FileName, new ChapterComparer()).ToList();

                // Clear the PdfFiles collection and re-add sorted items
                manga.PdfFiles.Clear();
                foreach (var sortedPdf in sortedPdfFiles) manga.PdfFiles.Add(sortedPdf);
            }
            else
            {
                manga.PdfFiles.Clear();
            }
        }
    }

    public void OpenPdf(string filePath)
    {
        if (filePath != null && !string.IsNullOrEmpty(filePath))
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
    }

    public class ChapterComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            // Try parsing chapter names into integers
            if (int.TryParse(x.Replace("Chapter ", ""), out var chapterX) &&
                int.TryParse(y.Replace("Chapter ", ""), out var chapterY))
                // Compare integers numerically
                return chapterX.CompareTo(chapterY);
            // If parsing fails, fallback to string comparison
            return string.Compare(x, y);
        }
    }
}