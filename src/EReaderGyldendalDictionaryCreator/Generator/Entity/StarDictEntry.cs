using EReaderGyldendalDictionaryCreator.Mapper.Entity;

namespace EReaderGyldendalDictionaryCreator.Generator.Entity;

internal class StarDictEntry
{
    public StarDictEntry(ICollection<string> words, ICollection<IEntry> entries)
    {
        Words = words;
        Entries = entries;
    }

    public ICollection<string> Words { get; set; }
    public ICollection<IEntry> Entries { get; set; }
}