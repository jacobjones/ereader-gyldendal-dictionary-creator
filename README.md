# Gyldendal Danish–English Dictionary Creator for E-readers 🇩🇰🇬🇧

## Description
A .NET console application that generates either a [StarDict](https://en.wikipedia.org/wiki/StarDict) (for example, for a BOOX device) or Kindle compatible dictionary from Gyldendal dictionary database files.

The application is specifically built around creating a good Danish–English dictionary for e-readers (as none exists), but could be easily modified to support English–Danish (or other Gyldendal dictionaries).

Based on both [JavaGro](https://github.com/ejvindh/JavaGro/) and [spt-gro](https://github.com/Athas/spt-gro).

## Prerequisites

1. The [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).
2. A downloaded Gyldendal dictionary (previously at http://ordbog.gyldendal.dk/). Unfortunately, this is now pretty difficult to find. The *.dat and *.gdd files are required once the dictionary has been installed.
3. (Optional) The full-form list for "Den Danske Ordbog" available [here](https://korpus.dsl.dk/resources/details/ddo-fullforms.html), this provides additional inflected forms that the Gyldendal's Danish–English dictionary is missing.
4. (StarDict only) StarDict Editor to compile the generated dictionary. Available for multiple platforms [here](https://stardict-4.sourceforge.net/index_en.php).
5. (Kindle only) The Kindle Previewer is required to generate a dictionary from the generated OPF (XML) file. Available for multiple platforms [here](https://www.amazon.com/Kindle-Previewer/b?node=21381691011). A good write-up about creating a custom Kindle dictionary is [here](https://jakemccrary.com/blog/2020/11/11/creating-a-custom-kindle-dictionary/).

## Running

1. Install the [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
2. Clone the repository
3. Copy *.dat and *.gdd to the `data` directory (alternatively, the path can be changed [here](src/EReaderGyldendalDictionaryCreator/Connector/Vector/DictionaryVectorConnector.cs) and [here](src/EReaderGyldendalDictionaryCreator/Connector/Vector/DictionaryVectorConnector.cs))
4. Download, unzip, and copy "Den Danske Ordbog" full-form list to the `data`` directory (or change the path [here](src/EReaderGyldendalDictionaryCreator/Manager/InflectedForms/InflectedFormsManager.cs))
5. (Kindle only) To generate a Kindle dictionary switch `IOutputGenerator` in [Program.cs](src/EReaderGyldendalDictionaryCreator/Program.cs) and update the outputted file in [App.cs](src/EReaderGyldendalDictionaryCreator/App.cs).
6. (StarDict only) Some devices/applications support synonyms, if applicable set the `SupportSynonyms` flag to `true` in the [StarDictBabylonGenerator.cs](src/EReaderGyldendalDictionaryCreator/Generator/StarDictBabylonGenerator.cs)
7. From the src directory use `dotnet run`

## Generating a Dictionary

### StarDict
Use the StarDict Editor to compile a dictionary from the outputted text file. You should select "Babylon file" as the file type and provide the generated text file for the input.

### Kindle
Follow the steps provided in ["Creating a custom Kindle dictionary"](https://jakemccrary.com/blog/2020/11/11/creating-a-custom-kindle-dictionary/) to convert the generated XML file into a Kindle dictionary.