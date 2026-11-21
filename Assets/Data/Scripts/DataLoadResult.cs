namespace AngryKoala.Data
{
    internal enum DataLoadResult
    {
        NoFile,
        Current,
        Backup,
        Unreadable,
        DefaultsChanged
    }
}
