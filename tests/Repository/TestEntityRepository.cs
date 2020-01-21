using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Extended.Dapper.Core.Database.Entities;
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
        /// Tests if a given author is valid
        /// </summary>
        /// <param name="author"></param>
        /// <param name="authorType"></param>
        protected void TestIfAuthorIsValid(Author author, AuthorModelType authorType)
        {
            Assert.AreNotEqual(null, author, "Could not retrieve the Author");
            Assert.AreNotEqual(Guid.Empty, author.Id, "Author has invalid ID");

            Author testAuthor = ModelHelper.GetAuthorModel(authorType);
            
            Assert.AreEqual(testAuthor.Name,       author.Name, "Author name is incorrect");
            Assert.AreEqual(testAuthor.BirthYear,  author.BirthYear, "Author birth year is incorrect");
            Assert.AreEqual(testAuthor.Country,    author.Country, "Author country is incorrect");
        }

        /// <summary>
        /// Tests if a given book is valid
        /// </summary>
        /// <param name="book"></param>
        /// <param name="bookType"></param>
        /// <param name="includeCategory">Should category be included in testing?</param>
        /// <param name="includeAuthor">Should author be included in testing?</param>
        /// <param name="includeCoAuthor">Should co-author be included in testing?</param>
        protected void TestIfBookIsValid(Book book, BookModelType bookType, bool includeCategory = false,
            bool includeAuthor = false, bool includeCoAuthor = false)
        {
            Assert.AreNotEqual(null, book, "Could not retrieve the Book");
            Assert.AreNotEqual(Guid.Empty, book.Id, "Book has invalid ID");

            Category scienceCategory = null;

            var testBook = ModelHelper.GetBookModel(bookType);

            if (includeCategory)
                scienceCategory = ModelHelper.GetScienceCategory();

            Assert.AreEqual(testBook.Name,          book.Name, "Book name is incorrect");
            Assert.AreEqual(testBook.ReleaseYear,   book.ReleaseYear, "Book release year is incorrect");

            if (includeCategory)
            {
                if (book.Category == null || testBook.Category == null)
                    Assert.AreEqual(testBook.Category, book.Category);

                Assert.AreNotEqual(Guid.Empty, book.Category.Id);
                Assert.AreEqual(testBook.Category.Name, book.Category.Name);
                Assert.AreEqual(testBook.Category.Description, book.Category.Description);
            }

            if (includeAuthor)
                this.TestIfAuthorIsValid(book.Author, ModelHelper.GetAuthorModelFromBookModel(bookType));

            if (includeCoAuthor && bookType == BookModelType.ScienceAnswered)
                this.TestIfAuthorIsValid(book.CoAuthor, AuthorModelType.CarlSagan); // Coauthor is always Carl in tests
            else if (includeCoAuthor)
                Assert.AreEqual(null, book.CoAuthor, "Co Author was not empty on Book that doesn't have one");
        }

        public Task<Log> Log<T>(T obj, string action, IDbTransaction transaction = null)
            where T : BaseEntity
        {
            var userId = Guid.NewGuid();

            var logEntry = new Log()
            {
                Date = DateTime.UtcNow,
                SubjectId = obj.Id.ToString(),
                Action = action,
                UserId = userId
            };
            return this.LogRepository.Insert(logEntry, transaction);
        }
    }


}