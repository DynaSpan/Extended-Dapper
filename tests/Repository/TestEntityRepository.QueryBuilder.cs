using System;
using System.Collections.Generic;
using System.Linq;
using Extended.Dapper.Tests.Helpers;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;

namespace Extended.Dapper.Tests.Repository
{
    [TestFixture]
    public class TestEntityRepositoryQueryBuilder : TestEntityRepository
    {
        [OneTimeSetUp]
        public override void FixtureSetUp()
        {
            base.FixtureSetUp();
            DatabaseHelper.PopulateDatabase().Wait();
        }

        /// <summary>
        /// Tests if only the fields provided in the select are selected
        /// </summary>
        [Test]
        public void TestSelect()
        {
            var carlSagan = this.AuthorRepository.GetQueryBuilder()
                .Select(a => a.Name)
                .Select(a => a.BirthYear)
                .Where(a => a.Name == "Carl Sagan")
                .GetResults().Result.FirstOrDefault();

            Assert.AreNotEqual(null, carlSagan, "Could not retrieve author");
            Assert.AreEqual("Carl Sagan", carlSagan.Name, "Could not retrieve selected field");
            Assert.AreEqual(1934, carlSagan.BirthYear, "Could not retrieve selected field");

            Assert.AreEqual(null, carlSagan.Country, "Field was not empty");
        }

        /// <summary>
        /// Tests if only the fields provided in the select are selected
        /// </summary>
        [Test]
        public void TestCombinedSelect()
        {
            var carlSagan = this.AuthorRepository.GetQueryBuilder()
                .Select(a => a.Name, a => a.BirthYear)
                .Where(a => a.Name == "Carl Sagan")
                .GetResults().Result.FirstOrDefault();

            Assert.AreNotEqual(null, carlSagan, "Could not retrieve author");
            Assert.AreEqual("Carl Sagan", carlSagan.Name, "Could not retrieve selected field");
            Assert.AreEqual(1934, carlSagan.BirthYear, "Could not retrieve selected field");

            Assert.AreEqual(null, carlSagan.Country, "Field was not empty");
        }

        /// <summary>
        /// Tests if only the fields provided in the select are selected,
        /// and if this also applies to the children
        /// </summary>
        [Test]
        public void TestSelectWithChildren()
        {
            var carlSagan = this.AuthorRepository.GetQueryBuilder()
                .Select(a => a.Name, a => a.BirthYear)
                .IncludeChild<Book>(a => a.Books, b => b.Name)
                .Where(a => a.Name == "Carl Sagan")
                .GetResults().Result.FirstOrDefault();

            Assert.AreNotEqual(null, carlSagan, "Could not retrieve author");
            Assert.AreEqual("Carl Sagan", carlSagan.Name, "Could not retrieve selected field");
            Assert.AreEqual(1934, carlSagan.BirthYear, "Could not retrieve selected field");

            Assert.AreEqual(null, carlSagan.Country, "Field was not empty");

            Assert.AreEqual(2, carlSagan.Books.Count, "Did not retrieve children properly");

            var paleBlueBook = carlSagan.Books.First(b => b.Name == "Pale Blue Dot: A Vision of the Human Future in Space");
            var cosmosBook = carlSagan.Books.First(b => b.Name == "Cosmos: A Personal Voyage");

            Assert.AreEqual(default(int), paleBlueBook.ReleaseYear, "Field of child was not empty");
            Assert.AreEqual(null, paleBlueBook.OriginalName, "Field of child was not empty");
            Assert.AreEqual(default(double), paleBlueBook.CalculatedReviewScore, "Field of child was not empty");
        }

        /// <summary>
        /// Tests if selecting with multiple childrens works properly
        /// </summary>
        [Test]
        public void TestSelectWithMultipleChildren()
        {
            var scienceAnswersBook = this.BookRepository.GetQueryBuilder()
                .Select(b => b.Name, b => b.ReleaseYear)
                .IncludeChild<Author>(b => b.Author, a => a.Name, a => a.Country)
                .IncludeChild<Author>(b => b.CoAuthor, a => a.Name, a => a.Country)
                .IncludeChild<Category>(b => b.Category, c => c.Name)
                .Where(b => b.Name == "Science questions answered")
                .GetResults().Result.First();

            Assert.AreEqual("Science questions answered", scienceAnswersBook.Name);
            Assert.AreEqual(2015, scienceAnswersBook.ReleaseYear);

            Assert.AreNotEqual(null, scienceAnswersBook.Author, "Child was not properly retrieved");
            Assert.AreEqual("Stephen Hawking", scienceAnswersBook.Author.Name, "Child info was not properly retrieved");
            Assert.AreEqual("Carl Sagan", scienceAnswersBook.CoAuthor.Name, "child info was not properly retrieved");

            Assert.AreEqual(null, scienceAnswersBook.OriginalName, "Non-selected property filled");
            Assert.AreEqual(default(int), scienceAnswersBook.Author.BirthYear, "Non-selected child property filled");
            Assert.AreEqual(default(int), scienceAnswersBook.CoAuthor.BirthYear, "Non-selected child property filled");

            Assert.AreEqual("Science", scienceAnswersBook.Category.Name, "Child property was not properly retrieved");
        }

