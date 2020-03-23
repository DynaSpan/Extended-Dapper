using System;
using System.Collections.Generic;
using System.Linq;
using Extended.Dapper.Tests.Helpers;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;

namespace Extended.Dapper.Tests.Repository
{
    [TestFixture]
    public class TestEntityRepositoryGetters : TestEntityRepository
    {
        [OneTimeSetUp]
        public override void FixtureSetUp()
        {
            base.FixtureSetUp();
            DatabaseHelper.ClearDatabase();
            DatabaseHelper.PopulateDatabase().Wait();
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

            this.TestIfAuthorIsValid(stephenHawking, AuthorModelType.StephenHawking);
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

            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime, true, true);
            this.TestIfAuthorIsValid(briefHistoryBook.Author, AuthorModelType.StephenHawking);

            Assert.AreNotEqual(null, briefHistoryBook.Category, "Could not retrieve the ManyToOne Category");
            Assert.AreEqual("Science", briefHistoryBook.Category.Name, "Book Category name is incorrect");
        }

        /// <summary>
        /// This tests if getting a single item from the db
        /// with their ManyToOne childrens works correctly when the include
        /// order is non-alphabetical
        /// </summary>
        [Test]
        public void TestGetSingleWithManyToOneChildrenNonAlphabetical()
        {
            var briefHistoryBook = BookRepository.Get(
                b => b.ReleaseYear == 1988, 
                b => b.Category,
                b => b.Author
            ).Result;

            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime, true, true);
            this.TestIfAuthorIsValid(briefHistoryBook.Author, AuthorModelType.StephenHawking);

            Assert.AreNotEqual(null, briefHistoryBook.Category, "Could not retrieve the ManyToOne Category");
            Assert.AreEqual("Science", briefHistoryBook.Category.Name, "Book Category name is incorrect");
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

            this.TestIfAuthorIsValid(stephenAuthor, AuthorModelType.StephenHawking);

            Assert.AreNotEqual(null, stephenAuthor.Books, "Could not retrieve the OneToMany Books");

