using System;
using System.Linq;
using NUnit.Framework;
using Extended.Dapper.Tests.Models;
using Extended.Dapper.Tests.Helpers;
using Extended.Dapper.Core.Sql.QueryProviders;
using Extended.Dapper.Core.Repository;

namespace Extended.Dapper.Tests.EntityMapping
{
    /// <summary>
    /// This class tests if the mapping of model atrributes works correctly
    /// </summary>
    [TestFixture]
    public class TestAttributeMapping
    {
        protected EntityRepository<Book> BookRepository { get; set; }
        protected EntityRepository<Author> AuthorRepository { get; set; }
        protected EntityRepository<Category> CategoryRepository { get; set; }
        protected EntityRepository<Log> LogRepository { get; set; }

        [OneTimeSetUp]
        public virtual void FixtureSetUp()
        {
            SqlQueryProviderHelper.Verbose = true;
            DatabaseHelper.CreateDatabase();

            BookRepository = new EntityRepository<Book>(DatabaseHelper.GetDatabaseFactory());
            AuthorRepository = new EntityRepository<Author>(DatabaseHelper.GetDatabaseFactory());
            CategoryRepository = new EntityRepository<Category>(DatabaseHelper.GetDatabaseFactory());
            LogRepository = new EntityRepository<Log>(DatabaseHelper.GetDatabaseFactory());
        }

        /// <summary>
        /// Clear database before every test
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            DatabaseHelper.ClearDatabase();
        }

        /// <summary>
        /// This tests if the [IgnoreOnUpdate] works properly
        /// </summary>
        [Test]
        public void TestIfIgnoreOnUpdateWorks()
        {
            var cosmosBook       = ModelHelper.GetBookModel(BookModelType.Cosmos, null);
            var cosmosBookEntity = this.BookRepository.Insert(cosmosBook).Result;

            int bookCount = this.BookRepository.GetAll().Result.Count();

            Assert.AreEqual(1, bookCount, "Book was not correctly inserted into database");

            cosmosBookEntity.OriginalName = "Test";

            var updateResult = this.BookRepository.Update(cosmosBookEntity).Result;
            var cosmos = this.BookRepository.GetById(cosmosBookEntity.Id).Result;

            Assert.AreEqual(true, updateResult, "Updating the Book failed");
            Assert.AreEqual(null, cosmos.OriginalName, "Updated the original name field while IgnoreOnUpdate");
        }

        /// <summary>
        /// This tests if the [IgnoreOnInsert] works properly
        /// </summary>
        [Test]
        public void TestIfIgnoreOnInsertWorks()
        {
            var category = new Category()
            {
                Name = "Romance",
                EditedBy = "DynaSpan" // [IgnoreOnInsert]
            };

            var categoryEntity = this.CategoryRepository.Insert(category).Result;
            var romanceCategory = this.CategoryRepository.GetById(categoryEntity.Id).Result;

            Assert.AreEqual(null, romanceCategory.EditedBy, "Inserted a [IgnoreOnInsert] property");
        }

        /// <summary>
        /// Tests if AutoValues work if they're not a key
        /// </summary>
        [Test]
        public void TestIfNonKeyAutoValueWorks()
        {
            var cosmosBook       = ModelHelper.GetBookModel(BookModelType.Cosmos, null);
            var cosmosBookEntity = this.BookRepository.Insert(cosmosBook).Result;

            var answersBook       = ModelHelper.GetBookModel(BookModelType.BriefHistoryOfTime, null);
            var answersBookEntity = this.BookRepository.Insert(answersBook).Result;

            Assert.AreNotEqual(null, cosmosBookEntity.ExternalId, "Did not fill non-key AutoValue");
            Assert.AreNotEqual(Guid.Empty, cosmosBookEntity.ExternalId, "Did not fill non-key AutoValue");

            Assert.AreNotEqual(null, answersBook.ExternalId, "Did not fill non-key AutoValue");
            Assert.AreNotEqual(Guid.Empty, answersBookEntity.ExternalId, "Did not fill non-key AutoValue");

            Assert.AreNotEqual(cosmosBookEntity.ExternalId, answersBookEntity.ExternalId, "Did not fill non-key AutoValue");

            var cosmosBookByExternalId = this.BookRepository.Get(b => b.ExternalId == cosmosBookEntity.ExternalId).Result;
            var answersBookByExternalid = this.BookRepository.Get(b => b.ExternalId == answersBookEntity.ExternalId).Result;

            Assert.AreNotEqual(null, cosmosBookByExternalId, "Could not retrieve child by auto value");
            Assert.AreNotEqual(null, answersBookByExternalid, "Could not retrieve child by auto value");

            Assert.AreNotEqual(null, cosmosBookByExternalId.ExternalId, "Did not fill non-key AutoValue");
            Assert.AreNotEqual(Guid.Empty, cosmosBookByExternalId.ExternalId, "Did not fill non-key AutoValue");

            Assert.AreNotEqual(null, answersBookByExternalid.ExternalId, "Did not fill non-key AutoValue");
            Assert.AreNotEqual(Guid.Empty, answersBookByExternalid.ExternalId, "Did not fill non-key AutoValue");
        }
    }
}