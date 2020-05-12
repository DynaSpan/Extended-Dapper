using System;
using System.Collections.Generic;
using System.Linq;
using Extended.Dapper.Tests.Helpers;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;

namespace Extended.Dapper.Tests.Repository
{
    [TestFixture]
    public class TestEntityRepositoryDeletes : TestEntityRepository
    {
        /// <summary>
        /// Clear and insert database before every test
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            DatabaseHelper.ClearDatabase();
            DatabaseHelper.PopulateDatabase().Wait();
        }
    }
}