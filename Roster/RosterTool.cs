using System.Windows;

namespace Madden26Plugin.Roster;

public static class RosterTool
{
    public static void OpenRosterEditor(Window owner = null)
    {
        var window = new Views.RosterEditorWindow();

        if (owner != null)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        window.Show();
    }

    public static void OpenRosterEditorWithFile(string filePath, Window owner = null)
    {
        var window = new Views.RosterEditorWindow();

        if (owner != null)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        window.LoadRoster(filePath);
        window.Show();
    }
}
