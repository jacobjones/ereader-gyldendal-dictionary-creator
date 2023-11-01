using System.Text;
using EReaderGyldendalDictionaryCreator.Connector.Vector.Entity;
using EReaderGyldendalDictionaryCreator.Connector.Vector.Property;
using Microsoft.Data.Sqlite;

namespace EReaderGyldendalDictionaryCreator.Connector.Vector;

internal class DictionaryVectorConnector : IDictionaryVectorConnector
{
    private const string ConnectionString = "Data Source = data/EngelskOrdbog.gdd";

    public IVector GetVector(LookupDirection direction, int entryId)
    {
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select * from entries{(int)direction} where id_ = {entryId};";

        using var reader = command.ExecuteReader();

        IVector vector = null;

        while (reader.Read())
        {
            vector = new Entity.Vector(reader.GetInt32(0), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4));
        }

        connection.Close();

        return vector;
    }

    public IList<IVector> GetVectors(LookupDirection direction, ICollection<int> entryIds)
    {
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select * from entries{(int)direction} where id_ in ({string.Join(",", entryIds)});";

        using var reader = command.ExecuteReader();

        IList<IVector> vectors = new List<IVector>();

        while (reader.Read())
        {
            vectors.Add(new Entity.Vector(reader.GetInt32(0), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4)));
        }

        connection.Close();

        return vectors;
    }

    public int GetEntryCount(LookupDirection direction)
    {
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select count(id_) from entries{(int)direction};";

        long count = (long)command.ExecuteScalar();

        connection.Close();

        return (int)count;
    }

    public IList<IVector> GetVectors(LookupDirection direction, int skip, int count)
    {
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select * from entries{(int)direction} order by id_ asc limit {skip}, {count};";

        using var reader = command.ExecuteReader();

        IList<IVector> vectors = new List<IVector>();

        while (reader.Read())
        {
            vectors.Add(new Entity.Vector(reader.GetInt32(0), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4)));
        }

        connection.Close();

        return vectors;
    }

    public IList<int> Search(LookupDirection direction, string table, string query)
    {
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        // entry_id_, word_
        command.CommandText = $"select entry_id_ from {table}{(int)direction} where word_ like \'{query}\';";

        using var reader = command.ExecuteReader();

        IList<int> entryIds = new List<int>();

        while (reader.Read())
        {
            entryIds.Add(reader.GetInt32(0));
        }

        connection.Close();

        return entryIds;
    }

    public IList<int> Search(LookupDirection direction, IList<string> terms)
    {
        if (!terms.Any())
        {
            throw new ArgumentException("terms requires at least one value.", nameof(terms));
        }
            
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        var sb = new StringBuilder();

        sb.Append($"select a.entry_id_ from {Table.CollocationLookup}{(int)direction} {GetUniqueName(0)}");

        for (int i = 1; i < terms.Count; i++)
        {
            sb.Append($", {Table.CollocationLookup}{(int)direction} {GetUniqueName(i)}");
        }

        sb.Append($" where {GetUniqueName(0)}.word_ like \'{terms.First()}\'");

        for (int i = 1; i < terms.Count; i++)
        {
            sb.Append($" and {GetUniqueName(i-1)}.entry_id_ = {GetUniqueName(i)}.entry_id_ and {GetUniqueName(i)}.word_ like \'{terms[i]}\'");
        }

        using var command = connection.CreateCommand();

        command.CommandText = sb.ToString();

        using var reader = command.ExecuteReader();

        IList<int> entryIds = new List<int>();

        while (reader.Read())
        {
            entryIds.Add(reader.GetInt32(0));
        }

        connection.Close();

        return entryIds;
    }

    public IDictionary<int, IList<string>> GetAll(LookupDirection direction, string table)
    {
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select * from {table}{(int)direction};";

        using var reader = command.ExecuteReader();

        ICollection<(int id, string word)> entries = new List<(int id, string word)>();

        while (reader.Read())
        {
            entries.Add((reader.GetInt32(0), reader.GetString(1)));
        }

        connection.Close();

        return entries.GroupBy(x => x.id).ToDictionary(x => x.Key, x => (IList<string>)x.Select(e => e.word).ToList());
    }
        
    public IList<string> GetColumnNames(string table)
    {
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"PRAGMA table_info('{table}');";

        using var reader = command.ExecuteReader();

        IList<string> tables = new List<string>();

        while (reader.Read())
        {
            tables.Add(reader.GetString(1));
        }

        connection.Close();

        return tables;
    }

    public IList<string> GetColumnNames(LookupDirection direction, string table)
    {
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"PRAGMA table_info('{table}{(int)direction}');";

        using var reader = command.ExecuteReader();

        IList<string> tables = new List<string>();

        while (reader.Read())
        {
            tables.Add(reader.GetString(1));
        }

        connection.Close();

        return tables;
    }

    public IList<string> GetTables()
    {
        using var connection = new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select name from sqlite_master where type = 'table' order by 1";

        using var reader = command.ExecuteReader();

        IList<string> tables = new List<string>();

        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        connection.Close();

        return tables;
    }

    private static string GetUniqueName(int index)
    {
        const string letters = "abcdefghijklmnopqrstuvwxyz";

        var value = "";

        if (index >= letters.Length)
        {
            value += letters[index / letters.Length - 1];
        }

        value += letters[index % letters.Length];

        return value;
    }
}