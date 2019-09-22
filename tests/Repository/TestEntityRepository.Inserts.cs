using System;
using System.Collections.Generic;
using System.Linq;
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
        /// This tests if inserting a single item without children works correctly
        /// </summary>
        [Test]
        public void TestSingleItemInsert()
        {   
            var stephenHawking = ModelHelper.GetAuthorModel(AuthorModelType.StephenHawking);

            var stephenHawkingEntity = this.AuthorRepository.Insert(stephenHawking).Result;

            this.TestIfAuthorIsValid(stephenHawkingEntity, AuthorModelType.StephenHawking);
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
            stephenHawking.Books.Add(ModelHelper.GetBookModel(BookModelType.BriefAnswers, scienceCategory, stephenHawking));
            stephenHawking.Books.Add(ModelHelper.GetBookModel(BookModelType.BriefHistoryOfTime, scienceCategory, stephenHawking));

            var stephenHawkingEntity = this.AuthorRepository.Insert(stephenHawking).Result;

            this.TestIfAuthorIsValid(stephenHawkingEntity, AuthorModelType.StephenHawking);

            this.TestIfBookIsValid(stephenHawkingEntity.Books.ElementAt(0), BookModelType.BriefAnswers);
            this.TestIfBookIsValid(stephenHawkingEntity.Books.ElementAt(1), BookModelType.BriefHistoryOfTime);

            // Test if both books have the same author entity
            Assert.AreEqual(stephenHawkingEntity.Id, stephenHawkingEntity.Books.ElementAt(0).Author.Id, "Parent Author Id is not equal; a new parent has been created");
            Assert.AreEqual(stephenHawkingEntity.Id, stephenHawkingEntity.Books.ElementAt(1).Author.Id, "Parent Author Id is not equal; a new parent has been created");
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
            Assert.AreNotEqual(Guid.Empty, paleBlueDotBookEntity.Category.Id, "Category has not been inserted properly as it doesn't have an ID");
        }
    }
}