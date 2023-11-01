using EReaderGyldendalDictionaryCreator.Connector.Vector.Entity;
using EReaderGyldendalDictionaryCreator.Connector.Vector.Property;

namespace EReaderGyldendalDictionaryCreator.Connector.Vector;

internal interface IDictionaryVectorConnector
{
    int GetEntryCount(LookupDirection direction);

    IVector GetVector(LookupDirection direction, int entryId);

    IList<IVector> GetVectors(LookupDirection direction, ICollection<int> entryIds);

    IList<IVector> GetVectors(LookupDirection direction, int skip, int count);

    IList<int> Search(LookupDirection direction, string table, string query);

    IList<int> Search(LookupDirection direction, IList<string> terms);

    IDictionary<int, IList<string>> GetAll(LookupDirection direction, string table);

    IList<string> GetColumnNames(string table);
        
    IList<string> GetColumnNames(LookupDirection direction, string table);

    IList<string> GetTables();
};