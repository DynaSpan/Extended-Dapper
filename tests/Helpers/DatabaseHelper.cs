using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Repository;
using Extended.Dapper.Tests.Models;

namespace Extended.Dapper.Tests.Helpers
{
    public static class DatabaseHelper
    {
        private static bool DbCreated { get; set; } = false;
        private static IDatabaseFactory DatabaseFactory { get; set; }
        private static IDatabaseFactory LegacyDatabaseFactory { get; set; }

        /// <summary>
        /// Creates the database in the SQLite file
        /// </summary>
        public static void CreateDatabase()
        {
            var dbProvider = GetDatabaseFactory().DatabaseProvider;

            // Delete database first
            if (DbCreated && dbProvider == DatabaseProvider.SQLite)
            {
                File.Delete("./test-db.db");
                File.Delete("./test-legacy-db.db"); 
            } 

            if (!DbCreated && dbProvider != DatabaseProvider.SQLite)
            {
                using (var connection = GetCreationConnection())
                {
                    connection.Open();

                    var createDbQuery = connection.CreateCommand();

                    if (dbProvider == DatabaseProvider.MySQL)
                        createDbQuery.CommandText = "CREATE DATABASE IF NOT EXISTS testing; CREATE DATABASE IF NOT EXISTS legacytesting;";
                    else
                    {
                        createDbQuery.CommandText = @"
                            DROP DATABASE IF EXISTS testing;
                            CREATE DATABASE testing; 

                            DROP DATABASE IF EXISTS legacytesting; 
                            CREATE DATABASE legacytesting;";
                    }
                    
                    createDbQuery.ExecuteNonQuery(); 
                }

                //Thread.Sleep(250);
            }

            string tableQuery;
            string legacyTableQuery;

            switch (dbProvider)
            {
                case DatabaseProvider.MSSQL:
                    tableQuery = File.ReadAllText("./test-db.mssql"); 
                    legacyTableQuery = File.ReadAllText("./test-legacy-db.mssql");
                    break;

                case DatabaseProvider.MySQL:
                    tableQuery = File.ReadAllText("./test-db.mysql"); 
                    legacyTableQuery = File.ReadAllText("./test-legacy-db.mysql");
                    break;

                default:
                    tableQuery = File.ReadAllText("./test-db.sql"); 
                    legacyTableQuery = File.ReadAllText("./test-legacy-db.sql");
                    break;
            }

            using (var connection = GetConnection())
            {
                connection.Open();

                var createTableQuery = connection.CreateCommand();

                createTableQuery.CommandText = tableQuery;
                createTableQuery.ExecuteNonQuery();
            }

            using (var connection = GetLegacyConnection())
            {
                connection.Open();

                var createTableQuery = connection.CreateCommand();

                createTableQuery.CommandText = legacyTableQuery;
                createTableQuery.ExecuteNonQuery();
            }

            DbCreated = true;
        }

        /// <summary>
        /// Clears the database from any items
        /// </summary>
        public static void ClearDatabase()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                var deleteQuery = connection.CreateCommand();
                deleteQuery.CommandText = "DELETE FROM Book; DELETE FROM Author; DELETE FROM Category; DELETE FROM Log; DELETE FROM Spaceship;";
                deleteQuery.ExecuteNonQuery();
            }

