using System.Text.RegularExpressions;
using EReaderGyldendalDictionaryCreator.Mapper.Entity;
using HtmlAgilityPack;

namespace EReaderGyldendalDictionaryCreator.Mapper
{
    internal class EntryMapper : IEntryMapper
    {
        private const string NullCharacter = "\0";
        
        public IEntry Map(int entryId, string text)
        {
            if (entryId == 329084)
            {
                // Fix a missing null in the "tatars" entry
                text = text.Replace("\atatars", $"{NullCharacter}atatars", StringComparison.Ordinal);
            }
            
            var (word, type) = ExtractWordAndType(text);


            var html = ExtractHtml(text);

            if (string.IsNullOrEmpty(html))
            {
                var (headword, inflected) = ParseInflection(text);

                return new Entry(entryId)
                {
                    PartOfSpeech = type,
                    Type = headword.Contains(' ') ? EntryType.IdiomInflection : EntryType.Inflection,
                    Headword = headword,
                    Words = new List<string> {inflected},
                    RawData = text
                };
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var entry = new Entry(entryId)
            {
                Headword = ParseH2(htmlDoc),
                PartOfSpeech = type,
                AlternativeWords = ParseAlternativeWords(htmlDoc)
            };

            var h3 = ParseH3(htmlDoc).Replace("®", string.Empty);
            entry.PrimaryWord = h3;

            var words = new List<string>(entry.AlternativeWords);
            words.Insert(0, h3);
            entry.Words = words.Distinct().ToList();

            entry.Type = h3.Contains(' ') ? EntryType.Idiom : EntryType.Standard;
            entry.RawData = Regex.Unescape(html);

            return entry;
        }

        private static (string word, string type) ExtractWordAndType(string text)
        {
            var wordStart = IndexOfOccurence(text, NullCharacter, 4) + NullCharacter.Length + 1;
            var wordEnd = text.IndexOf(NullCharacter, wordStart, StringComparison.Ordinal);

            var word = text.Substring(wordStart, wordEnd - wordStart)
                .Replace("  ", " ");

            word = Regex.Replace(word, @"\<.*?\>", string.Empty);

            var typeStart = wordEnd + NullCharacter.Length;
            var typeEnd = text.IndexOf(NullCharacter, typeStart, StringComparison.Ordinal);
            
            var type = text.Substring(typeStart, typeEnd - typeStart);
            
            return (word, string.IsNullOrEmpty(type) ? null : type);
        }

        private static (string headword, string inflected) ParseInflection(string text)
        {
            // Get the last one not in brackets!
            var cleanText = text.Replace("\0", "");

            if (cleanText.EndsWith(')'))
            {
                cleanText = text[..text.LastIndexOf('(')];
            }
            
            if (!cleanText.Contains("sb.") && !cleanText.Contains("vb."))
            {
                Console.WriteLine("why?");
            }

            // Every word ends with sb. or vb.
            var headword = ExtractValue(cleanText, " af ", "b.", true)[..^2];

            // Extract the inflected version
            var inflected =  ExtractReverseValue(text, " er ", NullCharacter);
            
            return (headword, inflected);
        }
        
        private static int IndexOfOccurence(string s, string match, int occurence)
        {
            var i = 1;
            var index = -1;

            while (i <= occurence && (index = s.IndexOf(match, index + 1, StringComparison.Ordinal)) != -1)
            {
                if (i == occurence)
                    return index;

                i++;
            }

            return -1;
        }

        private static string ExtractValue(string text, string start, string end, bool last = false)
        {
            var startPosition = last ? text.LastIndexOf(start, StringComparison.Ordinal) : text.IndexOf(start, StringComparison.Ordinal);

            if (startPosition < 0)
            {
                return null;
            }

            var endPosition = text.IndexOf(end, startPosition + start.Length, StringComparison.Ordinal);

            return endPosition < 0 ? null : text.Substring(startPosition + start.Length, endPosition - (startPosition + start.Length));
        }

        private static string ExtractReverseValue(string text, string end, string start)
        {
            var endPosition = text.LastIndexOf(end, StringComparison.Ordinal);

            if (endPosition < 0)
            {
                return null;
            }

            var startPosition = text.LastIndexOf(start, endPosition - end.Length, StringComparison.Ordinal);

            return startPosition < 0 ? null : text.Substring(startPosition + start.Length, endPosition - (startPosition + start.Length));
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

        private static string ExtractHtml(string text)
        {
            return ExtractReverseValue(text, NullCharacter, NullCharacter).Trim();
        }
    }
}
