using Dapper;
using Npgsql;

namespace Mp3Storage.Core.Models;

public static class FileRepository
{
    public static string ConectionString;

    public static IEnumerable<FileMp3> GetAll()
    {
        using (var db = new NpgsqlConnection(ConectionString))
        {
            var result = db.Query<FileMp3>(@"Select * from ""Files""");
            return result;
        }
    }

    public static void Delete(int id)
    {
        using (var db = new NpgsqlConnection(ConectionString))
        {
            db.Execute(@"Delete from ""Files"" WHERE ""Id"" = @Id", new{Id = id});
        }
    }
}