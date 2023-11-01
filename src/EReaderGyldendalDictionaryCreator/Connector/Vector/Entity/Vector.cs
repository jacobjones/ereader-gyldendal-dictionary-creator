namespace EReaderGyldendalDictionaryCreator.Connector.Vector.Entity;

internal class Vector : IVector
{
    public Vector(int entryId, int linkId, int offset, int count)
    {
        EntryId = entryId;
        LinkId = linkId;
        Offset = offset;
        Count = count;
    }

    public int EntryId { get; }
    public int LinkId { get; }
    public int Offset { get; }
    public int Count { get; }
}