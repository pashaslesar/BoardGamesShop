using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using BoardGamesShop.Data;
using DataModels;

namespace DataControll
{
    public static class DBController
    {
        public static List<Game> GetGames()
        {
            const string sql = @"
                SELECT
                    Id,
                    Name,
                    AuthorId,
                    GenreId,
                    Price,
                    MinPlayers,
                    MaxPlayers,
                    Age,
                    Duration AS PlayTime,
                    Stock,
                    IsActive,
                    ImagePath
                FROM Games
                WHERE IsActive = 1
                ORDER BY Name;";

            using var con = new SQLiteConnection(Db.ConnectionString);
            con.Open();
            using var cmd = new SQLiteCommand(sql, con);
            using var r = cmd.ExecuteReader();
            var list = new List<Game>();

            int iId = r.GetOrdinal("Id");
            int iName = r.GetOrdinal("Name");
            int iAuthor = r.GetOrdinal("AuthorId");
            int iGenre = r.GetOrdinal("GenreId");
            int iPrice = r.GetOrdinal("Price");
            int iMin = r.GetOrdinal("MinPlayers");
            int iMax = r.GetOrdinal("MaxPlayers");
            int iAge = r.GetOrdinal("Age");
            int iPlay = r.GetOrdinal("PlayTime");
            int iStock = r.GetOrdinal("Stock");
            int iActive = r.GetOrdinal("IsActive");
            int iImg = r.GetOrdinal("ImagePath");

            static int ReadMoney(SQLiteDataReader rd, int ord)
            {
                if (rd.IsDBNull(ord)) return 0;
                object v = rd.GetValue(ord);

                return v switch
                {
                    int i => i,
                    long l => checked((int)l),
                    double d => (int)Math.Round(d, MidpointRounding.AwayFromZero),
                    float f => (int)Math.Round(f, MidpointRounding.AwayFromZero),
                    decimal m => (int)Math.Round((double)m, MidpointRounding.AwayFromZero),
                    string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d2)
                            => (int)Math.Round(d2, MidpointRounding.AwayFromZero),
                    _ => throw new InvalidCastException($"Unsupported numeric type in Price: {v.GetType()}")
                };
            }

            static int? ReadMoneyNullable(SQLiteDataReader rd, int ord)
                => rd.IsDBNull(ord) ? (int?)null : ReadMoney(rd, ord);

            while (r.Read())
            {
                list.Add(new Game
                {
                    Id = r.GetInt32(iId),
                    Name = r.GetString(iName),
                    AuthorId = r.IsDBNull(iAuthor) ? (int?)null : r.GetInt32(iAuthor),
                    GenreId = r.IsDBNull(iGenre) ? (int?)null : r.GetInt32(iGenre),

                    Price = ReadMoney(r, iPrice),

                    MinPlayers = r.IsDBNull(iMin) ? 0 : r.GetInt32(iMin),
                    MaxPlayers = r.IsDBNull(iMax) ? 0 : r.GetInt32(iMax),
                    Age = r.IsDBNull(iAge) ? 0 : r.GetInt32(iAge),
                    PlayTime = r.IsDBNull(iPlay) ? 0 : r.GetInt32(iPlay),
                    Stock = r.IsDBNull(iStock) ? 0 : r.GetInt32(iStock),
                    IsActive = !r.IsDBNull(iActive) && Convert.ToInt32(r.GetValue(iActive)) == 1,
                    ImagePath = r.IsDBNull(iImg) ? null : r.GetString(iImg)
                });
            }

            while (r.Read())
            {
                list.Add(new Game
                {
                    Id = r.GetInt32(iId),
                    Name = r.GetString(iName),
                    AuthorId = r.IsDBNull(iAuthor) ? (int?)null : r.GetInt32(iAuthor),
                    GenreId = r.IsDBNull(iGenre) ? (int?)null : r.GetInt32(iGenre),

                    Price = r.GetInt32(iPrice),

                    MinPlayers = r.IsDBNull(iMin) ? 0 : r.GetInt32(iMin),
                    MaxPlayers = r.IsDBNull(iMax) ? 0 : r.GetInt32(iMax),
                    Age = r.IsDBNull(iAge) ? 0 : r.GetInt32(iAge),
                    PlayTime = r.IsDBNull(iPlay) ? 0 : r.GetInt32(iPlay),
                    Stock = r.IsDBNull(iStock) ? 0 : r.GetInt32(iStock),
                    IsActive = !r.IsDBNull(iActive) && r.GetInt32(iActive) == 1,
                    ImagePath = r.IsDBNull(iImg) ? null : r.GetString(iImg)
                });
            }

            return list;
        }

        public static string GetAuthorNameById(int authorId)
        {
            if (authorId <= 0) return "—";
            using var con = new SQLiteConnection(Db.ConnectionString);
            con.Open();
            using var cmd = new SQLiteCommand("SELECT Name FROM Authors WHERE Id=@id;", con);
            cmd.Parameters.AddWithValue("@id", authorId);
            var o = cmd.ExecuteScalar();
            return o == null || o == DBNull.Value ? "—" : (string)o;
        }

        public static List<string> GetGenreNamesByGameId(int gameId)
        {
            var res = new List<string>();
            using var con = new SQLiteConnection(Db.ConnectionString);
            con.Open();
            using var cmd = new SQLiteCommand(@"
                SELECT ge.Name
                FROM GameCategories gc
                JOIN Genres ge ON ge.Id = gc.GenreId
                WHERE gc.GameId = @g
                ORDER BY ge.Name;", con);
            cmd.Parameters.AddWithValue("@g", gameId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) res.Add(r.GetString(0));
            return res;
        }

        public static int GetGenreIdByName(string name)
        {
            using var con = new SQLiteConnection(Db.ConnectionString);
            con.Open();
            using var cmd = new SQLiteCommand("SELECT Id FROM Genres WHERE Name=@n;", con);
            cmd.Parameters.AddWithValue("@n", name);
            var v = cmd.ExecuteScalar();
            return v == null || v == DBNull.Value ? -1 : Convert.ToInt32(v);
        }

        public static List<int> GetGenreIdsByGameId(int gameId)
        {
            var res = new List<int>();
            using var con = new SQLiteConnection(Db.ConnectionString);
            con.Open();
            using var cmd = new SQLiteCommand("SELECT GenreId FROM GameCategories WHERE GameId=@g;", con);
            cmd.Parameters.AddWithValue("@g", gameId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) res.Add(r.GetInt32(0));
            return res;
        }

        public static List<string> GetGenres()
        {
            var list = new List<string>();
            using var con = new SQLiteConnection(Db.ConnectionString);
            con.Open();
            using var cmd = new SQLiteCommand("SELECT Name FROM Genres ORDER BY Name;", con);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
            return list;
        }


    }
}
