namespace EReaderGyldendalDictionaryCreator.Connector.Vector.Entity;

internal interface IVector
{
    int EntryId { get; }
    int LinkId { get; }
    int Offset { get; }
    int Count { get; }
}