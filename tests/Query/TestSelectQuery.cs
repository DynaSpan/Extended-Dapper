using System;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Sql;
using Extended.Dapper.Core.Sql.QueryProviders;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;

namespace Extended.Dapper.Tests.Query
{
    [TestFixture]
    public class TestSelectQuery
    {
        [Test]
        public void TestModelMapping()
        {
            SqlQueryProviderHelper.SetProvider(DatabaseProvider.MSSQL);
            var sqlGenerator = new SqlGenerator(DatabaseProvider.MSSQL);

            Console.WriteLine(sqlGenerator.Select<Book>(null));
            Console.WriteLine(sqlGenerator.Select<Book>(b => b.Name == "Test"));
            Console.WriteLine(sqlGenerator.Select<Author>(null));
            Console.WriteLine(sqlGenerator.Select<Author>(a => a.Country == "NL"));
        }
    }
}
