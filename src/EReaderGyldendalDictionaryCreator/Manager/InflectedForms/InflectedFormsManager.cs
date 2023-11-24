using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EReaderGyldendalDictionaryCreator.Manager.InflectedForms.Entity;
using EReaderGyldendalDictionaryCreator.Mapper.Entity;

namespace EReaderGyldendalDictionaryCreator.Manager.InflectedForms
{
    // This requires the Full-form list for "Den Danske Ordbog" available here:
    // https://korpus.dsl.dk/resources/details/ddo-fullforms.html
    
    internal class InflectedFormsManager : IInflectedFormsManager
    {
        private const string Path = "data";
        private const string SearchPattern = "ddo_fullforms_*.csv";

        private IDictionary<string, ICollection<IInflectedFormEntry>> _inflectedFormEntries;
        private IDictionary<string, ICollection<IInflectedFormEntry>> InflectedFormEntries
        {
            get
            {
                if (_inflectedFormEntries != null)
                {
                    return _inflectedFormEntries;
                }
                
                _inflectedFormEntries = Filter(LoadAll()).GroupBy(x => x.Headword.ToLowerInvariant())
                    .ToDictionary(x => x.Key, x => (ICollection<IInflectedFormEntry>)x.Select(i => i).ToList());

                return _inflectedFormEntries;
            }
        }

        public ICollection<IInflectedFormEntry> Get(string headword)
        {
            return InflectedFormEntries.TryGetValue(headword, out var inflectedFormEntries) ? inflectedFormEntries : new List<IInflectedFormEntry>();
        }

        public ICollection<IInflectedFormEntry> Get(string headword, string partOfSpeech)
        {
            return Get(headword).Where(x => string.Equals(x.PartOfSpeech, partOfSpeech, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public void MergeInflectedForms(ICollection<IEntry> entries, IDictionary<string, List<IEntry>> inflectedEntries)
        {
            var total = inflectedEntries.Count;
            var count = 0;
            var stringFormat = $"D{total.ToString().Length}";
            
            foreach (var inflectedEntry in inflectedEntries)
            {
                count++;

                var wordTypes = inflectedEntry.Value.GroupBy(x => x.PartOfSpeech).ToList();

                foreach (var wordType in wordTypes)
                {
                    var matchingEntries = entries.Where(x =>
                        string.Equals(x.Headword, inflectedEntry.Key, StringComparison.OrdinalIgnoreCase) && x.PartOfSpeech != null && x.PartOfSpeech.Contains(wordType.Key, StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    if (matchingEntries.Count == 0)
                    {
                        continue;
                    }

                    foreach (var matchingEntry in matchingEntries)
                    {
                        var addedInflections = new List<string>();

                        foreach (var inflection in inflectedEntry.Value.SelectMany(x => x.Words))
                        {
                            if (matchingEntry.Words.Contains(inflection, StringComparer.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                    
                            matchingEntry.Words.Add(inflection);
                    
                            // Used for tracking
                            addedInflections.Add(inflection);
                        }
                
                        Console.WriteLine($"[{count.ToString(stringFormat)}/{total}] ({matchingEntry.Id}) Adding inflected forms for word {matchingEntry.Headword}: {string.Join(", ", addedInflections)}");
                    }
                }
            }
        }

        public void AddInflectedForms(ICollection<IEntry> entries)
        {
            // Nouns, verbs, adjectives, adverbs and pronouns
            // "sb.", "vb.", "adj.", "adv.", "pron."
            //var validPartsOfSpeech = new[] {"sb.", "vb.", "adj.", "adv.", "pron."};

            var validPartsOfSpeech = LoadAll().Select(x => x.PartOfSpeech).Where(x => !string.IsNullOrEmpty(x))
                .Distinct().ToList();

            var filteredEntries = entries.Where(x => validPartsOfSpeech.Contains(x.PartOfSpeech ?? "")).ToList();

            var total = filteredEntries.Count;
            var count = 0;
            var stringFormat = $"D{total.ToString().Length}";

            foreach (var entry in filteredEntries)
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

                var inflectedForms = Get(entry.Headword, entry.PartOfSpeech);
                
                if (!inflectedForms.Any())
                {
                    continue;
                }

                IEntry lastAltEntry = null;

                var addedInflections = new List<string>();
                
                foreach (var inflectedForm in inflectedForms.Select(x => x.InflectedForm))
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

                    // Used for tracking
                    addedInflections.Add(inflectedForm);
                    
                    entry.Words.Add(inflectedForm);
                }

                if (addedInflections.Any())
                {
                    Console.WriteLine($"[{count.ToString(stringFormat)}/{total}] ({entry.Id}) Adding inflected forms for word {entry.Headword}: {string.Join(", ", addedInflections)}");
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
            var fileName = Directory.GetFiles(Path, SearchPattern).OrderByDescending(x => x).FirstOrDefault();

            if (string.IsNullOrEmpty(fileName))
            {
                return new List<IInflectedFormEntry>();
            }

            using var reader = new StreamReader(fileName);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "\t" });

            return csv.GetRecords<InflectedFormEntry>().OfType<IInflectedFormEntry>().ToList();
        }
    }
}