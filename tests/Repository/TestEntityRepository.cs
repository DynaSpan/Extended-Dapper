using Extended.Dapper.Core.Repository;
using Extended.Dapper.Core.Sql.QueryProviders;
using Extended.Dapper.Tests.Helpers;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;

namespace Extended.Dapper.Tests.Repository
{
    [TestFixture]
    public class TestEntityRepository
    {
        private EntityRepository<Book> BookRepository { get; set; }
        private EntityRepository<Author> AuthorRepository { get; set; }

        [SetUp]
        public void Setup()
        {
            SqlQueryProviderHelper.Verbose = true;
            DatabaseHelper.CreateDatabase();
            DatabaseHelper.PopulateDatabase().Wait();

            BookRepository = new EntityRepository<Book>(DatabaseHelper.DatabaseFactory);
            AuthorRepository = new EntityRepository<Author>(DatabaseHelper.DatabaseFactory);
        }

        /// <summary>
        /// This tests if getting items from the DB
        /// (inserted by DatabaseHelper.PopulateDatabase())
        /// is working
        /// </summary>
        [Test]
        public void TestGet()
        {
            var stephenHawking = AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;

            Assert.AreNotEqual(null, stephenHawking);
            Assert.AreEqual("Stephen Hawking", stephenHawking.Name);
            Assert.AreEqual(1942, stephenHawking.BirthYear);
            Assert.AreEqual("United Kingdom", stephenHawking.Country);
        }

        /// <summary>
        /// This tests if getting items from the db
        /// with their ManyToOne childrens works correctly
        /// </summary>
        [Test]
        public void TestGetWithManyToOneChildren()
        {
            var briefHistoryBook = BookRepository.Get(
                b => b.ReleaseYear == 1988, 
                b => b.Author,
                b => b.Category
            ).Result;

            Assert.AreNotEqual(null, briefHistoryBook, "Could not retrieve the book");
            Assert.AreNotEqual(null, briefHistoryBook.Author, "Could not retrieve the ManyToOne Author");
            Assert.AreNotEqual(null, briefHistoryBook.Category, "Could not retrieve the ManyToOne Category");

            Assert.AreEqual("Stephen Hawking", briefHistoryBook.Author.Name);
            Assert.AreEqual("A Brief History of Time", briefHistoryBook.Name);
            Assert.AreEqual("Science", briefHistoryBook.Category.Name);
        }
    }
}