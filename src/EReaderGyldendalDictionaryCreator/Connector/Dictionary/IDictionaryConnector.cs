using EReaderGyldendalDictionaryCreator.Connector.Vector.Entity;

namespace EReaderGyldendalDictionaryCreator.Connector.Dictionary;

internal interface IDictionaryConnector
{
    ICollection<(int entryId, byte[] data)> GetEntries(ICollection<IVector> vectors);
}