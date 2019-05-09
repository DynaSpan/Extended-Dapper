using System;
using System.Linq;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.EntityRepository;
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
            SqlQueryProviderHelper.SetProvider(DatabaseProvider.MySQL);
            var sqlGenerator = new SqlGenerator(DatabaseProvider.MySQL);

            var databaseSettings = new DatabaseSettings()
            {
                Host = "172.18.0.5",
                User = "dapper",
                Password = "extended-dapper-sql-password",
                Database = "dapper",
                DatabaseProvider = DatabaseProvider.MySQL
            };
            var databaseFactory = new DatabaseFactory(databaseSettings);
            
            var bookEntityRepository = new EntityRepository<Book>(databaseFactory);
            var authorEntityRepository = new EntityRepository<Author>(databaseFactory);

            ReflectionHelper.GetTypeListFromIncludes<Book>((book, user) => { book.Author = user; return book; });

            var book = (bookEntityRepository.Get(b => b.Name == "1984 - second edition", b => b.Author).Result);

            //Console.WriteLine(book);

            // Console.WriteLine(sqlGenerator.Select<Book>());
            // Console.WriteLine(sqlGenerator.Select<Book>(b => b.Name == "Test"));
            // Console.WriteLine(sqlGenerator.Select<Author>());
            // Console.WriteLine(sqlGenerator.Select<Author>(a => a.Country == "NL" || a.Country == "BE"));
        }
    }
}
