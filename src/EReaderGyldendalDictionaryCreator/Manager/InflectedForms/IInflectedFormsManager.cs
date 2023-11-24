using EReaderGyldendalDictionaryCreator.Manager.InflectedForms.Entity;
using EReaderGyldendalDictionaryCreator.Mapper.Entity;

namespace EReaderGyldendalDictionaryCreator.Manager.InflectedForms
{
    internal interface IInflectedFormsManager
    {
        ICollection<IInflectedFormEntry> Get(string headword);
        
        ICollection<IInflectedFormEntry> Get(string headword, string partOfSpeech);
        void MergeInflectedForms(ICollection<IEntry> entries, IDictionary<string, List<IEntry>> inflectedEntries);
        void AddInflectedForms(ICollection<IEntry> entries);
    }
}