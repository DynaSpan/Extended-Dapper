using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Extended.Dapper.Tests.Helpers;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;

namespace Extended.Dapper.Tests.Repository
{
    [TestFixture]
    public class TestEntityRepositoryUpdates : TestEntityRepository
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

        /// <summary>
        /// This tests if updating an entity
        /// without its children works as expected
        /// </summary>
        [Test]
        public void TestUpdate()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;
            stephenAuthor.Country = "United States";

            this.AuthorRepository.Update(stephenAuthor).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(3, stephenWithBooks.Books.Count, "Books have been changed by update");
            Assert.AreEqual("United States", stephenWithBooks.Country, "Country has not been updated properly");
        }

        /// <summary>
        /// This tests if updating an entity
        /// without its children works as expected
        /// </summary>
        [Test]
        public void TestUpdateWithAlternativeKey()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;
            stephenAuthor.Id      = default(int);
            stephenAuthor.Country = "United States";

            this.AuthorRepository.Update(stephenAuthor).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(3, stephenWithBooks.Books.Count, "Books have been changed by update");
            Assert.AreEqual("United States", stephenWithBooks.Country, "Country has not been updated properly");
        }

        /// <summary>
        /// This tests if updating an entity
        /// without its children works as expected,
        /// even if the children have changed; they should
        /// not be updated if they are not included in the update
        /// </summary>
        [Test]
        public void TestUpdateWithoutChildren()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            var briefHistoryBook = stephenAuthor.Books.Single(b => b.Name == "A Brief History of Time");
            briefHistoryBook.Name = "Small History of Time";

            this.AuthorRepository.Update(stephenAuthor).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(3, stephenWithBooks.Books.Count, "Books have been changed by update");

            briefHistoryBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "A Brief History of Time");
            Assert.AreNotEqual(null, briefHistoryBook, "Book has been updated though it wasn't included");
        }

        /// <summary>
        /// This tests if updating an entity
        /// with its children works as expected
        /// </summary>
        [Test]
        public void TestUpdateWithChildren()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            var briefHistoryBook = stephenAuthor.Books.Single(b => b.Name == "A Brief History of Time");
            briefHistoryBook.Name = "Small History of Time";

            this.AuthorRepository.Update(stephenAuthor, a => a.Books).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(3, stephenWithBooks.Books.Count, "Books have been changed by update");

            briefHistoryBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "Small History of Time");
            Assert.AreNotEqual(null, briefHistoryBook, "Book has not been updated though it was included");
        }

        /// <summary>
        /// This test checks if multiple children in updates works correctly without an parameter
        /// count mismatch
        /// </summary>
        [Test]
        public void TestUpdateWithMultipleChildren() 
        {
            var book = this.BookRepository.Get(b => b.Name == "Brief Answers to the Big Questions").Result;
            Assert.AreNotEqual(null, book, "Book was null");

            var author = this.AuthorRepository.Get(a => a.Name == "Carl Sagan").Result;
            Assert.AreNotEqual(null, author, "Author was null");

            var category = this.CategoryRepository.Get(c => c.Name == "Science").Result;
            Assert.AreNotEqual(null, category, "Category was null");

            book.Author = author;
            book.Category = category;

            var updateResult = this.BookRepository.Update(book, b => b.Author, b => b.Category).Result;

            Assert.AreEqual(true, updateResult, "Could not update book with multiple children");
        }

        /// <summary>
        /// This tests if updating children that already exists (i.e. have an id)
        /// works properly when mixed with new children that don't have an id
        /// </summary>
        [Test]
        public void TestUpdateWithExistingAndNewChildren()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            var briefHistoryBook = stephenAuthor.Books.Single(b => b.Name == "A Brief History of Time");
            briefHistoryBook.Name = "Small History of Time";

            // Create a new book
            Book newBook = new Book()
            {
                Author = stephenAuthor,
                Name = "The Grand Design",
                ReleaseYear = 2010
            };
            stephenAuthor.Books.Add(newBook);

            this.AuthorRepository.Update(stephenAuthor, a => a.Books).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(4, stephenWithBooks.Books.Count, "Books have not been saved properly through update");

            briefHistoryBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "Small History of Time");
            var grandDesignBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "The Grand Design");
            Assert.AreNotEqual(null, briefHistoryBook, "Book has not been updated though it was included");
            Assert.AreNotEqual(null, grandDesignBook, "The newly inserted child could not be found");
        }

        /// <summary>
        /// This tests if updating children that already exists (i.e. have an id)
        /// works properly when mixed with new children that don't have an id
        /// </summary>
        [Test]
        public void TestUpdateWithExistingKeylessChildren()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;

            foreach (var book in stephenAuthor.Books)
                book.Id = default(int);

            // Create a new book
            Book newBook = new Book()
            {
                Author = stephenAuthor,
                Name = "The Grand Design",
                ReleaseYear = 2010
            };
            stephenAuthor.Books.Add(newBook);

            this.AuthorRepository.Update(stephenAuthor, a => a.Books).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(4, stephenWithBooks.Books.Count, "Books have not been saved properly through update");

            var grandDesignBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "The Grand Design");
            Assert.AreNotEqual(null, grandDesignBook, "The newly inserted child could not be found");
        }

        /// <summary>
        /// This tests if updating children when a child is removed
        /// indeeds remove the child from the database
        /// </summary>
        [Test]
        public void TestUpdateWithRemovingChildren()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            var briefHistoryBook = stephenAuthor.Books.Single(b => b.Name == "A Brief History of Time");
            
            // Remove book
            stephenAuthor.Books.Remove(briefHistoryBook);

            this.AuthorRepository.Update(stephenAuthor, a => a.Books).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(2, stephenWithBooks.Books.Count, "Books have not been saved properly through update");
        }

        /// <summary>
        /// This tests if updating an entity
        /// with its alternative key children works as expected
        /// </summary>
        [Test]
        public void TestUpdateWithAlternativeKeyChildren()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            var briefHistoryBook = stephenAuthor.Books.Single(b => b.Name == "A Brief History of Time");

            briefHistoryBook.Name = "Small History of Time";
            briefHistoryBook.Id   = default(int);

            this.AuthorRepository.Update(stephenAuthor, a => a.Books).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(3, stephenWithBooks.Books.Count, "Books have been changed by update");

            briefHistoryBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "Small History of Time");
            Assert.AreNotEqual(null, briefHistoryBook, "Book has not been updated though it was included");
        }

        /// <summary>
        /// This test checks if multiple children in updates works correctly when alternative
        /// keys are used
        /// </summary>
        [Test]
        public void TestUpdateWithMultipleAlternativeKeyChildren() 
        {
            var book = this.BookRepository.Get(b => b.Name == "Brief Answers to the Big Questions").Result;
            Assert.AreNotEqual(null, book, "Book was null");

            var author = this.AuthorRepository.Get(a => a.Name == "Carl Sagan").Result;
            Assert.AreNotEqual(null, author, "Author was null");

            var category = this.CategoryRepository.Get(c => c.Name == "Science").Result;
            Assert.AreNotEqual(null, category, "Category was null");

            book.Author     = author;
            book.Category   = category;
            book.Author.Id  = default(int);

            var updateResult = this.BookRepository.Update(book, b => b.Author, b => b.Category).Result;

            Assert.AreEqual(true, updateResult, "Could not update book with multiple children");

            var authorCount = this.AuthorRepository.GetAll().Result.Count();

            Assert.AreEqual(3, authorCount, "Inserted new author; alternative key not honored");
        }

        /// <summary>
        /// This tests if updating children that already exists (i.e. have an (alternative) id)
        /// works properly when mixed with new children that don't have an id
        /// </summary>
        [Test]
        public void TestUpdateWithExistingAndNewAlternativeKeyChildren()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            var briefHistoryBook = stephenAuthor.Books.Single(b => b.Name == "A Brief History of Time");
            briefHistoryBook.Name = "Small History of Time";
            briefHistoryBook.Id   = default(int);

            // Create a new book
            Book newBook = new Book()
            {
                Author = stephenAuthor,
                Name = "The Grand Design",
                ReleaseYear = 2010
            };
            stephenAuthor.Books.Add(newBook);

            this.AuthorRepository.Update(stephenAuthor, a => a.Books).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(4, stephenWithBooks.Books.Count, "Books have not been saved properly through update");

            briefHistoryBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "Small History of Time");
            var grandDesignBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "The Grand Design");
            Assert.AreNotEqual(null, briefHistoryBook, "Book has not been updated though it was included");
            Assert.AreNotEqual(null, grandDesignBook, "The newly inserted child could not be found");
        }

        /// <summary>
        /// This tests if updating children that already exists (i.e. have an id)
        /// works properly when mixed with new children that don't have an id, with a parent
        /// that has a alternative key
        /// </summary>
        [Test]
        public void TestUpdateWithExistingAndNewChildrenAndAlternativeKeyParent()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            stephenAuthor.Id  = default(int);
            
            var briefHistoryBook = stephenAuthor.Books.Single(b => b.Name == "A Brief History of Time");
            briefHistoryBook.Name = "Small History of Time";

            // Create a new book
            Book newBook = new Book()
            {
                Author = stephenAuthor,
                Name = "The Grand Design",
                ReleaseYear = 2010
            };
            stephenAuthor.Books.Add(newBook);

            this.AuthorRepository.Update(stephenAuthor, a => a.Books).Wait();

            // Check if no new author has been added
            var authorCount = this.AuthorRepository.GetAll(a => a.Name == "Stephen Hawking").Result.Count();
            Assert.AreEqual(1, authorCount, "New parent has been inserted while parent had alternative key");

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(4, stephenWithBooks.Books.Count, "Books have not been saved properly through update");

            briefHistoryBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "Small History of Time");
            var grandDesignBook = stephenWithBooks.Books.SingleOrDefault(b => b.Name == "The Grand Design");
            Assert.AreNotEqual(null, briefHistoryBook, "Book has not been updated though it was included");
            Assert.AreNotEqual(null, grandDesignBook, "The newly inserted child could not be found");
        }

        /// <summary>
        /// This tests if updating children when a child is removed
        /// indeeds remove the child from the database
        /// </summary>
        [Test]
        public void TestUpdateWithRemovingAlternativeKeyChildren()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            var briefHistoryBook = stephenAuthor.Books.Single(b => b.Name == "A Brief History of Time");
            briefHistoryBook.Id  = default(int);

            // Remove book
            stephenAuthor.Books.Remove(briefHistoryBook);

            this.AuthorRepository.Update(stephenAuthor, a => a.Books).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(2, stephenWithBooks.Books.Count, "Books have not been saved properly through update");
        }

        /// <summary>
        /// Tests if the IgnoreOnUpdate property works as intended
        /// </summary>
        [Test]
        public void TestIgnoreOnUpdate()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            var newBook = new Book()
            {
                Name = "Auto-Biography of Stephen Hawking",
                Author = stephenAuthor,
                ReleaseYear = 2018,
                OriginalName = "Auto-Biography of Stephen Hawking",
            };

            var newBookEntity = this.BookRepository.Insert(newBook).Result;

            // Update name and originalName (IgnoreOnUpdate)
            newBookEntity.Name          = "The Auto-Biography of Stephen Hawking";
            newBookEntity.OriginalName  = "The Auto-Biography of Stephen Hawking";

            this.BookRepository.Update(newBookEntity).Wait();
            var updatedBookEntity = this.BookRepository.Get(b => b.Name == "The Auto-Biography of Stephen Hawking").Result;

            Assert.AreEqual("Auto-Biography of Stephen Hawking", updatedBookEntity.OriginalName, "IgnoreOnUpdate property was ignored in Book");
        }

        /// <summary>
        /// Tests if the UpdateOnly method works as intended
        /// </summary>
        [Test]
        public void TestUpdateOnly()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;

            stephenAuthor.Name = "Stephen Birding";
            stephenAuthor.BirthYear = 9999;
            stephenAuthor.Country = "USA";

            var updateResult = this.AuthorRepository.UpdateOnly(stephenAuthor, a => a.Name, a => a.BirthYear).Result;

            Assert.AreEqual(true, updateResult, "Could not update some properties on Author");

            var newStephen = this.AuthorRepository.Get(a => a.Name == "Stephen Birding").Result;

            Assert.AreNotEqual(null, newStephen, "Could not find updated Author");
            Assert.AreEqual(9999, newStephen.BirthYear, "Could not update property");
            Assert.AreNotEqual("USA", newStephen.Country, "Property was updated even though it was excluded");
        }

        /// <summary>
        /// Tests if updating with transactions is working properly
        /// </summary>
        [Test]
        public void TestUpdateWithTransaction()
        {
            IDbConnection connection = DatabaseHelper.GetDatabaseFactory().GetDatabaseConnection();
            connection.Open();

            IDbTransaction transaction = connection.BeginTransaction();

            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            stephenAuthor.Country = "UK";

            this.AuthorRepository.Update(stephenAuthor, transaction).Wait();

            var newBook = new Book()
            {
                Name = "Auto-Biography of Stephen Hawking",
                Author = stephenAuthor,
                ReleaseYear = 2018,
                OriginalName = "Auto-Biography of Stephen Hawking",
            };

            var newBookEntity = this.BookRepository.Insert(newBook, transaction).Result;

            transaction.Commit();
            connection.Close();

            var bookEntity = this.BookRepository.Get(b => b.Name == "Auto-Biography of Stephen Hawking").Result;

            Assert.AreNotEqual(null, bookEntity, "Insert in transaction failed");
        }

        /// <summary>
        /// This tests if a changed parent will NOT update when using the update only method, as it should only update references and not objects
        /// </summary>
        [Test]
        public void TestUpdateOnlyWithEditedParent()
        {
            var bookEntity = this.BookRepository.Get(b => b.Name == "Brief Answers to the Big Questions").Result;
            var carlSagan = this.AuthorRepository.Get(a => a.Name == "Carl Sagan").Result;

            bookEntity.Name = "The Brief Answers to the Big Questions";
            bookEntity.Author = carlSagan;
            carlSagan.Name = "Karl Sagan";
            carlSagan.BirthYear = 1944;

            var updateResult = this.BookRepository.UpdateOnly(bookEntity, b => b.Name, b => b.Author).Result;

            Assert.AreNotEqual(false, updateResult, "Could not update Book");

            var updatedBookEntity = this.BookRepository.Get(b => b.Name == "The Brief Answers to the Big Questions", b => b.Author).Result;

            Assert.AreNotEqual(null, updatedBookEntity, "Could not retrieve updated Book");
            Assert.AreEqual(carlSagan.Id, updatedBookEntity.Author.Id, "Author reference in Book was not updated");
            Assert.AreEqual("Carl Sagan", updatedBookEntity.Author.Name, "Author name was updated when only reference should be updated");
            Assert.AreEqual(1934, updatedBookEntity.Author.BirthYear, "Author birth year was updated when only reference should be updated");
        }
    }
}