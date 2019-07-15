using System.Data;
using System.IO;
using System.Threading.Tasks;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Repository;
using Extended.Dapper.Tests.Models;

namespace Extended.Dapper.Tests.Helpers
{
    public class DatabaseHelper
    {
        private static IDatabaseFactory DatabaseFactory { get; set; }

        /// <summary>
        /// Creates the database in the SQLite file
        /// </summary>
        public static void CreateDatabase()
        {
            // Delete database first
            File.Delete("./test-db.db");

            using (var connection = GetConnection())
            {
                connection.Open();

                var createQuery = connection.CreateCommand();

                switch (GetDatabaseFactory().DatabaseProvider)
                {
                    case DatabaseProvider.MSSQL:
                        createQuery.CommandText = File.ReadAllText("./test-db.mssql"); break;
                    case DatabaseProvider.MySQL:
                        createQuery.CommandText = File.ReadAllText("./test-db.mysql"); break;
                    default:
                        createQuery.CommandText = File.ReadAllText("./test-db.sql"); break;
                }

                
                createQuery.ExecuteNonQuery();
            }
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
                deleteQuery.CommandText = "DELETE FROM Book; DELETE FROM Author; DELETE FROM Category";
                deleteQuery.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts a few test objects into the database
        /// </summary>
        public static async Task<bool> PopulateDatabase()
        {
            var bookRepository = new EntityRepository<Book>(GetDatabaseFactory());
            var authorRepository = new EntityRepository<Author>(GetDatabaseFactory());

            var authorHawking = new Author() {
                Name = "Stephen Hawking",
                BirthYear = 1942,
                Country = "United Kingdom"
            };
            var authorSagan = new Author() {
                Name = "Carl Sagan",
                BirthYear = 1934,
                Country = "United States"
            };

            var scienceCategory = new Category() {
                Name = "Science",
                Description = "All kinds of books with science"
            };

            var briefHistoryBook = new Book() {
                Name = "A Brief History of Time",
                ReleaseYear = 1988,
                Author = authorHawking,
                Category = scienceCategory
            };
            var briefAnswersBook = new Book() {
                Name = "Brief Answers to the Big Questions",
                ReleaseYear = 2018,
                Author = authorHawking,
                Category = scienceCategory
            };

            var cosmosBook = new Book() {
                Name = "Cosmos: A Personal Voyage",
                ReleaseYear = 1980,
                Author = authorSagan
            };
            var paleBlueDotBook = new Book() {
                Name = "Pale Blue Dot: A Vision of the Human Future in Space",
                ReleaseYear = 1994,
                Author = authorSagan,
                Category = scienceCategory
            };

            var coBook = new Book() {
                Name = "Science questions answered",
                ReleaseYear = 2015,
                Author = authorHawking,
                CoAuthor = authorSagan,
                Category = scienceCategory
            };

            await bookRepository.Insert(briefHistoryBook);
            await bookRepository.Insert(briefAnswersBook);

            await bookRepository.Insert(cosmosBook);
            await bookRepository.Insert(paleBlueDotBook);

            await bookRepository.Insert(coBook);

            return true;
        }

        public static IDatabaseFactory GetDatabaseFactory()
        {
            if (DatabaseFactory == null)
            {
                var databaseSettings = new DatabaseSettings()
                {
                    Host = "172.20.0.10",
                    User = "dapper",
                    Password = "extended-dapper-sql-password",
                    Database = "dapper",
                    DatabaseProvider = DatabaseProvider.MSSQL
                };

                DatabaseFactory = new DatabaseFactory(databaseSettings);
            }
            
            return DatabaseFactory;
        }

        private static IDbConnection GetConnection()
        {
            return GetDatabaseFactory().GetDatabaseConnection();
        }
    }
}