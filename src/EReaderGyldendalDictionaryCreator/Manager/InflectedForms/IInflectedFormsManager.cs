using EReaderGyldendalDictionaryCreator.Mapper.Entity;

namespace EReaderGyldendalDictionaryCreator.Manager.InflectedForms;

internal interface IInflectedFormsManager
{
    ICollection<string> Get(string headword);
    void AddInflectedForms(ICollection<IEntry> entries);
}