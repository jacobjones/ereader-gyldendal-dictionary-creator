namespace EReaderGyldendalDictionaryCreator.Parser;

internal interface IGroParser
{
    string ParseEntry(int entryId, byte[] bytes);
}