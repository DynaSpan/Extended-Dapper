using System;
using System.Collections.Generic;
using System.Linq;
using Extended.Dapper.Core.Repository;
using Extended.Dapper.Core.Sql.QueryProviders;
using Extended.Dapper.Tests.Helpers;
using Extended.Dapper.Tests.Models;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Extended.Dapper.Tests.Repository
{
    [TestFixture]
    public class TestEntityRepositoryGetters
    {
        private EntityRepository<Book> BookRepository { get; set; }
        private EntityRepository<Author> AuthorRepository { get; set; }

        [OneTimeSetUpAttribute]
        public void FixtureSetUp()
        {
            SqlQueryProviderHelper.Verbose = true;
            DatabaseHelper.CreateDatabase();
            DatabaseHelper.PopulateDatabase().Wait();

            BookRepository = new EntityRepository<Book>(DatabaseHelper.GetDatabaseFactory());
            AuthorRepository = new EntityRepository<Author>(DatabaseHelper.GetDatabaseFactory());
        }

        #region Single items

        /// <summary>
        /// This tests if getting a single item from the DB
        /// (inserted by DatabaseHelper.PopulateDatabase())
        /// is working
        /// </summary>
        [Test]
        public void TestGetSingle()
        {
            var stephenHawking = AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;

            Assert.AreNotEqual(null, stephenHawking);
            Assert.AreNotEqual(Guid.Empty, stephenHawking.Id);
            Assert.AreEqual("Stephen Hawking", stephenHawking.Name);
            Assert.AreEqual(1942, stephenHawking.BirthYear);
            Assert.AreEqual("United Kingdom", stephenHawking.Country);
        }

        /// <summary>
        /// This tests if getting a single item from the db
        /// with their ManyToOne childrens works correctly
        /// </summary>
        [Test]
        public void TestGetSingleWithManyToOneChildren()
        {
            var briefHistoryBook = BookRepository.Get(
                b => b.ReleaseYear == 1988, 
                b => b.Author,
                b => b.Category
            ).Result;

            Console.WriteLine(JsonConvert.SerializeObject(briefHistoryBook));

            Assert.AreNotEqual(null, briefHistoryBook, "Could not retrieve the book");
            Assert.AreNotEqual(null, briefHistoryBook.Author, "Could not retrieve the ManyToOne Author");
            Assert.AreNotEqual(null, briefHistoryBook.Category, "Could not retrieve the ManyToOne Category");

            Assert.AreEqual("Stephen Hawking", briefHistoryBook.Author.Name);
            Assert.AreEqual("A Brief History of Time", briefHistoryBook.Name);
            Assert.AreEqual("Science", briefHistoryBook.Category.Name);
        }

        /// <summary>
        /// This tests if getting a single item from the db
        /// with their OneToMany children works correctly
        /// </summary>
        [Test]
        public void TestGetSingleWithOneToManyChildren()
        {
            var stephenAuthor = AuthorRepository.Get(
                a => a.Name == "Stephen Hawking",
                a => a.Books
            ).Result;

            Assert.AreNotEqual(null, stephenAuthor, "Could not retrieve the Author");
            Assert.AreNotEqual(null, stephenAuthor.Books, "Could not retrieve the OneToMany Books");

            var briefHistoryBook = stephenAuthor.Books.Where(b => b.Name == "A Brief History of Time").SingleOrDefault();
            var briefAnswersBook = stephenAuthor.Books.Where(b => b.ReleaseYear == 2018).SingleOrDefault();

            Assert.AreNotEqual(null, briefHistoryBook, "Could not retrieve OneToMany BriefHistoryBook");
            Assert.AreNotEqual(null, briefAnswersBook, "Could not retrieve OneTomany BriefAnswersBook");
        }

        /// <summary>
        /// This tests if given a model which includes multiple children of the same object,
        /// the query and JOIN generation goes correctly (joins cant have the same table twice without aliassing)
        /// </summary>
        [Test]
        public void TestGetSingleWithMultipleJoinsFromSameTable()
        {
            var coBook = BookRepository.Get(b => b.Name == "Science questions answered", b => b.Author, b => b.CoAuthor).Result;

            Assert.AreNotEqual(null, coBook, "Could not retrieve the Book");
            Assert.AreEqual("Science questions answered", coBook.Name);

            Assert.AreNotEqual(null, coBook.Author, "Could not retrieve the Author child");
            Assert.AreNotEqual(null, coBook.CoAuthor, "Could not retrieve the CoAuthor child");

            Assert.AreEqual("Stephen Hawking", coBook.Author.Name, "Incorrect Author retrieved from the Book");
            Assert.AreEqual("Carl Sagan", coBook.CoAuthor.Name, "Incorrect CoAuthor retrieved from the Book");
        }

        /// <summary>
        /// This test checks if you can search for entities while using another
        /// entity's ID as parameter. (e.g. retrieving all the books where the author is Stephen Hawking)
        /// </summary>
        [Test]
        public void TestSearchingWithForeignEntityIdAsParameter()
        {
            var stephenHawking = AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;

            // Grab all books written by Stephen
            var books = BookRepository.GetAll(b => b.Author.Id == stephenHawking.Id).Result;

            Assert.AreNotEqual(null, books, "Could not retrieve books by Author Id");
            Assert.AreEqual(3, books.Count(), "Could not retrieve the correct books by Author Id");
        }

        /// <summary>
        /// This test checks if you can search for entities while using another
        /// entity as parameter. (e.g. retrieving all the books where the author is Stephen Hawking)
        /// </summary>
        [Test]
        public void TestSearchingWithForeignEntityAsParameter()
        {
            var stephenHawking = AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;

            // Grab all books written by Stephen
            var books = BookRepository.GetAll(b => b.Author == stephenHawking).Result;

            Assert.AreNotEqual(null, books, "Could not retrieve books by Author Entity");
            Assert.AreEqual(3, books.Count(), "Could not retrieve the correct books by Author Entity");
        }

        /// <summary>
        /// This test checks if you can search for entities while using another
        /// entity's property as parameter. (e.g. retrieving all the books where the author is born in 1934)
        /// </summary>
        [Test]
        public void TestSearchingWithForeignEntityPropertyAsParameter()
        {
            // Grab all books written by Carl Sagan (born in 1934)
            var books = BookRepository.GetAll(b => b.Author.BirthYear == 1934, b => b.Author).Result;

            Assert.AreNotEqual(null, books, "Could not retrieve books by Author BirthYear property");
            Assert.AreEqual(2, books.Count(), "Could not retrieve the correct books by Author BirthYear property");
        }

        #endregion

        #region Multiple items

        /// <summary>
        /// This tests if getting a single item from the DB
        /// (inserted by DatabaseHelper.PopulateDatabase())
        /// is working
        /// </summary>
        [Test]
        public void TestGetAll()
        {
            var authors = AuthorRepository.GetAll(a => a.Books).Result;
            Assert.AreNotEqual(null, authors, "Could not retrieve Authors");

            var carlAuthor = authors.Where(a => a.Name == "Carl Sagan").SingleOrDefault();
            Assert.AreNotEqual(null, carlAuthor, "Could not retrieve Author Carl Sagan");
            Assert.AreNotEqual(null, carlAuthor.Books, "Could not retrieve Books of Author Carl Sagan");

            var cosmosVoyageBook = carlAuthor.Books.Where(b => b.Name == "Cosmos: A Personal Voyage").SingleOrDefault();
            var paleBlueDotBook  = carlAuthor.Books.Where(b => b.ReleaseYear == 1994).SingleOrDefault();
            Assert.AreNotEqual(null, cosmosVoyageBook, "The Cosmos Voyage Book of Carl Sagan could not be found");
            Assert.AreNotEqual(null, paleBlueDotBook, "The Pale Blue Dot Book of Carl Sagan could not be found");

            var stephenAuthor = authors.Where(a => a.Name == "Stephen Hawking").SingleOrDefault();
            Assert.AreNotEqual(null, stephenAuthor, "Could not retrieve Author Stephen Hawking");
            Assert.AreNotEqual(null, stephenAuthor.Books, "Could not retrieve Books of Author Stephen Hawking");

            var briefHistoryBook = stephenAuthor.Books.Where(b => b.Name == "A Brief History of Time").SingleOrDefault();
            var briefAnswersBook = stephenAuthor.Books.Where(b => b.ReleaseYear == 2018).SingleOrDefault();
            Assert.AreNotEqual(null, briefHistoryBook, "The Brief History of Time Book of Stephen Hawking could not be found");
            Assert.AreNotEqual(null, briefAnswersBook, "The Brief Answers Book of Stephen Hawking could not be found");
        }

        /// <summary>
        /// This tests if the nullable ManyToOne Category
        /// on the Book is truely nullable
        /// </summary>
        [Test]
        public void TestNullableRelations() 
        {
            var authors = AuthorRepository.GetAll(a => a.Books).Result;
            Assert.AreNotEqual(null, authors, "Could not retrieve Authors");

            var carlAuthor = authors.Where(a => a.Name == "Carl Sagan").SingleOrDefault();
            Assert.AreNotEqual(null, carlAuthor, "Could not retrieve Author");
            Assert.AreNotEqual(null, carlAuthor.Books, "Could not retrieve Books of Author");

            carlAuthor.Books = AuthorRepository.GetMany<Book>(carlAuthor, a => a.Books, b => b.Category).Result as ICollection<Book>;
            Assert.AreNotEqual(null, carlAuthor.Books, "Could not retrieve Many Books of Author");

            var nullableCategoryBook = carlAuthor.Books.Where(b => b.Name == "Cosmos: A Personal Voyage").SingleOrDefault();
            Assert.AreNotEqual(null, nullableCategoryBook, "The Book with the nullable Category could not be found");
            Assert.AreEqual(null, nullableCategoryBook.Category, "The nullable Category is not null in Book");
        }

        #endregion
    }
}