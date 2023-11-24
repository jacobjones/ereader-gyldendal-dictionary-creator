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

        var mappedEntries = entryValues.Select(entry => _entryMapper.Map(entry.entryId, entry.text)).ToList();
        
        var standardEntries = mappedEntries.Where(x => x.Type == EntryType.Standard).ToList();

        var inflectedEntries = mappedEntries.Where(x => x.Type == EntryType.Inflection).GroupBy(x => x.Headword)
            .ToDictionary(x => x.Key, x => x.ToList());

        _inflectedFormsManager.MergeInflectedForms(standardEntries, inflectedEntries);
        _inflectedFormsManager.AddInflectedForms(standardEntries);
        
        var xml = _outputGenerator.Generate(standardEntries);
        
        // Should have plural "oprindelige"
        var test1 = standardEntries.SingleOrDefault(x => x.Headword == "oprindelig");
        // Should have t-form "ensformig"
        var test2 = standardEntries.SingleOrDefault(x => x.Headword == "ensformigt");

        File.WriteAllText("stardict-gyldendal_dansk_engelsk.babylon.txt",xml);
    }
}