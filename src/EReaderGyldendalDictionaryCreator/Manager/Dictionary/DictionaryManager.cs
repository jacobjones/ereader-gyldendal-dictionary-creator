using EReaderGyldendalDictionaryCreator.Connector.Dictionary;
using EReaderGyldendalDictionaryCreator.Connector.Vector;
using EReaderGyldendalDictionaryCreator.Connector.Vector.Entity;
using EReaderGyldendalDictionaryCreator.Connector.Vector.Property;
using EReaderGyldendalDictionaryCreator.Manager.Dictionary.Property;
using EReaderGyldendalDictionaryCreator.Parser;

namespace EReaderGyldendalDictionaryCreator.Manager.Dictionary;

internal class DictionaryManager : IDictionaryManager
{
    private readonly IDictionaryVectorConnector _vectorConnector;
    private readonly IDictionaryConnector _dictionaryConnector;
    private readonly IGroParser _groParser;

    private static readonly IDictionary<SearchType, string> SearchTypes = new Dictionary<SearchType, string>
    {
        { SearchType.Lookup, Table.Lookup },
        { SearchType.Reverse, Table.Reverse},
        { SearchType.CollocationLookup, Table.CollocationLookup }
    };

    public DictionaryManager(IDictionaryVectorConnector vectorConnector, IDictionaryConnector dictionaryConnector, IGroParser groParser)
    {
        _vectorConnector = vectorConnector;
        _dictionaryConnector = dictionaryConnector;
        _groParser = groParser;
    }

    public int GetEntriesCount(LookupDirection direction)
    {
        return _vectorConnector.GetEntryCount(direction);
    }

    public ICollection<(int entryId, string text)> GetEntries(LookupDirection direction, int skip, int count)
    {
        var vectors = _vectorConnector.GetVectors(direction, skip, count);

        var entries = _dictionaryConnector.GetEntries(vectors);

        IList<(int entryId, string text)> collection = new List<(int entryId, string text)>();

        foreach (var (entryId, data) in entries)
        {
            var text = _groParser.ParseEntry(entryId, data);

            collection.Add((entryId, text));
        }

        return collection;
    }

    public ICollection<(int entryId, string text)> Search(LookupDirection direction, SearchType type, string query, bool followLinks = false)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("query is required.", nameof(query));
        }

        var formattedQuery = query.Replace("*", "%").Replace("'", "''").ToLowerInvariant();

        var terms = formattedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        // If we have more than one term it needs be a CollocationLookup
        if (terms.Count > 1 && type != SearchType.CollocationLookup)
        {
            return new List<(int entryId, string text)>();
        }

        var entryIds = new List<int>();

        entryIds.AddRange(terms.Count > 1
            ? _vectorConnector.Search(direction, terms)
            : _vectorConnector.Search(direction, SearchTypes[type], terms.First()));

        var vectors = _vectorConnector.GetVectors(direction, entryIds.Distinct().ToList());

        if (followLinks)
        {
            for (var i = 0; i < vectors.Count; i++)
            {
                var vector = vectors[i];

                if (vector.LinkId == vector.EntryId)
                {
                    continue;
                }

                var linkedVector = _vectorConnector.GetVector(direction, vector.LinkId);

                vectors[i] = linkedVector;
            }
        }

        var entries = _dictionaryConnector.GetEntries(vectors.GroupBy(x => x.EntryId).Select(x => x.First()).ToList());

        IList<(int entryId, string text)> collection = new List<(int entryId, string text)>();

        foreach (var (entryId, data) in entries)
        {
            var text = _groParser.ParseEntry(entryId, data);

            collection.Add((entryId, text));
        }

        return collection;
    }

    public void GetAll(LookupDirection direction, bool followLinks = false)
    {
        var entries = _vectorConnector.GetAll(direction, Table.Lookup);

        var vectors = new List<IVector>();

        foreach (var entry in entries)
        {
            var vector = _vectorConnector.GetVector(direction, entry.Key);

            if (followLinks && vector.EntryId != vector.LinkId)
            {
                vector = _vectorConnector.GetVector(direction, vector.LinkId);
            }

            if (vector.EntryId != vector.LinkId)
            {
                var damn = true;
            }

            vectors.Add(vector);
        }
    }
}