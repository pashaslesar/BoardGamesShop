using System;
using System.Data.SQLite;
using BoardGamesShop.Data;

namespace BoardGamesShop.Data
{
    public sealed class UserDto
    {
        public int Id { get; init; }
        public string UserName { get; init; } = "";
        public string? Email { get; init; }
        public int RoleId { get; init; }
        public byte[] PasswordHash { get; init; } = Array.Empty<byte>();
        public byte[] PasswordSalt { get; init; } = Array.Empty<byte>();
    }

    public static class UserRepository
    {
        public static UserDto? FindByUserNameOrEmail(string userOrEmail)
        {
            using var con = new SQLiteConnection(Db.ConnectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                "SELECT Id, UserName, Email, RoleId, PasswordHash, PasswordSalt " +
                "FROM Users WHERE UserName=@u OR Email=@u LIMIT 1;", con);
            cmd.Parameters.AddWithValue("@u", userOrEmail);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new UserDto
            {
                Id = r.GetInt32(0),
                UserName = r.GetString(1),
                Email = r.IsDBNull(2) ? null : r.GetString(2),
                RoleId = r.GetInt32(3),
                PasswordHash = (byte[])r[4],
                PasswordSalt = (byte[])r[5]
            };
        }

        public static bool UserNameExists(string userName)
        {
            using var con = new SQLiteConnection(Db.ConnectionString);
            con.Open();
            using var cmd = new SQLiteCommand("SELECT 1 FROM Users WHERE UserName=@u LIMIT 1;", con);
            cmd.Parameters.AddWithValue("@u", userName);
            return cmd.ExecuteScalar() != null;
        }

        public static int Create(string userName, string? email, byte[] hash, byte[] salt, int roleId)
        {
            using var con = new SQLiteConnection(Db.ConnectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                "INSERT INTO Users(UserName,Email,RoleId,PasswordHash,PasswordSalt) " +
                "VALUES(@u,@e,@r,@h,@s); SELECT last_insert_rowid();", con);
            cmd.Parameters.AddWithValue("@u", userName);
            cmd.Parameters.AddWithValue("@e", (object?)email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@r", roleId);
            cmd.Parameters.AddWithValue("@h", hash);
            cmd.Parameters.AddWithValue("@s", salt);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
