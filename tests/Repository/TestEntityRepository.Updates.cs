using System;
using System.Collections.Generic;
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
        public void TestUpdateWithNullChildren()
        {
            var stephenAuthor = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking").Result;
            stephenAuthor.Country = "United States";

            this.AuthorRepository.Update(stephenAuthor).Wait();

            // Check if books still exists
            var stephenWithBooks = this.AuthorRepository.Get(a => a.Name == "Stephen Hawking", a => a.Books).Result;
            Assert.AreEqual(3, stephenWithBooks.Books.Count, "Books have been changed by update");
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
    }
}