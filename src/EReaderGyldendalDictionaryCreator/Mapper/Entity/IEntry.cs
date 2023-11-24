namespace EReaderGyldendalDictionaryCreator.Mapper.Entity;

internal interface IEntry
{
    int Id { get; }
    string? PartOfSpeech { get; }
    EntryType Type { get; }
    string Headword { get; }
    string PrimaryWord { get; }
    ICollection<string> AlternativeWords { get; }
    ICollection<string> Words { get; }
    string RawData { get; }
}