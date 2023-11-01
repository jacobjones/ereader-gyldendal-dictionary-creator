using EReaderGyldendalDictionaryCreator.Mapper.Entity;

namespace EReaderGyldendalDictionaryCreator.Mapper;

internal interface IEntryMapper
{
    IEntry Map(int entryId, string text);
}