            using (var connection = GetLegacyConnection())
            {
                connection.Open();

                var deleteQuery = connection.CreateCommand();
                deleteQuery.CommandText = "DELETE FROM LegacyBook;";
                deleteQuery.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts a few test objects into the database
        /// </summary>
        public static async Task<bool> PopulateDatabase()
        {
            var bookRepository      = new EntityRepository<Book>(GetDatabaseFactory());
            var authorRepository    = new EntityRepository<Author>(GetDatabaseFactory());
            var shipRepository      = new EntityRepository<Spaceship>(GetDatabaseFactory());

            var authorHawking   = ModelHelper.GetAuthorModel(AuthorModelType.StephenHawking);
            var authorSagan     = ModelHelper.GetAuthorModel(AuthorModelType.CarlSagan);
            var authorWithoutBooks = ModelHelper.GetAuthorModel(AuthorModelType.AuthorWithoutBooks);

            var scienceCategory = ModelHelper.GetScienceCategory();

            var briefAnswersBook = ModelHelper.GetBookModel(BookModelType.BriefAnswers, scienceCategory, authorHawking);
            var briefHistoryBook = ModelHelper.GetBookModel(BookModelType.BriefHistoryOfTime, scienceCategory, authorHawking);

            var cosmosBook      = ModelHelper.GetBookModel(BookModelType.Cosmos, null, authorSagan);
            var paleBlueDotBook = ModelHelper.GetBookModel(BookModelType.PaleBlueDot, scienceCategory, authorSagan);

            var coBook = ModelHelper.GetBookModel(BookModelType.ScienceAnswered, scienceCategory, authorHawking, authorSagan);

            var andromedaShip = ModelHelper.GetSpaceshipModel(SpaceshipModelType.AndromedaLink);
            var galaxyShip    = ModelHelper.GetSpaceshipModel(SpaceshipModelType.GalaxyTraveller);

            await bookRepository.Insert(briefHistoryBook);
            await bookRepository.Insert(briefAnswersBook);

            await bookRepository.Insert(cosmosBook);
            await bookRepository.Insert(paleBlueDotBook);

            await bookRepository.Insert(coBook);

            await authorRepository.Insert(authorWithoutBooks);

            await shipRepository.Insert(andromedaShip);
            await shipRepository.Insert(galaxyShip);

            return true;
        }

        public static IDatabaseFactory GetCreationDatabaseFactory()
        {
            var dbBackend = Environment.GetEnvironmentVariable("DBBACKEND");
            DatabaseSettings databaseSettings = null;

            if (string.IsNullOrWhiteSpace(dbBackend))
                dbBackend = "sqlite";

            switch (dbBackend)
            {
                case "mysql":
                    databaseSettings = new DatabaseSettings()
                    {
                        Host = "extendeddappermysqltesting",
                        User = "root",
                        Password = "TestingPassword!",
                        DatabaseProvider = DatabaseProvider.MySQL
                    };
                    break;

                case "mssql":
                    databaseSettings = new DatabaseSettings()
                    {
                        Host = "extendeddappermssqltesting",
                        User = "SA",
                        Password = "TestingPassword!",
                        DatabaseProvider = DatabaseProvider.MSSQL
                    };
                    break;

                default:
                    throw new NotImplementedException($"Database backend {dbBackend} is not implemented");
            }

            return new DatabaseFactory(databaseSettings);
        }

        public static IDatabaseFactory GetDatabaseFactory()
        {
            var dbBackend = Environment.GetEnvironmentVariable("DBBACKEND");

            if (string.IsNullOrWhiteSpace(dbBackend))
                dbBackend = "sqlite";

            if (DatabaseFactory == null)
            {
                DatabaseSettings databaseSettings = null;

                switch (dbBackend)
                {
                    case "sqlite":
                        databaseSettings = new DatabaseSettings()
                        {
                            Database = "./test-db.db",
                            DatabaseProvider = DatabaseProvider.SQLite
                        };
                        break;

                    case "mysql":
                        databaseSettings = new DatabaseSettings()
                        {
                            Host = "extendeddappermysqltesting",
                            User = "root",
                            Password = "TestingPassword!",
                            Database = "testing",
                            DatabaseProvider = DatabaseProvider.MySQL
                        };
                        break;

                    case "mssql":
                        databaseSettings = new DatabaseSettings()
                        {
                            Host = "extendeddappermssqltesting",
                            User = "SA",
                            Password = "TestingPassword!",
                            Database = "testing",
                            DatabaseProvider = DatabaseProvider.MSSQL
                        };
                        break;

                    default:
                        throw new NotImplementedException($"Database backend {dbBackend} is not implemented");
                }

                DatabaseFactory = new DatabaseFactory(databaseSettings);
            }
            
            return DatabaseFactory;
        }

        public static IDatabaseFactory GetLegacyDatabaseFactory()
        {
            var dbBackend = Environment.GetEnvironmentVariable("DBBACKEND");

            if (string.IsNullOrWhiteSpace(dbBackend))
                dbBackend = "sqlite";

            if (LegacyDatabaseFactory == null)
            {
                DatabaseSettings databaseSettings = null;

                switch (dbBackend)
                {
                    case "sqlite":
                        databaseSettings = new DatabaseSettings()
                        {
                            Database = "./test-legacy-db.db",
                            DatabaseProvider = DatabaseProvider.SQLite
                        };
                        break;

                    case "mysql":
                        databaseSettings = new DatabaseSettings()
                        {
                            Host = "extendeddappermysqltesting",
                            User = "root",
                            Password = "TestingPassword!",
                            Database = "legacytesting",
                            DatabaseProvider = DatabaseProvider.MySQL
                        };
                        break;

                    case "mssql":
                        databaseSettings = new DatabaseSettings()
                        {
                            Host = "extendeddappermssqltesting",
                            User = "SA",
                            Password = "TestingPassword!",
                            Database = "legacytesting",
                            DatabaseProvider = DatabaseProvider.MSSQL
                        };
                        break;

                    default:
                        throw new NotImplementedException($"Legacy database backend {dbBackend} is not implemented");
                }

                LegacyDatabaseFactory = new DatabaseFactory(databaseSettings);
            }
            
            return LegacyDatabaseFactory;
        }

        private static IDbConnection GetConnection()
            => GetDatabaseFactory().GetDatabaseConnection();

        private static IDbConnection GetLegacyConnection()
            => GetLegacyDatabaseFactory().GetDatabaseConnection();

        private static IDbConnection GetCreationConnection()
            => GetCreationDatabaseFactory().GetDatabaseConnection();
    }
}