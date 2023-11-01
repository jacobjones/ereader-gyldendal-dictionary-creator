using System.Text.RegularExpressions;
using EReaderGyldendalDictionaryCreator.Mapper.Entity;
using HtmlAgilityPack;

namespace EReaderGyldendalDictionaryCreator.Generator;

internal class KindleXmlGenerator : IOutputGenerator
{
    private const string WordPlaceholder = "{word}";
    private const string AlternativesPlaceholder = "{alternatives}";
    private const string DataPlaceholder = "{data}";
    private const string IdPlaceholder = "{id}";

    private readonly string _entryTemplate = $"<idx:entry scriptable=\"yes\" spell=\"yes\" id=\"{IdPlaceholder}\"><idx:orth value=\"{WordPlaceholder}\">{AlternativesPlaceholder}</idx:orth>{DataPlaceholder}</idx:entry><hr/>";
    private readonly string _alternativesTemplate = $"<idx:infl>{AlternativesPlaceholder}</idx:infl>";
    private readonly string _alternativeTemplate = $"<idx:iform value=\"{WordPlaceholder}\"/>";

    public string Generate(ICollection<IEntry> entries)
    {
        IList<string> xmls = new List<string>();

        foreach (var entry in entries)
        {
            var html = GetValidHtml(entry);
            html = RewriteLinks(html);
            html = RewriteInfoLinks(html);
            html = RewriteSearchLinks(html, entries);
            html = SimplifyLists(html);

            var alternatives = entry.AlternativeWords.Aggregate("", (current, next) => current + _alternativeTemplate.Replace(WordPlaceholder, next));

            if (!string.IsNullOrEmpty(alternatives))
            {
                alternatives = _alternativesTemplate.Replace(AlternativesPlaceholder, alternatives);
            }

            var listing = _entryTemplate
                .Replace(WordPlaceholder, entry.PrimaryWord)
                .Replace(AlternativesPlaceholder, alternatives)
                .Replace(DataPlaceholder, html)
                .Replace(IdPlaceholder, entry.Id.ToString());

            xmls.Add(listing);
        }

        // Rewrite the IDs
        //for (var i = 0; i < xmls.Count; i++)
        //{
        //    var id = i + 1;

        //    const string idPattern = @"<idx:entry scriptable=""yes"" spell=""yes"" id=""([0-9]+)""";

        //    var match = Regex.Match(xmls[i], idPattern);

        //    xmls[i] = Regex.Replace(xmls[i], idPattern, m => $"<idx:entry scriptable=\"yes\" spell=\"yes\" id=\"{id}\"");

        //    for (var x = 0; x < xmls.Count; x++)
        //    {
        //        xmls[x] = xmls[x].Replace($"id=\"{match.Groups[1].Value}\"", $"id=\"{id}\"");
        //    }
        //}

        return string.Join(Environment.NewLine, xmls);
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

    private static string SimplifyLists(string rawData)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(rawData);

        var changed = false;

        changed |= SimplifyLists(ref htmlDoc);
        changed |= UnwrapSpans(ref htmlDoc);
        changed |= ReduceDivs(ref htmlDoc);

        return changed ? htmlDoc.DocumentNode.OuterHtml : rawData;
    }

    private static bool SimplifyLists(ref HtmlDocument htmlDoc)
    {
        var liNodes = htmlDoc.DocumentNode.SelectNodes("//li");

        if (liNodes == null)
        {
            return false;
        }

        var changed = false;

        foreach (var node in liNodes)
        {
            if (!node.HasChildNodes)
            {
                continue;
            }

            // All the children need to be divs
            if (!node.ChildNodes.All(x => string.Equals(x.Name, "div", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Flatten divs inside list items (li)
            if (node.ChildNodes.Count == 1)
            {
                var divNode = node.FirstChild;

                if (divNode.ChildNodes != null && divNode.ChildNodes.Count == 1)
                {
                    if (node.FirstChild.FirstChild.NodeType == HtmlNodeType.Text)
                    {
                        node.RemoveChild(divNode, true);
                        changed = true;
                        continue;
                    }
                }

                if (divNode.ChildNodes != null && divNode.ChildNodes.Count == 2)
                {
                    var textNode = node.FirstChild.FirstChild;
                    var spanNode = node.FirstChild.ChildNodes[1];

                    if (textNode.NodeType == HtmlNodeType.Text && string.Equals(spanNode.Name, "span", StringComparison.OrdinalIgnoreCase))
                    {
                        node.RemoveAllChildren();
                        node.ChildNodes.Add(textNode);

                        foreach (var grandchildNode in spanNode.ChildNodes)
                        {
                            node.ChildNodes.Add(grandchildNode);
                        }

                        changed = true;
                        continue;
                    }
                }
            }

            if (node.ChildNodes.Count > 1)
            {
                var grandchildNodes = new List<HtmlNode>();

                foreach (var grandchildNode in node.ChildNodes.SelectMany(childNode => childNode.ChildNodes))
                {
                    if (!string.Equals(grandchildNode.Name, "span"))
                    {
                        grandchildNodes.Add(grandchildNode);
                        continue;
                    }

                    if (!grandchildNode.HasChildNodes)
                    {
                        grandchildNodes.Add(grandchildNode);
                        continue;
                    }

                    grandchildNodes.AddRange(grandchildNode.ChildNodes);
                }

                // Insert breaks between every node
                for (var i = grandchildNodes.Count - 1; i > 0; i--)
                {
                    if (string.Equals(grandchildNodes[i].Name, "font"))
                    {
                        continue; ;
                    }

                    grandchildNodes.Insert(i, htmlDoc.CreateElement("br"));
                }

                node.RemoveAllChildren();

                foreach (var grandchildNode in grandchildNodes)
                {
                    node.ChildNodes.Add(grandchildNode);
                }

                changed = true;
            }
        }

        return changed;
    }

    private static bool UnwrapSpans(ref HtmlDocument htmlDoc)
    {
        var spanNodes = htmlDoc.DocumentNode.SelectNodes("//span");

        if (spanNodes == null)
        {
            return false;
        }

        var changed = false;

        // Unwrap all remaining spans
        foreach (var node in spanNodes)
        {
            node.ParentNode.RemoveChild(node, true);
            changed = true;
        }

        return changed;
    }

    private static bool ReduceDivs(ref HtmlDocument htmlDoc)
    {
        var ulNodes = htmlDoc.DocumentNode.SelectNodes("//div//ul");

        if (ulNodes == null)
        {
            return false;
        }

        var changed = false;

        // Unwrap all remaining spans
        foreach (var node in ulNodes)
        {
            if (node.ParentNode.ChildNodes.Count > 1)
            {
                continue;
            }

            node.ParentNode.ParentNode.RemoveChild(node.ParentNode, true);
            changed = true;
        }

        return changed;
    }

    private static string RewriteLinks(string rawData)
    {
        const string pattern = @"href=""lookup:\/\/[0-9]+:([0-9]+)""";

        var replaced = Regex.Replace(rawData, pattern, m => $"id=\"{m.Groups[1].Value}\"");

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

        var replaced = Regex.Replace(rawData, pattern, m => $"id=\"{entry.Id}\"");

        return replaced;
    }
}