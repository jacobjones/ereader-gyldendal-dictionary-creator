using System.Text.RegularExpressions;
using EReaderGyldendalDictionaryCreator.Mapper.Entity;
using HtmlAgilityPack;

namespace EReaderGyldendalDictionaryCreator.Mapper;

internal class EntryMapper : IEntryMapper
{
    public IEntry Map(int entryId, string text)
    {
        var html = ExtractHtml(text);

        if (string.IsNullOrEmpty(html))
        {
            return new Entry(entryId) { Type = EntryType.Partial, RawData = text };
        }

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var entry = new Entry(entryId)
        {
            Headword = ParseH2(htmlDoc),
            AlternativeWords = ParseAlternativeWords(htmlDoc)
        };

        var h3 = ParseH3(htmlDoc);
        entry.PrimaryWord = h3;

        var words = new List<string>(entry.AlternativeWords);
        words.Insert(0, h3);
        entry.Words = words.Distinct().ToList();

        entry.Type = h3.Contains(' ') ? EntryType.Idiom : EntryType.Standard;
        entry.RawData = Regex.Unescape(html);

        return entry;
    }

    private static IList<string> ParseAlternativeWords(HtmlDocument htmlDoc)
    {
        var words = htmlDoc.DocumentNode.SelectSingleNode("//h2/following-sibling::div//text()")?.InnerText;

        if (words == null)
        {
            return new List<string>();
        }

        var altWords = words.Trim('(', ')').Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).ToList();

        return NormalizeAlternativeWords(altWords);
    }
        
        
    private static IList<string> NormalizeAlternativeWords(IEnumerable<string> alternativeWords)
    {
        var filteredAltWords = alternativeWords.Where(x => !x.StartsWith('÷'));

        IList<string> finalAltWords = new List<string>();
            
        foreach (var altWord in filteredAltWords)
        {
            if (altWord.StartsWith("de ") || altWord.StartsWith("den "))
            {
                var words = altWord.Split(' ');
                finalAltWords.Add(words.Last());
                continue;
            }

            if (altWord.Contains(" / ") || altWord.Contains(" el. "))
            {
                var words = altWord.Split(' ');
                finalAltWords.Add(words.First());
                finalAltWords.Add(words.Last());
                continue;
            }
                
            finalAltWords.Add(altWord);
        }

        return finalAltWords;
    }

    private static string ParseH2(HtmlDocument htmlDoc)
    {
        return htmlDoc.DocumentNode.SelectSingleNode("//h2/text()")?.InnerText?.Trim();
    }

    private static string ParseH3(HtmlDocument htmlDoc)
    {
        var h3Node = htmlDoc.DocumentNode.SelectSingleNode("//h3");

        var fontNode = h3Node.ChildNodes.FirstOrDefault(x => x.OriginalName.Equals("font"));

        if (fontNode != null)
        {
            h3Node.ChildNodes.Remove(fontNode);
        }

        return h3Node.InnerText;
    }

    private static string? ExtractHtml(string text)
    {
        var end = text.LastIndexOf("\0", StringComparison.Ordinal);

        if (end < 0)
        {
            return null;
        }

        var start = text.LastIndexOf("\0", end - 1, StringComparison.Ordinal);

        return start < 0 ? null : text.Substring(start + 1, end - (start + 1)).Trim();
    }
}