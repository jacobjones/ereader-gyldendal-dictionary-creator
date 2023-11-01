using EReaderGyldendalDictionaryCreator;
using EReaderGyldendalDictionaryCreator.Connector.Dictionary;
using EReaderGyldendalDictionaryCreator.Connector.Vector;
using EReaderGyldendalDictionaryCreator.Generator;
using EReaderGyldendalDictionaryCreator.Manager.Dictionary;
using EReaderGyldendalDictionaryCreator.Manager.InflectedForms;
using EReaderGyldendalDictionaryCreator.Mapper;
using EReaderGyldendalDictionaryCreator.Parser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = CreateHostBuilder().Build();
using var scope = host.Services.CreateScope();

try
{
    scope.ServiceProvider.GetRequiredService<App>().Run();
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}

IHostBuilder CreateHostBuilder()
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<IGroParser, GroParser>();
            services.AddSingleton<IDictionaryVectorConnector, DictionaryVectorConnector>();
            services.AddSingleton<IDictionaryConnector, DictionaryConnector>();
            services.AddSingleton<IDictionaryManager, DictionaryManager>();
            services.AddSingleton<IEntryMapper, EntryMapper>();
            services.AddSingleton<IOutputGenerator, StarDictBabylonGenerator>();
            // Switch to use Kindle XML generator
            //services.AddSingleton<IOutputGenerator, KindleXmlGenerator>();
            services.AddSingleton<IInflectedFormsManager, InflectedFormsManager>();
            services.AddSingleton<App>();
        });
}