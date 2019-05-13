using System;
using System.Linq;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Repository;
using Extended.Dapper.Core.Reflection;
using Extended.Dapper.Core.Sql;
using Extended.Dapper.Core.Sql.QueryProviders;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;

namespace Extended.Dapper.Tests.Query
{
    [TestFixture]
    public class TestSelectQuery
    {
        /// <summary>
        /// Currently used for literal testing, not unittesting
        /// </summary>
        [Test]
        public void TestModelMapping()
        {
            SqlQueryProviderHelper.SetProvider(DatabaseProvider.MSSQL);
            var sqlGenerator = new SqlGenerator(DatabaseProvider.MSSQL);

            var databaseSettings = new DatabaseSettings()
            {
                Host = "172.20.0.10",
                User = "dapper",
                Password = "extended-dapper-sql-password",
                Database = "dapper",
                DatabaseProvider = DatabaseProvider.MSSQL
            };
            var databaseFactory = new DatabaseFactory(databaseSettings);
            
            var bookEntityRepository = new EntityRepository<Book>(databaseFactory);
            var authorEntityRepository = new EntityRepository<Author>(databaseFactory);

            //ReflectionHelper.GetTypeListFromIncludes<Book>((book, user) => { book.Author = user; return book; });

            var books = (bookEntityRepository.Get(null, b => b.Author).Result);

            foreach (var book in books)
            {
                Console.WriteLine(book);
            }

            var authors = (authorEntityRepository.Get(null, a => a.Books).Result);

            foreach (var author in authors)
            {
                Console.WriteLine(author);
            }

            // Console.WriteLine(sqlGenerator.Select<Book>());
            // Console.WriteLine(sqlGenerator.Select<Book>(b => b.Name == "Test"));
            // Console.WriteLine(sqlGenerator.Select<Author>());
            // Console.WriteLine(sqlGenerator.Select<Author>(a => a.Country == "NL" || a.Country == "BE"));
        }
    }
}
