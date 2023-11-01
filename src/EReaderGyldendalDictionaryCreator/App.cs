using EReaderGyldendalDictionaryCreator.Connector.Vector.Property;
using EReaderGyldendalDictionaryCreator.Generator;
using EReaderGyldendalDictionaryCreator.Manager.Dictionary;
using EReaderGyldendalDictionaryCreator.Manager.InflectedForms;
using EReaderGyldendalDictionaryCreator.Mapper;
using EReaderGyldendalDictionaryCreator.Mapper.Entity;

namespace EReaderGyldendalDictionaryCreator;

internal class App
{
    private readonly IDictionaryManager _dictionaryManager;
    private readonly IEntryMapper _entryMapper;
    private readonly IInflectedFormsManager _inflectedFormsManager;
    private readonly IOutputGenerator _outputGenerator;
    
    public App(IDictionaryManager dictionaryManager, IEntryMapper entryMapper, IInflectedFormsManager inflectedFormsManager, IOutputGenerator outputGenerator)
    {
        _dictionaryManager = dictionaryManager;
        _entryMapper = entryMapper;
        _inflectedFormsManager = inflectedFormsManager;
        _outputGenerator = outputGenerator;
    }

    public void Run()
    {
        // This will do a typical Danish > English search
        //var milkSearch = _dictionaryManager.Search(LookupDirection.FromDanish, SearchType.Lookup, "mælk", true);

        var count = _dictionaryManager.GetEntriesCount(LookupDirection.FromDanish);
        var entryValues = _dictionaryManager.GetEntries(LookupDirection.FromDanish, 0, count);
        //var entryValues = _dictionaryManager.GetEntries(LookupDirection.FromDanish, 0, 5000);

        var entries = entryValues.Select(entry => _entryMapper.Map(entry.entryId, entry.text))
            .Where(x => x.Type == EntryType.Standard).ToList();
        
        _inflectedFormsManager.AddInflectedForms(entries);
            
        var xml = _outputGenerator.Generate(entries);

        File.WriteAllText("stardict-gyldendal_dansk_engelsk.babylon.txt",xml);
    }
}