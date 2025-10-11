using System;
using System.IO;
using System.Data.SQLite;

namespace BoardGamesShop.Data
{
    public static class Db
    {
        private static readonly string DbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shop.db");

        public static string ConnectionString =>
            $"Data Source={DbPath};Version=3;Foreign Keys=True;BusyTimeout=5000;Journal Mode=WAL;";

        public static void EnsureCreated()
        {
            if (!File.Exists(DbPath))
                SQLiteConnection.CreateFile(DbPath);

            using var con = new SQLiteConnection(ConnectionString);
            con.Open();

            try
            {
                using (var pragma = new SQLiteCommand("PRAGMA foreign_keys=ON; PRAGMA synchronous=NORMAL;", con))
                    pragma.ExecuteNonQuery();

                string ddl = @"
                CREATE TABLE IF NOT EXISTS Genres (
                  Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                  Name TEXT NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS Authors (
                  Id      INTEGER PRIMARY KEY AUTOINCREMENT,
                  Name    TEXT NOT NULL UNIQUE,
                  Country TEXT
                );

                CREATE TABLE IF NOT EXISTS Games (
                  Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                  Name        TEXT NOT NULL,
                  AuthorId    INTEGER NULL,
                  GenreId     INTEGER NULL,
                  Price       REAL NOT NULL CHECK(Price >= 0),
                  MinPlayers  INTEGER,
                  MaxPlayers  INTEGER,
                  Age         INTEGER,
                  Duration    INTEGER,
                  Stock       INTEGER NOT NULL DEFAULT 0 CHECK(Stock >= 0),
                  IsActive    INTEGER NOT NULL DEFAULT 1,
                  FOREIGN KEY (AuthorId) REFERENCES Authors(Id) ON DELETE SET NULL ON UPDATE CASCADE,
                  FOREIGN KEY (GenreId)  REFERENCES Genres(Id)  ON DELETE SET NULL ON UPDATE CASCADE
                );

                CREATE TABLE IF NOT EXISTS GameCategories (
                  GameId  INTEGER NOT NULL,
                  GenreId INTEGER NOT NULL,
                  PRIMARY KEY (GameId, GenreId),
                  FOREIGN KEY (GameId)  REFERENCES Games(Id)  ON DELETE CASCADE ON UPDATE CASCADE,
                  FOREIGN KEY (GenreId) REFERENCES Genres(Id) ON DELETE CASCADE ON UPDATE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Roles (
                  Id   INTEGER PRIMARY KEY,
                  Name TEXT NOT NULL UNIQUE
                );
                INSERT OR IGNORE INTO Roles(Id, Name) VALUES (1,'Admin'), (2,'User');

                CREATE TABLE IF NOT EXISTS Users (
                  Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                  UserName     TEXT NOT NULL UNIQUE,
                  Email        TEXT UNIQUE,
                  RoleId       INTEGER NOT NULL DEFAULT 2,
                  PasswordHash BLOB NOT NULL,
                  PasswordSalt BLOB NOT NULL,
                  CreatedAt    TEXT NOT NULL DEFAULT (datetime('now')),
                  FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE RESTRICT ON UPDATE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Sessions (
                  Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                  UserId    INTEGER NOT NULL,
                  Token     TEXT NOT NULL UNIQUE,
                  ExpiresAt TEXT NOT NULL,
                  FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Orders (
                  Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                  UserId     INTEGER NOT NULL,
                  OrderDate  TEXT NOT NULL DEFAULT (datetime('now')),
                  TotalPrice REAL NOT NULL CHECK(TotalPrice >= 0),
                  Status     TEXT NOT NULL DEFAULT 'New',
                  FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE ON UPDATE CASCADE
                );

                CREATE TABLE IF NOT EXISTS OrderItems (
                  OrderId   INTEGER NOT NULL,
                  LineNo    INTEGER NOT NULL,
                  GameId    INTEGER NULL,
                  GameName  TEXT NOT NULL,
                  Quantity  INTEGER NOT NULL CHECK(Quantity > 0),
                  UnitPrice REAL NOT NULL CHECK(UnitPrice >= 0),
                  PRIMARY KEY (OrderId, LineNo),
                  FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE ON UPDATE CASCADE,
                  FOREIGN KEY (GameId)  REFERENCES Games(Id)  ON DELETE SET NULL ON UPDATE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Discounts (
                  Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                  GameId   INTEGER NOT NULL,
                  Percent  REAL NOT NULL CHECK(Percent>=0 AND Percent<=90),
                  StartsAt TEXT NOT NULL,
                  EndsAt   TEXT NOT NULL,
                  IsActive INTEGER NOT NULL DEFAULT 1,
                  FOREIGN KEY (GameId) REFERENCES Games(Id) ON DELETE CASCADE
                );

                DROP VIEW IF EXISTS v_GamesWithPrice;
                CREATE VIEW v_GamesWithPrice AS
                SELECT
                  g.Id, g.Name, g.Price AS BasePrice, g.Stock, g.IsActive,
                  COALESCE(
                    ROUND(g.Price * (1 - (SELECT MAX(Percent)/100.0
                                          FROM Discounts d
                                          WHERE d.GameId = g.Id
                                            AND d.IsActive=1
                                            AND datetime('now') BETWEEN d.StartsAt AND d.EndsAt)), 2),
                    g.Price
                  ) AS FinalPrice
                FROM Games g;

                CREATE INDEX IF NOT EXISTS idx_games_genre  ON Games(GenreId);
                CREATE INDEX IF NOT EXISTS idx_games_author ON Games(AuthorId);
                CREATE INDEX IF NOT EXISTS idx_orders_user  ON Orders(UserId);
                ";
                using var cmd = new SQLiteCommand(ddl, con);
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                System.Windows.MessageBox.Show(
                    "SQLite error: " + ex.Message +
                    "\nЗакройте DB Browser/прошлый экземпляр приложения и попробуйте снова.",
                    "Database error", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
