using System;
using System.Linq;
using NUnit.Framework;
using Extended.Dapper.Tests.Models;
using Extended.Dapper.Core.Mappers;

namespace Extended.Dapper.Tests.EntityMapping
{
    /// <summary>
    /// This class tests if the mapping of model works correctly
    /// </summary>
    [TestFixture]
    public class TestEntityMapping
    {
        public EntityMap AuthorMap { get; set; }

        public EntityMap BookMap { get; set; }

        [OneTimeSetUpAttribute]
        public void FixtureSetup()
        {
            this.AuthorMap  = EntityMapper.GetEntityMap(typeof(Author));
            this.BookMap    = EntityMapper.GetEntityMap(typeof(Book));
        }

        /// <summary>
        /// This tests if all native properties get correctly mapped
        /// </summary>
        [Test]
        [TestCase("Id", typeof(Guid))]
        [TestCase("Name", typeof(string))]
        [TestCase("ReleaseYear", typeof(int))]
        [TestCase("OriginalName", typeof(string))]
        public void TestNativePropertyMapping(string propertyName, Type expectedType)
        {
            var property = this.BookMap.MappedProperties.Single(p => p.Name == propertyName);
            Assert.AreEqual(expectedType, property.PropertyType, "Property Type is incorrect");
        }

        /// <summary>
        /// This tests if the primary keys get properly mapped
        /// </summary>
        [Test]
        public void TestIfPrimaryKeysGetMapped()
        {
            // Author
            var authorPrimaryKeys = this.AuthorMap.PrimaryKeyPropertiesMetadata;
            var authorPrimaryKey = authorPrimaryKeys.FirstOrDefault();

            Assert.AreEqual(1, authorPrimaryKeys.Count());
            Assert.AreEqual("Id", authorPrimaryKey.ColumnName);
            Assert.AreEqual("Id", authorPrimaryKey.PropertyName);
            Assert.AreEqual(typeof(Guid), authorPrimaryKey.PropertyInfo.PropertyType);
            Assert.AreEqual(true, authorPrimaryKey.AutoValue);
        }

        /// <summary>
        /// This tests if the relations get properly mapped
        /// </summary>
        [Test]
        public void TestIfRelationsGetMapped()
        {
            // Author
            var authorRelations = this.AuthorMap.RelationProperties;
            Assert.AreEqual(2, authorRelations.Count, "Author does not have 2 relation properties mapped");
            var bookRelation = authorRelations.Single(kv => kv.Key.Name == "Books");
            var carRelation  = authorRelations.Single(kv => kv.Key.Name == "Spaceships");

            // Books
            var booksRelations      = this.BookMap.RelationProperties;
            Assert.AreEqual(3, booksRelations.Count, "Book does not have 3 relation properties mapped");
            var authorRelation      = booksRelations.Single(kv => kv.Key.Name == "Author");
            var coAuthorRelation    = booksRelations.Single(kv => kv.Key.Name == "CoAuthor");
            var categoryRelation    = booksRelations.Single(kv => kv.Key.Name == "Category");
        }

        /// <summary>
        /// This tests if a NotMapped field doesn't get mapped
        /// </summary>
        [Test]
        public void TestIfNotMappedWorks()
        {
            var prop = this.BookMap.Properties.SingleOrDefault(p => p.Name == "CalculatedReviewScore");
            var notMappedProp = this.BookMap.MappedProperties.SingleOrDefault(p => p.Name == "CalculatedReviewScore");

            Assert.AreNotEqual(null, prop, "NotMapped property does not exist");
            Assert.AreEqual(null, notMappedProp, "A property with NotMapped has been mapped!");
        }
    }
}
