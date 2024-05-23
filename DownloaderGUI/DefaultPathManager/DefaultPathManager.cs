using System.IO;

public static class DefaultPathManager
{
    private static readonly string
        DefaultPathFile = "PathSettings.config"; //DefaultPathFile is a .txt file in where the path is stored

    public static string ReadDefaultPath()
    {
        if (!File.Exists(DefaultPathFile)) //if DefaultPathFile doesn't exist
        {
            var defaultPath =
                "/home/"; //create a new file with default path C:\ (for Windows), / (for linux and MacOS). Can change during developement
            WriteDefaultPath(defaultPath); //calls WriteDefaultPath
            return defaultPath; //returns defaultPath
        }

        return File.ReadAllText(DefaultPathFile); //if the file exists, read all data of the file
    }

    public static void WriteDefaultPath(string path)
    {
        File.WriteAllText(DefaultPathFile, path); //change all text with the output from the method that called it.
    }
}