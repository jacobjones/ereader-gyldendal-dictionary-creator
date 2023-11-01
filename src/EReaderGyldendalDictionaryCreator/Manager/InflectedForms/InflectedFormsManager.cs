using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EReaderGyldendalDictionaryCreator.Manager.InflectedForms.Entity;
using EReaderGyldendalDictionaryCreator.Mapper.Entity;

namespace EReaderGyldendalDictionaryCreator.Manager.InflectedForms;

// This requires the Full-form list for "Den Danske Ordbog" available here:
// https://korpus.dsl.dk/resources/details/ddo-fullforms.html
internal class InflectedFormsManager : IInflectedFormsManager
{
    private const string Path = "data";
    private const string SearchPattern = "ddo_fullforms_*.csv";

    private IDictionary<string, ICollection<string>>? _inflectedFormEntries;
    private IDictionary<string, ICollection<string>> InflectedFormEntries
    {
        get
        {
            if (_inflectedFormEntries != null)
            {
                return _inflectedFormEntries;
            }

            _inflectedFormEntries = Filter(LoadAll()).GroupBy(x => x.Headword.ToLowerInvariant())
                .ToDictionary(x => x.Key, x => (ICollection<string>)x.Select(i => i.InflectedForm).ToList(), StringComparer.OrdinalIgnoreCase);

            return _inflectedFormEntries;
        }
    }

    public ICollection<string> Get(string headword)
    {
        return InflectedFormEntries.TryGetValue(headword, out var inflectedFormEntries) ? inflectedFormEntries : new List<string>();
    }

    public void AddInflectedForms(ICollection<IEntry> entries)
    {
        var total = entries.Count;
        var count = 0;
        var stringFormat = $"D{total.ToString().Length}";
            
        foreach (var entry in entries)
        {
            count++;
                
            if (string.IsNullOrEmpty(entry.Headword))
            {
                continue;
            }
                
            if (entry.Type != EntryType.Standard)
            {
                continue;
            }
                
            Console.WriteLine($"[{count.ToString(stringFormat)}/{total}] ({entry.Id}) Checking word {entry.Headword}");

            var inflectedForms = Get(entry.Headword);

            if (!inflectedForms.Any())
            {
                continue;
            }

            IEntry lastAltEntry = null;

            foreach (var inflectedForm in inflectedForms)
            {
                if (entry.Words.Contains(inflectedForm, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                    
                if (entry.AlternativeWords.Contains(inflectedForm, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (lastAltEntry != null)
                {
                    // Allows us to quickly check if the last alt entry already contains it
                    if (lastAltEntry.Words?.Contains(inflectedForm, StringComparer.OrdinalIgnoreCase) ?? false)
                    {
                        continue;
                    }
                }
                    
                var altEntry = entries.FirstOrDefault(x =>
                    x.Words?.Contains(inflectedForm, StringComparer.OrdinalIgnoreCase) ?? false);

                lastAltEntry = altEntry;
                    
                // Another entry already exists for this word
                if (altEntry != null)
                {
                    continue;
                }

                Console.WriteLine($"[{count.ToString(stringFormat)}/{total}] ({entry.Id}) Adding inflected form for word {entry.Headword}: {inflectedForm}");
                entry.Words.Add(inflectedForm);
            }
        }
    }

    private static IEnumerable<IInflectedFormEntry> Filter(IEnumerable<IInflectedFormEntry> inflectedFormEntries)
    {
        return inflectedFormEntries
            // No need to include the actual headword
            .Where(x => !string.Equals(x.Headword, x.InflectedForm, StringComparison.OrdinalIgnoreCase))
            // These introduce a lot of noise
            .Where(x => !x.InflectedForm.EndsWith('s'))
            .Where(x => !x.InflectedForm.EndsWith('\''));
    }
        
    private static ICollection<IInflectedFormEntry> LoadAll()
    {
        var fileName = Directory.GetFiles(Path, SearchPattern).MaxBy(x => x);

        if (string.IsNullOrEmpty(fileName))
        {
            return new List<IInflectedFormEntry>();
        }

        using var reader = new StreamReader(fileName);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "\t" });

        return csv.GetRecords<InflectedFormEntry>().OfType<IInflectedFormEntry>().ToList();
    }
}