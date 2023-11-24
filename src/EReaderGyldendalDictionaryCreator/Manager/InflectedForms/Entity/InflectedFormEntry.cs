using CsvHelper.Configuration.Attributes;

namespace EReaderGyldendalDictionaryCreator.Manager.InflectedForms.Entity;

internal class InflectedFormEntry : IInflectedFormEntry
{
    [Index(0)]
    public string InflectedForm { get; set; }
        
    [Index(1)]
    public string Headword { get; set; }
        
    [Index(2)]
    public int? HomographNumber { get; set; }
        
    [Index(3)]
    public string PartOfSpeech { get; set; }
        
    [Index(4)]
    public int Id { get; set; }
}