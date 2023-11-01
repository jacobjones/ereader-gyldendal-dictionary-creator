namespace EReaderGyldendalDictionaryCreator.Mapper.Entity;

internal class Entry : IEntry
{
    public Entry(int id)
    {
        Id = id;
    }

    public int Id { get; set; }
    public EntryType Type { get; set; }
    public string Headword { get; set; }
    public string PrimaryWord { get; set; }
    public ICollection<string> AlternativeWords { get; set; }
    public ICollection<string> Words { get; set; }
    public string RawData { get; set; }
}