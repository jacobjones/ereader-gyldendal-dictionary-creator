using System.IO.MemoryMappedFiles;
using EReaderGyldendalDictionaryCreator.Connector.Vector.Entity;

namespace EReaderGyldendalDictionaryCreator.Connector.Dictionary;

internal class DictionaryConnector : IDictionaryConnector
{
    private const string FilePath = "data/EngelskOrdbog.dat";

    public ICollection<(int entryId, byte[] data)> GetEntries(ICollection<IVector> vectors)
    {
        using var mmf = MemoryMappedFile.CreateFromFile(FilePath, FileMode.Open);

        ICollection<(int entryId, byte[] data)> collection = new List<(int entryId, byte[] data)>();

        foreach (var vector in vectors)
        {
            byte[] bytes = new byte[vector.Count];

            using var accessor = mmf.CreateViewAccessor(vector.Offset, vector.Count);

            accessor.ReadArray(0, bytes, 0, vector.Count);

            collection.Add((vector.EntryId, bytes));
        }

        return collection;
    }
}