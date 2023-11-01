using System.Text;
using System.Text.RegularExpressions;
using EReaderGyldendalDictionaryCreator.Generator.Entity;
using EReaderGyldendalDictionaryCreator.Mapper.Entity;

namespace EReaderGyldendalDictionaryCreator.Generator;

internal class StarDictBabylonGenerator : IOutputGenerator
{
    private const string EntrySeparator = "<hr>";
    private const bool SupportSynonyms = false;
        
    public string Generate(ICollection<IEntry> entries)
    {
        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine("#bookname=Gyldendals Røde Ordbøger: Dansk-Engelsk");
        sb.AppendLine("#author=Gyldendals");
        sb.AppendLine("#stripmethod=keep");
        sb.AppendLine("#sametypesequence=h");
        sb.AppendLine();
            
        var starDictEntries = GetStarDictEntries(new List<IEntry>(entries), SupportSynonyms);
            
        foreach (var starDictEntry in starDictEntries)
        {
            var html = GetValidHtml(starDictEntry.Entries);
            html = RewriteLinks(html, entries);
            html = RewriteInfoLinks(html);
            html = RewriteSearchLinks(html, entries);
            html = ReduceHeadings(html);

            sb.AppendLine($"{string.Join('|', starDictEntry.Words)}");
            sb.AppendLine(html);
            sb.AppendLine();
        }

        return sb.ToString();
    }
        
    private static IEnumerable<StarDictEntry> GetStarDictEntries(ICollection<IEntry> entries, bool supportSynonyms)
    {
        var starDictEntries = new List<StarDictEntry>();

        var wordEntries = entries
            .SelectMany(e => e.Words.Select(w => new { Word = w, Entry = e }))
            .GroupBy(x => x.Word, x => x.Entry)
            .ToDictionary(x => x.Key, x => x.ToList());

        while (entries.Any())
        {
            var entry = entries.First();

            var pairedEntries = new List<IEntry> {entry};
                
            foreach (var word in entry.Words)
            {
                if (wordEntries.TryGetValue(word, out var items))
                {
                    pairedEntries.AddRange(items);
                }
            }

            pairedEntries = pairedEntries.Distinct().ToList();

            foreach (var sameEntry in pairedEntries)
            {
                entries.Remove(sameEntry);
            }
                
            if (pairedEntries.Count == 1)
            {
                starDictEntries.Add(new StarDictEntry(entry.Words, new List<IEntry> {entry}));
                continue;
            }

            var headWords = pairedEntries.Select(x => x.Headword).Distinct().ToList();

            if (headWords.Count == 1)
            {
                var primaryEntries = pairedEntries.Where(x => x.PrimaryWord == headWords.Single()).ToList();

                if (primaryEntries.Count == 1)
                {
                    var primaryEntry = primaryEntries.Single();

                    var secondaryEntries = pairedEntries.Where(x => x.Id != primaryEntry.Id).ToList();

                    // Remove some redundant plurals
                    foreach (var secondaryEntry in secondaryEntries.Where(secondaryEntry => !secondaryEntry.Words.Except(primaryEntry.Words).Any()).Where(secondaryEntry => IsPlural(secondaryEntry) && IsLink(secondaryEntry)))
                    {
                        pairedEntries.Remove(secondaryEntry);
                    }
                }
            }
                
            var sameGroups = pairedEntries
                .SelectMany(e => e.Words.Select(w => new { Word = w, Entry = e }))
                .GroupBy(x => x.Word, x => x.Entry)
                .Select(x => new { Ids = string.Join("|", x.Select(g => g.Id).OrderBy(g => g)), Word = x.Key, Entry = x.ToList() })
                .GroupBy(x => x.Ids)
                .ToList();

            starDictEntries.AddRange(sameGroups.Select(sameGroup =>
                new StarDictEntry(sameGroup.Select(x => x.Word).ToList(),
                    sameGroup.SelectMany(x => x.Entry).Distinct().ToList())));
        }

        if (supportSynonyms)
        {
            return starDictEntries;
        }

        // If we don't support synonyms we need to duplicate everything per word
        var singleWordStarDictEntries = new List<StarDictEntry>();
            
        foreach (var entry in starDictEntries)
        {
            singleWordStarDictEntries.AddRange(entry.Words.Select(word => new StarDictEntry(new List<string> {word}, entry.Entries)));
        }

        return singleWordStarDictEntries;
    }

