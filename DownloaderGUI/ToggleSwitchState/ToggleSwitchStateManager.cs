using System.IO;

public static class ToggleSwitchStateManager
{
    private static readonly string FilePath = "toggleSwitchState.config";

    public static bool LoadState()
    {
        if (File.Exists(FilePath))
        {
            var content = File.ReadAllText(FilePath);
            if (bool.TryParse(content, out bool result))
            {
                return result;
            }
        }
        return true; // Default state
    }

    public static void SaveState(bool state)
    {
        File.WriteAllText(FilePath, state.ToString());
    }
}
