using System.Data;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Repository;
using Extended.Dapper.Tests.Models;

namespace Extended.Dapper.Tests.Helpers
{
    public class DatabaseHelper
    {
        private static bool DbCleared { get; set; } = false;
        private static IDatabaseFactory DatabaseFactory { get; set; }

        /// <summary>
        /// Creates the database in the SQLite file
        /// </summary>
        public static void CreateDatabase()
        {
            // Delete database first
            if (DbCleared)
                return;

            File.Delete("./test-db.db");
            DbCleared = true;

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

            var authorHawking   = ModelHelper.GetAuthorModel(AuthorModelType.StephenHawking);
            var authorSagan     = ModelHelper.GetAuthorModel(AuthorModelType.CarlSagan);

            var scienceCategory = ModelHelper.GetScienceCategory();

            var briefAnswersBook = ModelHelper.GetBookModel(BookModelType.BriefAnswers, scienceCategory, authorHawking);
            var briefHistoryBook = ModelHelper.GetBookModel(BookModelType.BriefHistoryOfTime, scienceCategory, authorHawking);
            

            var cosmosBook      = ModelHelper.GetBookModel(BookModelType.Cosmos, null, authorSagan);
            var paleBlueDotBook = ModelHelper.GetBookModel(BookModelType.PaleBlueDot, scienceCategory, authorSagan);

            var coBook = ModelHelper.GetBookModel(BookModelType.ScienceAnswered, scienceCategory, authorHawking, authorSagan);

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
                    Database = "./test-db.db",
                    DatabaseProvider = DatabaseProvider.SQLite
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