    private static bool IsPlural(IEntry entry)
    {
        return entry.RawData.Contains("<i>pl. af sb.</i></font></h3>");
    }
        
    private static bool IsLink(IEntry entry)
    {
        return entry.RawData.Contains("se <a href=\"lookup");
    }
        
    private static string GetValidHtml(IEnumerable<IEntry> entries)
    {
        return string.Join(EntrySeparator, entries.Select(GetValidHtml));
    }
        
    private static string GetValidHtml(IEntry entry)
    {
        var html = entry.RawData;

        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        // Problem with several entries
        if (html.Contains("<div/>", StringComparison.Ordinal))
        {
            html = html.Replace("<div/>", "<div>", StringComparison.Ordinal);
        }

        // The following have specific formatting issues
        switch (entry.Id)
        {
            // fremmed
            case 23166:
                return $"{html.Remove(html.Length - 11, 11)}</li></ol></li></ol>";
            // naturen
            case 51857:
                return $"{html.Remove(html.Length - 11, 11)}</li></ol>";
            // prædikat
            case 60775:
                return $"{html.Remove(html.Length - 11, 11)}</li></ol></li></ol>";
            // udestående
            case 87557:
                return $"{html}</li></ol></li></ol>";
            // vantro
            case 91348:
                return $"{html.Remove(html.Length - 11, 11)}</li></ol></li></ol>";
            default:
                return html;
        }
    }

    private static string ReduceHeadings(string rawData)
    {
        for (var i = 10; i > 0; i--)
        {
            rawData = rawData.Replace($"<h{i}>", $"<h{i+1} style=\"margin:0\">");
            rawData = rawData.Replace($"</h{i}>", $"</h{i+1}>");
        }

        return rawData;
    }

    private static string RewriteLinks(string rawData, ICollection<IEntry> entries)
    {
        const string pattern = @"href=""lookup:\/\/[0-9]+:([0-9]+)"">([^<]+)<";
            
        var match = Regex.Match(rawData, pattern);
            
        if (!match.Success)
        {
            return rawData;
        }

        var id = int.Parse(match.Groups[1].Value);
        var word = match.Groups[2].Value;

        var entry = entries.FirstOrDefault(x => x.Id == id);

        var replacement = entry?.Headword ?? word;

        const string replacePattern = @"href=""lookup:\/\/[0-9]+:([0-9]+)""";
            
        var replaced = Regex.Replace(rawData, replacePattern, m => $"href=\"bword://{replacement}\"");

        return replaced;
    }
        
    private static string RewriteInfoLinks(string rawData)
    {
        const string pattern = @"<a href=""expand:\/\/(((?!INFO).)+)"">\[INFO\]<\/a>";

        var replaced = Regex.Replace(rawData, pattern, m => $"({m.Groups[1].Value})");

        return replaced;
    }
        
    private static string RewriteSearchLinks(string rawData, ICollection<IEntry> entries)
    {
        const string pattern = @"href=""search:\/\/[0-9]+:(.+)""";

        var match = Regex.Match(rawData, pattern);

        if (!match.Success)
        {
            return rawData;
        }

        var word = match.Groups[1].Value;

        var entry = entries.FirstOrDefault(x =>
            string.Equals(x.Headword, word, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.PrimaryWord, word, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            entry = entries.FirstOrDefault(x =>
                x.AlternativeWords.Any(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)));
        }

        if (entry == null)
        {
            // Remove the link
            const string fullPattern = @"<a href=""search:\/\/[0-9]+:((?!"").)+"">(((?!\/a).)+)<\/a>";

            return Regex.Replace(rawData, fullPattern, m => m.Groups[2].Value);
        }

        var replaced = Regex.Replace(rawData, pattern, m => $"href=\"bword://{entry.Headword}\"");

        return replaced;
    }
}