        /// <summary>
        /// Tests if executing the QueryBuilder with a search works
        /// as expected
        /// </summary>
        [Test]
        public void TestWhere()
        {
            var carlSagan = this.AuthorRepository.GetQueryBuilder()
                .Where(a => a.BirthYear == 1934)
                .GetResults()
                .Result;

            Assert.AreEqual(1, carlSagan.Count(), "Search was not executed correctly");

            this.TestIfAuthorIsValid(carlSagan.First(), AuthorModelType.CarlSagan);
        }

        /// <summary>
        /// Tests if executing the QueryBuilder with a search works
        /// as expected
        /// </summary>
        [Test]
        public void TestMultipleWhere()
        {
            var carlSagan = this.AuthorRepository.GetQueryBuilder()
                .Where(a => a.BirthYear == 1934)
                .Where(a => a.Name == "Carl Sagan")
                .GetResults()
                .Result;

            Assert.AreEqual(1, carlSagan.Count(), "Search was not executed correctly");

            this.TestIfAuthorIsValid(carlSagan.First(), AuthorModelType.CarlSagan);
        }

        /// <summary>
        /// Tests if limiting works correctly
        /// </summary>
        [Test]
        public void TestLimit()
        {
            var twoAuthors = this.AuthorRepository.GetQueryBuilder()
                .Limit(2)
                .GetResults()
                .Result;

            Assert.AreEqual(2, twoAuthors.Count(), "Query was not limited correctly");
        }

        /// <summary>
        /// Tests if limiting with an order by works as expected
        /// </summary>
        [Test]
        public void TestLimitWithOrderBy()
        {
            var twoAuthors = this.AuthorRepository.GetQueryBuilder()
                .OrderBy(a => a.Name)
                .Limit(2)
                .GetResults()
                .Result;

            Assert.AreEqual(2, twoAuthors.Count(), "Query was not limited correctly");

            var authorWoBooks = twoAuthors.First();
            var carlSagan = twoAuthors.ElementAt(1);

            this.TestIfAuthorIsValid(authorWoBooks, AuthorModelType.AuthorWithoutBooks);
            this.TestIfAuthorIsValid(carlSagan, AuthorModelType.CarlSagan);
        }

        /// <summary>
        /// Tests if limiting with an order by and search
        /// works as expected
        /// </summary>
        [Test]
        public void TestSearchLimitedWithOrderBy()
        {
            var twoAuthors = this.AuthorRepository.GetQueryBuilder()
                .Where(a => a.BirthYear > 1910) // exclude Author w/o Books
                .OrderBy(a => a.Name)
                .Limit(2)
                .GetResults()
                .Result;

            Assert.AreEqual(2, twoAuthors.Count(), "Query was not limited correctly");

            var carlSagan = twoAuthors.Single(a => a.Name == "Carl Sagan");
            var stephenHawking = twoAuthors.Single(a => a.Name == "Stephen Hawking");

            this.TestIfAuthorIsValid(stephenHawking, AuthorModelType.StephenHawking);
            this.TestIfAuthorIsValid(carlSagan, AuthorModelType.CarlSagan);
        }

        /// <summary>
        /// Tests if limiting with an order by and search
        /// works as expected
        /// </summary>
        [Test]
        public void TestSearchWithOrderByAndChildren()
        {
            var twoAuthors = this.AuthorRepository.GetQueryBuilder()
                .Where(a => a.BirthYear > 1910) // exclude Author w/o Books
                .IncludeChild<Book>(a => a.Books)
                .OrderBy(a => a.Name)
                .GetResults()
                .Result;

            Assert.AreEqual(2, twoAuthors.Count(), "Query was not filtered correctly");

            var carlSagan = twoAuthors.Single(a => a.Name == "Carl Sagan");
            var stephenHawking = twoAuthors.Single(a => a.Name == "Stephen Hawking");

            Assert.AreEqual(2, carlSagan.Books.Count, "Children count is incorrect");
            Assert.AreEqual(3, stephenHawking.Books.Count, "Children count is incorrect");

            this.TestIfAuthorIsValid(stephenHawking, AuthorModelType.StephenHawking);
            this.TestIfAuthorIsValid(carlSagan, AuthorModelType.CarlSagan);
        }
    }
}