namespace PickupPoint;

// Operation journal - security team asked for it
public static class DeskJournal
{
    public static bool Enabled = true;

    public static void Write(string text)
    {
        if (!Enabled)
            return;
        try
        {
            string path = Path.Combine(Path.GetTempPath(), "pickup-point-journal.log");
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(path, stamp + " " + text + "\n");
        }
        catch { }
    }
}
