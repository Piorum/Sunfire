using Sunfire.FSUtils.Models;

namespace Sunfire;

public class EntrySearcher(List<FSEntry> entries)
{
    private readonly List<FSEntry> _entries = entries;

    public FSEntry? GetBestMatch(string search)
    {
        if(string.IsNullOrEmpty(search)) return null;

        return _entries
            .Select(entry => new { Entry = entry, Score = ScoreMatch(entry.Name, search)})
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Name.Length) //Prefer shorter
            .Select(x => (FSEntry?)x.Entry)
            .FirstOrDefault();
    }
    private static int ScoreMatch(string text, string search)
    {
        if (text.Equals(search, StringComparison.OrdinalIgnoreCase)) return 4; //Exact
        if (text.StartsWith(search, StringComparison.OrdinalIgnoreCase)) return 3; //Starts With
        if (text.Contains(search, StringComparison.OrdinalIgnoreCase)) return 2; //Contains
        if (IsFuzzyMatch(text, search)) return 1; //Fuzzy
        
        return 0; //No Match
    }
    private static bool IsFuzzyMatch(string text, string search)
    {
        int searchIndex = 0;
        int searchLength = search.Length;

        foreach(char c in text)
            if (char.ToUpperInvariant(c) == char.ToUpperInvariant(search[searchIndex]))
            {
                searchIndex++;
                if(searchIndex == searchLength) return true;
            }

        return false;
    }
}
