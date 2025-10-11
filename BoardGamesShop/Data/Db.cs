using System;
using System.IO;
using System.Data.SQLite;
using System.Data;
using System.Security.Cryptography;

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

            using (var pragma = new SQLiteCommand("PRAGMA foreign_keys=ON; PRAGMA synchronous=NORMAL;", con))
                pragma.ExecuteNonQuery();

            var ddl = @"
                BEGIN TRANSACTION;

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
                  Price       REAL    NOT NULL CHECK(Price >= 0),
                  MinPlayers  INTEGER,
                  MaxPlayers  INTEGER,
                  Age         INTEGER,
                  Duration    INTEGER,
                  Stock       INTEGER NOT NULL DEFAULT 0 CHECK(Stock >= 0),
                  IsActive    INTEGER NOT NULL DEFAULT 1,
                  ImagePath   TEXT NULL,
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

                DROP VIEW IF EXISTS v_GamesWithPrice;
                CREATE VIEW v_GamesWithPrice AS
                SELECT g.Id, g.Name, g.Price AS BasePrice, g.Stock, g.IsActive, g.Price AS FinalPrice
                FROM Games g;

                CREATE INDEX IF NOT EXISTS idx_games_genre  ON Games(GenreId);
                CREATE INDEX IF NOT EXISTS idx_games_author ON Games(AuthorId);

                COMMIT;";
            using (var cmd = new SQLiteCommand(ddl, con))
                cmd.ExecuteNonQuery();

            var seed = @"
                BEGIN TRANSACTION;
                INSERT OR IGNORE INTO Genres (Id, Name) VALUES
                (1,'Fantasy'), (2,'Horor'), (3,'Rodinné'), (4,'Strategické'), (5,'Vědomostní'), (6,'Párty'),
                (7,'Sci-fi'), (8,'Kompetitivní'), (9,'Kooperativní'), (10,'Semi-kooperativní'),
                (11,'Házení kostek'), (12,'Area Control'), (13,'Duel'), (14,'Karetní'), (15,'Humor'), (16,'Post-Apokalypticke');
                COMMIT;
                UPDATE sqlite_sequence SET seq=(SELECT MAX(Id) FROM Genres) WHERE name='Genres';

                BEGIN TRANSACTION;
                INSERT OR IGNORE INTO Authors (Id, Name, Country) VALUES
                (1,'Greg Loring-Albright','Itálie'),
                (2,'Richard Launius','Belgie'),
                (3,'Matthew Inman','USA'),
                (4,'Antoine Bauza','France'),
                (5,'Emiliano Sciarra','France'),
                (6,'Bruno Faidutti','Itálie'),
                (7,'Nate Chatellier','Itálie'),
                (8,'James A. Wilson','USA'),
                (9,'David Lance Arneson','Germany'),
                (10,'Roman Hladík','Czech Republic'),
                (11,'Josh Wood','USA'),
                (12,'Reiner Knizia','Germany'),
                (13,'Steve Jackson','USA'),
                (14,'Elizabeth Hargrave','USA'),
                (15,'Henry Audubon','USA'),
                (16,'Daniel Piechnick','Australia'),
                (17,'Cole Wehrle','USA'),
                (18,'Luc Rémond','France'),
                (19,'Joseph Adams','USA'),
                (20,'Oliver Barrett','Británie'),
                (21,'Merril Robinson','USA'),
                (22,'Bruno Buchelati','Italie');
                COMMIT;
                UPDATE sqlite_sequence SET seq=(SELECT MAX(Id) FROM Authors) WHERE name='Authors';

                BEGIN TRANSACTION;
                INSERT OR IGNORE INTO Games
                (Id, Name, AuthorId, GenreId, Price, MinPlayers, MaxPlayers, Age, Duration, Stock, IsActive, ImagePath) VALUES
                (1,'Ahoy',                         1, NULL, 1100, 2, 4, 14,  75, 10, 1, NULL),
                (2,'Arkham Horror',                2, NULL, 1899, 1, 4, 14, 120, 10, 1, NULL),
                (3,'Výbušná koťátka',              3, NULL,  659, 2, 5,  7,  15, 10, 1, NULL),
                (4,'Výbušná zombie koťátka',       3, NULL,  669, 2, 6,  7,  15, 10, 1, NULL),
                (5,'Takenoko',                     4, NULL,  899, 2, 4,  8,  45, 10, 1, NULL),
                (6,'BANG! kostková hra',           5, NULL,  429, 3, 7,  8,  45, 10, 1, NULL),
                (7,'Citadela',                     6, NULL,  799, 2, 8, 10,  60, 10, 1, NULL),
                (8,'Dice Throne',                  7, NULL,  799, 2, 2,  8,  45, 10, 1, NULL),
                (9,'Divukraj',                     8, NULL, 1500, 1, 4, 13,  90, 10, 1, NULL),
                (10,'Dungeons and Dragons',        9, NULL,12049, 1, 5, 18, 480, 10, 1, NULL),
                (11,'Karak Goblins',               10,NULL,  349, 2, 5,  8,  30, 10, 1, NULL),
                (12,'Kočičí klub',                 11,NULL,  549, 2, 4,  8,  30, 10, 1, NULL),
                (13,'MLEM',                        12,NULL,  549, 2, 5,  8,  60, 10, 1, NULL),
                (14,'Munchkin',                    13,NULL,  399, 3, 6, 10,  45, 10, 1, NULL),
                (15,'Na Křídlech',                 14,NULL, 1799, 1, 5, 12, 140, 10, 1, NULL),
                (16,'Parks',                       15,NULL, 1449, 2, 4, 12,  60, 10, 1, NULL),
                (17,'Radlands',                    16,NULL,  599, 2, 2, 14,  45, 10, 1, NULL),
                (18,'Root',                        17,NULL, 1650, 2, 4, 10,  90, 10, 1, NULL),
                (19,'Sky Team',                    18,NULL,  649, 2, 2, 14,  20, 10, 1, NULL),
                (20,'Solar',                       19,NULL,  899, 1, 4, 10,  90, 10, 1, NULL),
                (21,'Unmatched',                   20,NULL,  777, 2, 2,  9,  30, 10, 1, NULL),
                (22,'UNO',                         21,NULL,  309, 2,10,  7,  20, 10, 1, NULL),
                (23,'Dice Throne Adventures',      7, NULL, 2099, 3, 5,  8,  45, 10, 1, NULL);
                COMMIT;
                UPDATE sqlite_sequence SET seq=(SELECT MAX(Id) FROM Games) WHERE name='Games';

                BEGIN TRANSACTION;
                UPDATE Games SET ImagePath='Assets/images/Ahoy.png'                    WHERE Id=1;
                UPDATE Games SET ImagePath='Assets/images/Arkham_Horror.png'          WHERE Id=2;
                UPDATE Games SET ImagePath='Assets/images/Vybusna_kotatka_classic.png' WHERE Id=3;
                UPDATE Games SET ImagePath='Assets/images/Vybusna_kotatka_zombie.png'  WHERE Id=4;
                UPDATE Games SET ImagePath='Assets/images/Takenoko.png'               WHERE Id=5;
                UPDATE Games SET ImagePath='Assets/images/BANG_kostky.png'            WHERE Id=6;
                UPDATE Games SET ImagePath='Assets/images/Citadela_Deluxe.png'        WHERE Id=7;
                UPDATE Games SET ImagePath='Assets/images/DiceThrone.png'             WHERE Id=8;
                UPDATE Games SET ImagePath='Assets/images/Divukraj.png'               WHERE Id=9;
                UPDATE Games SET ImagePath='Assets/images/DnD.png'                    WHERE Id=10;
                UPDATE Games SET ImagePath='Assets/images/Karak_goblins.png'          WHERE Id=11;
                UPDATE Games SET ImagePath='Assets/images/Kocici_klub.png'            WHERE Id=12;
                UPDATE Games SET ImagePath='Assets/images/Mlem.png'                   WHERE Id=13;
                UPDATE Games SET ImagePath='Assets/images/Munchkin.png'               WHERE Id=14;
                UPDATE Games SET ImagePath='Assets/images/Na_kridlech.png'            WHERE Id=15;
                UPDATE Games SET ImagePath='Assets/images/Parks.png'                  WHERE Id=16;
                UPDATE Games SET ImagePath='Assets/images/Radlands_Deluxe.png'        WHERE Id=17;
                UPDATE Games SET ImagePath='Assets/images/Root.png'                   WHERE Id=18;
                UPDATE Games SET ImagePath='Assets/images/Sky_Team.png'               WHERE Id=19;
                UPDATE Games SET ImagePath='Assets/images/Solar.png'                  WHERE Id=20;
                UPDATE Games SET ImagePath='Assets/images/Unmatched.png'              WHERE Id=21;
                UPDATE Games SET ImagePath='Assets/images/UNO_classic.png'            WHERE Id=22;
                UPDATE Games SET ImagePath='Assets/images/DiceThrone_adventures.png'  WHERE Id=23;
                COMMIT;

                BEGIN TRANSACTION;
                INSERT OR IGNORE INTO GameCategories (GameId, GenreId) VALUES
                (1,1),
                (2,2),
                (3,6),(3,14),(3,8),(3,15),
                (4,6),(4,14),(4,8),(4,15),
                (5,5),(5,3),(5,10),
                (6,8),(6,14),(6,11),
                (7,4),(7,14),(7,8),(7,5),
                (8,14),(8,1),(8,8),(8,13),
                (9,13),(9,10),(9,4),(9,5),(9,1),
                (10,11),(10,9),(10,1),
                (11,14),(11,3),(11,11),(11,8),(11,1),
                (12,3),(12,15),(12,14),
                (13,11),(13,12),(13,10),(13,7),
                (14,1),(14,8),(14,14),
                (15,8),(15,14),(15,1),
                (16,3),(16,4),(16,14),
                (17,13),(17,14),(17,8),(17,16),(17,5),
                (18,12),(18,11),(18,10),(18,4),(18,1),
                (19,13),(19,9),(19,11),
                (20,4),(20,11),(20,12),(20,7),
                (21,13),(21,4),(21,1),
                (22,14),(22,3),(22,6),
                (23,14),(23,1),(23,9);
                COMMIT;";
            using (var seedCmd = new SQLiteCommand(seed, con))
                seedCmd.ExecuteNonQuery();

            EnsureUser(con, userName: "admin", email: "admin@example.com", roleId: 1, plainPassword: "123456");
            EnsureUser(con, userName: "user", email: "user@example.com", roleId: 2, plainPassword: "123456");
        }

        private static void EnsureUser(SQLiteConnection con, string userName, string email, int roleId, string plainPassword)
        {
            using (var check = new SQLiteCommand("SELECT Id FROM Users WHERE UserName=@u LIMIT 1;", con))
            {
                check.Parameters.AddWithValue("@u", userName);
                var exists = check.ExecuteScalar();
                if (exists != null) return;
            }

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password: plainPassword,
                salt: salt,
                iterations: 100_000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32
            );

            using var cmd = new SQLiteCommand(@"
                INSERT INTO Users(UserName, Email, RoleId, PasswordHash, PasswordSalt, CreatedAt)
                VALUES(@u, @e, @r, @h, @s, datetime('now'));
            ", con);
            cmd.Parameters.AddWithValue("@u", userName);
            cmd.Parameters.AddWithValue("@e", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
            cmd.Parameters.AddWithValue("@r", roleId);
            cmd.Parameters.Add("@h", DbType.Binary, hash.Length).Value = hash;
            cmd.Parameters.Add("@s", DbType.Binary, salt.Length).Value = salt;
            cmd.ExecuteNonQuery();
        }
    }
}
