using EReaderGyldendalDictionaryCreator.Mapper.Entity;

namespace EReaderGyldendalDictionaryCreator.Generator;

internal interface IOutputGenerator
{
    string Generate(ICollection<IEntry> entries);
}