            var briefHistoryBook = stephenAuthor.Books.SingleOrDefault(b => b.Name == "A Brief History of Time");
            var briefAnswersBook = stephenAuthor.Books.SingleOrDefault(b => b.ReleaseYear == 2018);

            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime);
            this.TestIfBookIsValid(briefAnswersBook, BookModelType.BriefAnswers);
        }

        /// <summary>
        /// This tests if getting a single item from the db
        /// with their OneToMany children works correctly, if
        /// it doesn't have any children
        /// </summary>
        [Test]
        public void TestGetSingleWithEmptyOneToManyChildren()
        {
            var authorWithoutBooks = AuthorRepository.Get(
                a => a.Name == "Author w/o Books",
                a => a.Books
            ).Result;

            this.TestIfAuthorIsValid(authorWithoutBooks, AuthorModelType.AuthorWithoutBooks);

            if (authorWithoutBooks.Books != null)
                Assert.AreEqual(0, authorWithoutBooks.Books.Count, "Empty object detected in child");
        }

        /// <summary>
        /// This tests if given a model which includes multiple children of the same object,
        /// the query and JOIN generation goes correctly (joins cant have the same table twice without aliassing)
        /// </summary>
        [Test]
        public void TestGetSingleWithMultipleJoinsFromSameTable()
        {
            var coBook = BookRepository.Get(b => b.Name == "Science questions answered", b => b.Author, b => b.CoAuthor).Result;

            this.TestIfBookIsValid(coBook, BookModelType.ScienceAnswered, false, true, true);

            this.TestIfAuthorIsValid(coBook.Author, AuthorModelType.StephenHawking);
            this.TestIfAuthorIsValid(coBook.CoAuthor, AuthorModelType.CarlSagan);
        }

        /// <summary>
        /// This test checks if you can search for entities while using another
        /// entity's ID as parameter. (e.g. retrieving all the books where the author is Stephen Hawking)
        /// </summary>
        [Test]
        public void TestSearchingWithForeignEntityIdAsParameter()
        {
            var stephenHawking = AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;

            this.TestIfAuthorIsValid(stephenHawking, AuthorModelType.StephenHawking);

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

            this.TestIfAuthorIsValid(stephenHawking, AuthorModelType.StephenHawking);

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

        /// <summary>
        /// This test should check if getting one child via the dedicated method works
        /// correctly
        /// </summary>
        [Test]
        public void TestGetOneChild()
        {
            // Grab book
            var book = BookRepository.Get(b => b.Name == "Science questions answered").Result;
            book.Author = BookRepository.GetOne<Author>(book, b => b.Author).Result;

            Assert.AreNotEqual(null, book.Author, "Could not retrieve child with GetOne<T>");
            this.TestIfAuthorIsValid(book.Author, AuthorModelType.StephenHawking);
        }

        /// <summary>
        /// This test should check if getting one child via the dedicated method works
        /// correctly
        /// </summary>
        [Test]
        public void TestGetOneChildWithIncludes()
        {
            // Grab book
            var book = BookRepository.Get(b => b.Name == "Science questions answered").Result;
            book.Author = BookRepository.GetOne<Author>(book, b => b.Author, a => a.Books).Result;

            Assert.AreNotEqual(null, book.Author, "Could not retrieve child with GetOne<T>");
            this.TestIfAuthorIsValid(book.Author, AuthorModelType.StephenHawking);

            Assert.AreNotEqual(null, book.Author.Books, "Could not retrieve many children of child with GetOne<T>");
            Assert.AreEqual(3, book.Author.Books.Count(), "Retrieved incorrect amount of children of child with GetOne<T>");

            var briefHistoryBook = book.Author.Books.SingleOrDefault(b => b.Name == "A Brief History of Time");
            var briefAnswersBook = book.Author.Books.SingleOrDefault(b => b.ReleaseYear == 2018);
            var scienceAnswersBook = book.Author.Books.SingleOrDefault(b => b.Name == "Science questions answered");
            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime);
            this.TestIfBookIsValid(briefAnswersBook, BookModelType.BriefAnswers);
            this.TestIfBookIsValid(scienceAnswersBook, BookModelType.ScienceAnswered);
        }

        /// <summary>
        /// This tests if getting by int id works properly
        /// </summary>
        [Test]
        public void TestGetByIntegerId()
        {
            var ships = this.SpaceshipRepository.GetAll().Result;
            var andromedaShipId = ships.Single(s => s.Name == "Andromeda Link").Id;

            Assert.AreNotEqual(default(int), andromedaShipId, "ID was not properly set on object");

            var andromedaShip = this.SpaceshipRepository.GetById(andromedaShipId).Result;

            Assert.AreNotEqual(null, andromedaShipId, "Could not get by integer id");
            Assert.AreEqual("Andromeda Link", andromedaShip.Name);
            Assert.AreEqual(2009, andromedaShip.BuildYear);
        }

        /// <summary>
        /// This tests if getting by GUID works properly
        /// </summary>
        [Test]
        public void TestGetByGuidId()
        {
            var books = this.BookRepository.GetAll().Result;
            var briefAnswerId = books.Single(b => b.Name == "Brief Answers to the Big Questions").Id;

            Assert.AreNotEqual(default(Guid), briefAnswerId, "ID was not properly set on object");

            var briefBook = this.BookRepository.GetById(briefAnswerId).Result;

            this.TestIfBookIsValid(briefBook, BookModelType.BriefAnswers);
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

            var carlAuthor = authors.SingleOrDefault(a => a.Name == "Carl Sagan");
            this.TestIfAuthorIsValid(carlAuthor, AuthorModelType.CarlSagan);
            Assert.AreNotEqual(null, carlAuthor.Books, "Could not retrieve Books of Author Carl Sagan");

            var cosmosVoyageBook = carlAuthor.Books.SingleOrDefault(b => b.Name == "Cosmos: A Personal Voyage");
            var paleBlueDotBook  = carlAuthor.Books.SingleOrDefault(b => b.ReleaseYear == 1994);
            this.TestIfBookIsValid(cosmosVoyageBook, BookModelType.Cosmos);
            this.TestIfBookIsValid(paleBlueDotBook, BookModelType.PaleBlueDot);

            var stephenAuthor = authors.SingleOrDefault(a => a.Name == "Stephen Hawking");
            this.TestIfAuthorIsValid(stephenAuthor, AuthorModelType.StephenHawking);
            Assert.AreNotEqual(null, stephenAuthor.Books, "Could not retrieve Books of Author Stephen Hawking");

            var briefHistoryBook = stephenAuthor.Books.SingleOrDefault(b => b.Name == "A Brief History of Time");
            var briefAnswersBook = stephenAuthor.Books.SingleOrDefault(b => b.ReleaseYear == 2018);
            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime);
            this.TestIfBookIsValid(briefAnswersBook, BookModelType.BriefAnswers);
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

            var carlAuthor = authors.SingleOrDefault(a => a.Name == "Carl Sagan");
            this.TestIfAuthorIsValid(carlAuthor, AuthorModelType.CarlSagan);
            Assert.AreNotEqual(null, carlAuthor.Books, "Could not retrieve Books of Author");

            carlAuthor.Books = AuthorRepository.GetMany<Book>(carlAuthor, a => a.Books, b => b.Category).Result as ICollection<Book>;
            Assert.AreNotEqual(null, carlAuthor.Books, "Could not retrieve Many Books of Author");

            var nullableCategoryBook = carlAuthor.Books.SingleOrDefault(b => b.Name == "Cosmos: A Personal Voyage");
            this.TestIfBookIsValid(nullableCategoryBook, BookModelType.Cosmos);
            Assert.AreEqual(null, nullableCategoryBook.Category, "The nullable Category is not null in Book");
        }

        /// <summary>
        /// This test should check if getting many children via the dedicated method works
        /// correctly
        /// </summary>
        [Test]
        public void TestGetManyChildren()
        {
            // Grab author
            var author = AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;
            author.Books = AuthorRepository.GetMany<Book>(author, a => a.Books).Result.ToList();

            Assert.AreNotEqual(null, author.Books, "Could not retrieve children with GetMany<T>");
            Assert.AreEqual(3, author.Books.Count(), "Retrieved incorrect amount of children with GetMany<T>");

            var briefHistoryBook = author.Books.SingleOrDefault(b => b.Name == "A Brief History of Time");
            var briefAnswersBook = author.Books.SingleOrDefault(b => b.ReleaseYear == 2018);
            var scienceAnswersBook = author.Books.SingleOrDefault(b => b.Name == "Science questions answered");
            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime);
            this.TestIfBookIsValid(briefAnswersBook, BookModelType.BriefAnswers);
            this.TestIfBookIsValid(scienceAnswersBook, BookModelType.ScienceAnswered);
        }

        /// <summary>
        /// This test should check if getting many children via the dedicated method works
        /// correctly when including children
        /// </summary>
        [Test]
        public void TestGetManyChildrenWithIncludes()
        {
            // Grab author
            var author = AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;
            author.Books = AuthorRepository.GetMany<Book>(author, a => a.Books, b => b.Category).Result.ToList();

            Assert.AreNotEqual(null, author.Books, "Could not retrieve children with GetMany<T>");
            Assert.AreEqual(3, author.Books.Count(), "Retrieved incorrect amount of children with GetMany<T>");

            var briefHistoryBook = author.Books.SingleOrDefault(b => b.Name == "A Brief History of Time");
            var briefAnswersBook = author.Books.SingleOrDefault(b => b.ReleaseYear == 2018);
            var scienceAnswersBook = author.Books.SingleOrDefault(b => b.Name == "Science questions answered");
            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime, true);
            this.TestIfBookIsValid(briefAnswersBook, BookModelType.BriefAnswers, true);
            this.TestIfBookIsValid(scienceAnswersBook, BookModelType.ScienceAnswered, true);
        }

        /// <summary>
        /// This test should check if getting many children via the dedicated method works
        /// correctly when including optional children
        /// </summary>
        [Test]
        public void TestGetManyChildrenWithOptionalIncludes()
        {
            // Grab author
            var author = AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;
            author.Books = AuthorRepository.GetMany<Book>(author, a => a.Books, b => b.Category, b => b.CoAuthor).Result.ToList();

            Assert.AreNotEqual(null, author.Books, "Could not retrieve children with GetMany<T>");
            Assert.AreEqual(3, author.Books.Count(), "Retrieved incorrect amount of children with GetMany<T>");

            var briefHistoryBook = author.Books.SingleOrDefault(b => b.Name == "A Brief History of Time");
            var briefAnswersBook = author.Books.SingleOrDefault(b => b.ReleaseYear == 2018);
            var scienceAnswersBook = author.Books.SingleOrDefault(b => b.Name == "Science questions answered");
            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime, true, false, true);
            this.TestIfBookIsValid(briefAnswersBook, BookModelType.BriefAnswers, true, false, true);
            this.TestIfBookIsValid(scienceAnswersBook, BookModelType.ScienceAnswered, true, false, true);
        }

        /// <summary>
        /// This test should check if getting many children via the dedicated method works
        /// correctly when including optional children of the same type
        /// </summary>
        [Test]
        public void TestGetManyChildrenWithOptionalIncludesOfSameType()
        {
            // Grab author
            var author = AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;
            author.Books = AuthorRepository.GetMany<Book>(author, a => a.Books, b => b.Category, b => b.Author, b => b.CoAuthor).Result.ToList();

            Assert.AreNotEqual(null, author.Books, "Could not retrieve children with GetMany<T>");
            Assert.AreEqual(3, author.Books.Count(), "Retrieved incorrect amount of children with GetMany<T>");

            var briefHistoryBook = author.Books.SingleOrDefault(b => b.Name == "A Brief History of Time");
            var briefAnswersBook = author.Books.SingleOrDefault(b => b.ReleaseYear == 2018);
            var scienceAnswersBook = author.Books.SingleOrDefault(b => b.Name == "Science questions answered");
            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime, true, true, true);
            this.TestIfBookIsValid(briefAnswersBook, BookModelType.BriefAnswers, true, true, true);
            this.TestIfBookIsValid(scienceAnswersBook, BookModelType.ScienceAnswered, true, true, true);
        }

        #endregion
    }
}