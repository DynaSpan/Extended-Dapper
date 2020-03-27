using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Tests.Helpers;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;

namespace Extended.Dapper.Tests.Repository
{
    [TestFixture]
    public class TestEntityRepositoryInserts : TestEntityRepository
    {
        /// <summary>
        /// Clear database before every test
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            DatabaseHelper.ClearDatabase();
        }

        /// <summary>
        /// This tests if populating the database works as expected
        /// </summary>
        [Test]
        public void TestDatabasePopulation()
        {
            DatabaseHelper.PopulateDatabase().Wait();

            var authors = AuthorRepository.GetAll(a => a.Books).Result;
            Assert.AreNotEqual(null, authors, "Could not retrieve Authors");
            Assert.AreEqual(3, authors.Count(), "Incorrect number of Authors retrieved");

            var carlAuthor = authors.SingleOrDefault(a => a.Name == "Carl Sagan");
            var stephenAuthor = authors.SingleOrDefault(a => a.Name == "Stephen Hawking");
            var noBooksAuthor = authors.SingleOrDefault(a => a.Name == "Author w/o Books");

            this.TestIfAuthorIsValid(carlAuthor, AuthorModelType.CarlSagan);
            this.TestIfAuthorIsValid(stephenAuthor, AuthorModelType.StephenHawking);
            this.TestIfAuthorIsValid(noBooksAuthor, AuthorModelType.AuthorWithoutBooks);

            Assert.AreNotEqual(null, carlAuthor.Books, "Could not retrieve Books of Author Carl Sagan");
            Assert.AreEqual(2, carlAuthor.Books.Count(), "Did not retrieve the correct books for Carl Sagan");

            Assert.AreNotEqual(null, stephenAuthor.Books, "Could not retrieve Books of Author Stephen Hawking");
            Assert.AreEqual(3, stephenAuthor.Books.Count(), "Did not retrieve the correct books for Stephen Hawking");

            Assert.AreNotEqual(null, noBooksAuthor.Books, "Could not retrieve Books of Author Author w/o Books");
            Assert.AreEqual(0, noBooksAuthor.Books.Count(), "Did not retrieve the correct books for Author w/o Books");

            var cosmosVoyageBook = carlAuthor.Books.SingleOrDefault(b => b.Name == "Cosmos: A Personal Voyage");
            var paleBlueDotBook  = carlAuthor.Books.SingleOrDefault(b => b.ReleaseYear == 1994);

            this.TestIfBookIsValid(cosmosVoyageBook, BookModelType.Cosmos);
            this.TestIfBookIsValid(paleBlueDotBook, BookModelType.PaleBlueDot);

            var briefHistoryBook = stephenAuthor.Books.SingleOrDefault(b => b.Name == "A Brief History of Time");
            var briefAnswersBook = stephenAuthor.Books.SingleOrDefault(b => b.ReleaseYear == 2018);

            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime);
            this.TestIfBookIsValid(briefAnswersBook, BookModelType.BriefAnswers);
        }

        /// <summary>
        /// This tests if inserting a single item without children works correctly
        /// </summary>
        [Test]
        public void TestSingleItemInsert()
        {   
            var stephenHawking = ModelHelper.GetAuthorModel(AuthorModelType.StephenHawking);

            var stephenHawkingEntity = this.AuthorRepository.Insert(stephenHawking).Result;
            var authors = this.AuthorRepository.GetAll().Result;
            var author = authors.FirstOrDefault();

            Assert.AreEqual(1, authors.Count(), "Author was not correctly inserted into database");

            this.TestIfAuthorIsValid(author, AuthorModelType.StephenHawking);
        }

        /// <summary>
        /// This tests if inserting a single item with children works correctly
        /// </summary>
        [Test]
        public void TestSingleItemWithChildrenInsert()
        {
            var stephenHawking      = ModelHelper.GetAuthorModel(AuthorModelType.StephenHawking);
            var scienceCategory     = ModelHelper.GetScienceCategory();
            stephenHawking.Books    = new List<Book>();
            stephenHawking.Books.Add(ModelHelper.GetBookModel(BookModelType.BriefAnswers, scienceCategory, null, null, false, true));
            stephenHawking.Books.Add(ModelHelper.GetBookModel(BookModelType.BriefHistoryOfTime, scienceCategory, null, null, false, true));

            var galaxyShip = ModelHelper.GetSpaceshipModel(SpaceshipModelType.GalaxyTraveller);
            var andromedaShip = ModelHelper.GetSpaceshipModel(SpaceshipModelType.AndromedaLink);

            stephenHawking.Spaceships = new List<Spaceship>();
            stephenHawking.Spaceships.Add(galaxyShip);
            stephenHawking.Spaceships.Add(andromedaShip);

            var stephenHawkingEntity = this.AuthorRepository.Insert(stephenHawking).Result;

            var authorCount     = this.AuthorRepository.GetAll().Result.Count();
            var categoryCount   = this.CategoryRepository.GetAll().Result.Count();
            var bookCount       = this.BookRepository.GetAll().Result.Count();
            var shipCount       = this.SpaceshipRepository.GetAll().Result.Count();

            Assert.AreEqual(1, authorCount, "Author was not correctly inserted into database");
            Assert.AreEqual(1, categoryCount, "Category was not correctly inserted into database");
            Assert.AreEqual(2, bookCount, "Books were not correctly inserted into database");
            Assert.AreEqual(2, shipCount, "Spaceships were not correctly inserted into database");

            Assert.AreNotEqual(default(int), galaxyShip.Id, "Integer autovalue was not filled");
            Assert.AreNotEqual(default(int), andromedaShip.Id, "Integer autovalue was not filled");

            Assert.AreNotEqual(Guid.Empty, galaxyShip.ExternalId, "AutoValue GUID was not filled");
            Assert.AreNotEqual(Guid.Empty, andromedaShip.ExternalId, "AutoValue GUID was not filled");

            var booksEntity = this.AuthorRepository.GetMany<Book>(stephenHawkingEntity, a => a.Books, b => b.Author).Result;

            Assert.AreEqual(2, booksEntity.Count(), "Could not retrieve the correct number of books");

            this.TestIfAuthorIsValid(stephenHawkingEntity, AuthorModelType.StephenHawking);

            var briefAnswersBook = booksEntity.First(b => b.Name == "Brief Answers to the Big Questions");
            var briefHistoryBook = booksEntity.First(b => b.Name == "A Brief History of Time");

            this.TestIfBookIsValid(briefAnswersBook, BookModelType.BriefAnswers);
            this.TestIfBookIsValid(briefHistoryBook, BookModelType.BriefHistoryOfTime);

            // Test if both books have the same author entity
            Assert.AreEqual(stephenHawkingEntity.Id, briefAnswersBook.Author.Id, "Parent Author Id is not equal; a new parent has been created");
            Assert.AreEqual(stephenHawkingEntity.Id, briefHistoryBook.Author.Id, "Parent Author Id is not equal; a new parent has been created");
        }

        /// <summary>
        /// This tests if inserting children with a ManyToOne parent entity
        /// works correctly; without inserting a new parent
        /// </summary>
        [Test]
        public void TestChildrenInsertWithParentEntity()
        {
            var carlSagan       = ModelHelper.GetAuthorModel(AuthorModelType.CarlSagan);
            var carlSaganEntity = this.AuthorRepository.Insert(carlSagan).Result;

            var cosmosBook       = ModelHelper.GetBookModel(BookModelType.Cosmos, null, carlSaganEntity);
            var cosmosBookEntity = this.BookRepository.Insert(cosmosBook).Result;

            this.TestIfBookIsValid(cosmosBookEntity, BookModelType.Cosmos, false, true);

            // Check if book has the same parent entity
            Assert.AreEqual(carlSaganEntity.Id, cosmosBookEntity.Author.Id, "Parent Author Id is not equal; a new parent has been created");
        }

        /// <summary>
        /// This tests if inserting children with a ManyToOne parent non-entity
        /// works correctly; thereby creating the parent as well since it does not
        /// exist yet
        /// </summary>
        [Test]
        public void TestChildrenInsertWithParentNotEntity()
        {
            var carlSagan = ModelHelper.GetAuthorModel(AuthorModelType.CarlSagan);

            var cosmosBook       = ModelHelper.GetBookModel(BookModelType.Cosmos, null, carlSagan);
            var cosmosBookEntity = this.BookRepository.Insert(cosmosBook).Result;

            this.TestIfBookIsValid(cosmosBookEntity, BookModelType.Cosmos, false, true);

            // Check if book has the same parent entity
            Assert.AreNotEqual(Guid.Empty, carlSagan.Id, "Parent Id is empty; a new parent has not been created");
        }

        /// <summary>
        /// This tests if inserting children with multiple ManyToOne parents, entity
        /// and non-entity; works correctly
        /// </summary>
        [Test]
        public void TestChildrenInsertWithMultipleParents()
        {
            var carlSagan       = ModelHelper.GetAuthorModel(AuthorModelType.CarlSagan);
            var carlSaganEntity = this.AuthorRepository.Insert(carlSagan).Result;

            var scienceCategory       = ModelHelper.GetScienceCategory();
            var paleBlueDotBook       = ModelHelper.GetBookModel(BookModelType.PaleBlueDot, scienceCategory, carlSaganEntity);
            var paleBlueDotBookEntity = this.BookRepository.Insert(paleBlueDotBook).Result;

            this.TestIfBookIsValid(paleBlueDotBookEntity, BookModelType.PaleBlueDot, true, true);

            // Check if book has the same parent entity
            Assert.AreEqual(carlSaganEntity.Id, paleBlueDotBookEntity.Author.Id, "Author ID is not equal; a new Author has been generated");
            Assert.AreNotEqual(default(int), paleBlueDotBookEntity.Category.Id, "Category has not been inserted properly as it doesn't have an ID");
        }

        /// <summary>
        /// This tests if having children within children works correctly
        /// </summary>
        [Test]
        public void TestInsertWithDeepChildren()
        {
            var carlSagan = ModelHelper.GetAuthorModel(AuthorModelType.CarlSagan);
            var carlSaganEntity = this.AuthorRepository.Insert(carlSagan).Result;

            var scienceCategory = ModelHelper.GetScienceCategory();
            var paleBlueDotBook = ModelHelper.GetBookModel(BookModelType.PaleBlueDot, null, carlSaganEntity, null, true);
            scienceCategory.Books = new List<Book>();
            scienceCategory.Books.Add(paleBlueDotBook);

            var scienceCategoryEntity = this.CategoryRepository.Insert(scienceCategory).Result;

            Assert.AreNotEqual(null, scienceCategoryEntity, "Inserting entity with deep children failed");

            var numberOfCategories = this.CategoryRepository.GetAll().Result.Count();
            var numberOfAuthors = this.AuthorRepository.GetAll().Result.Count();
            var numberOfBooks = this.BookRepository.GetAll().Result.Count();

            Assert.AreEqual(1, numberOfCategories, "Number of categories is not equal to 1");
            Assert.AreEqual(1, numberOfAuthors, "Number of authors is not equal to 1");
            Assert.AreEqual(1, numberOfBooks, "Number of books are not equal to 1");
        }

        /// <summary>
        /// Tests if an object without Autovalue keys gets inserted properly
        /// </summary>
        [Test]
        public void TestInsertWithoutAutovalueKeys()
        {
            var paleBlueDotBook = ModelHelper.GetBookModel(BookModelType.PaleBlueDot);
            var bookEntity = this.BookRepository.Insert(paleBlueDotBook).Result;

            var log = this.Log(bookEntity, "CREATE").Result;

            var numberOfLogs = this.LogRepository.GetAll().Result.Count();
            Assert.AreEqual(1, numberOfLogs, "Incorrect number of logs");
        }

        /// <summary>
        /// Tests if an object with filled autovalue keys does not get inserted
        /// </summary>
        [Test]
        public void TestInsertWithFilledAutovalues()
        {
            var paleBlueDotBook = ModelHelper.GetBookModel(BookModelType.PaleBlueDot);
            paleBlueDotBook.Id = 287;

            var bookEntity = this.BookRepository.Insert(paleBlueDotBook).Result;

            var numberOfBooks = this.BookRepository.GetAll().Result.Count();
            Assert.AreEqual(0, numberOfBooks, "Object with filled autovalue got inserted");
        }

        /// <summary>
        /// Tests if an object with the autovalue keys does get inserted when forced
        /// </summary>
        [Test]
        public void TestForcedInsertWithFilledAutovalues()
        {
            // This is not possible on MSSQL
            if (DatabaseHelper.GetDatabaseFactory().DatabaseProvider == DatabaseProvider.MSSQL)
                Assert.Pass();

            var paleBlueDotBook = ModelHelper.GetBookModel(BookModelType.PaleBlueDot);
            paleBlueDotBook.Id = 287;

            var bookEntity = this.BookRepository.Insert(paleBlueDotBook, true).Result;

            var numberOfBooks = this.BookRepository.GetAll().Result.Count();
            Assert.AreEqual(1, numberOfBooks, "Object with forced filled autovalue didn't insert");
        }

        /// <summary>
        /// This test checks if integer autovalues works properly
        /// </summary>
        [Test]
        public void TestIntegerAutoValues()
        {
            var ship1 = ModelHelper.GetSpaceshipModel(SpaceshipModelType.AndromedaLink);
            var ship2 = ModelHelper.GetSpaceshipModel(SpaceshipModelType.GalaxyTraveller);
            var ship3 = ModelHelper.GetSpaceshipModel(SpaceshipModelType.NewHorizons);

            var ship1Entity = this.SpaceshipRepository.Insert(ship1).Result;
            var ship2Entity = this.SpaceshipRepository.Insert(ship2).Result;
            var ship3Entity = this.SpaceshipRepository.Insert(ship3).Result;

            Assert.AreNotEqual(null, ship1Entity, "Could not insert object with integer autovalue");
            Assert.AreNotEqual(null, ship2Entity, "Could not insert object with integer autovalue");
            Assert.AreNotEqual(null, ship3Entity, "Could not insert object with integer autovalue");

            Assert.AreNotEqual(default(int), ship1Entity.Id, "Integer autovalue ID was not properly inserted");
            Assert.AreNotEqual(default(int), ship2Entity.Id, "Integer autovalue ID was not properly inserted");
            Assert.AreNotEqual(default(int), ship3Entity.Id, "Integer autovalue ID was not properly inserted");

            Assert.AreNotEqual(Guid.Empty, ship1Entity.ExternalId, "GUID autovalue was not properly inserted");
            Assert.AreNotEqual(Guid.Empty, ship2Entity.ExternalId, "GUID autovalue was not properly inserted");
            Assert.AreNotEqual(Guid.Empty, ship3Entity.ExternalId, "GUID autovalue was not properly inserted");
        }

        /// <summary>
        /// This tests if inserting objects within a transaction works correctly
        /// </summary>
        [Test]
        public void TestInsertWithTransaction()
        {
            using (IDbConnection connection = DatabaseHelper.GetDatabaseFactory().GetDatabaseConnection())
            {
                connection.Open();

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    int numberOfAuthors;
                    int numberOfBooks;

                    try
                    {
                        var carlSagan = ModelHelper.GetAuthorModel(AuthorModelType.CarlSagan);
                        var insertEntity = this.AuthorRepository.Insert(carlSagan, transaction).Result;

                        this.TestIfAuthorIsValid(insertEntity, AuthorModelType.CarlSagan);

                        var paleBlueDotBook = ModelHelper.GetBookModel(BookModelType.PaleBlueDot, null, insertEntity, null, true);
                        var insertedBook = this.BookRepository.Insert(paleBlueDotBook, transaction).Result;

                        transaction.Commit();
                        connection.Close();

                        numberOfAuthors = this.AuthorRepository.GetAll().Result.Count();
                        numberOfBooks = this.BookRepository.GetAll().Result.Count();
                    }
                    catch (Exception)
                    {
                        transaction?.Rollback();
                        connection?.Close();

                        throw;
                    }

                    Assert.AreEqual(1, numberOfAuthors, "Number of authors is not equal to 1");
                    Assert.AreEqual(1, numberOfBooks, "Number of books are not equal to 1");
                }
            }
        }
    }
}