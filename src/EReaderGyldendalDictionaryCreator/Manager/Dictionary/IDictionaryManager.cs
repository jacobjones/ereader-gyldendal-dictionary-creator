using EReaderGyldendalDictionaryCreator.Connector.Vector.Property;
using EReaderGyldendalDictionaryCreator.Manager.Dictionary.Property;

namespace EReaderGyldendalDictionaryCreator.Manager.Dictionary;

internal interface IDictionaryManager
{
    int GetEntriesCount(LookupDirection direction);

    ICollection<(int entryId, string text)> GetEntries(LookupDirection direction, int skip, int count);

    ICollection<(int entryId, string text)> Search(LookupDirection direction, SearchType type, string query, bool followLinks = false);

    void GetAll(LookupDirection direction, bool followLinks = false);
}