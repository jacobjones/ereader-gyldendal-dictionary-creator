namespace EReaderGyldendalDictionaryCreator.Manager.InflectedForms.Entity;

internal interface IInflectedFormEntry
{
    string InflectedForm { get; set; }
    string Headword { get; set; }
    int? HomographNumber { get; set; }
    string PartOfSpeech { get; set; }
    int Id { get; set; }
}