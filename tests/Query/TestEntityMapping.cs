using System;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using Extended.Dapper.Tests.Models;
using Extended.Dapper.Core.Mappers;

namespace Extended.Dapper.Tests.Query
{
    /// <summary>
    /// This class tests if the mapping of model works correctly
    /// </summary>
    [TestFixture]
    public class TestEntityMapping
    {
        public EntityMap AuthorMap { get; set; }

        public EntityMap BookMap { get; set; }

        [SetUp]
        public void Setup()
        {
            this.AuthorMap  = EntityMapper.GetEntityMap(typeof(Author));
            this.BookMap    = EntityMapper.GetEntityMap(typeof(Book));
        }

        [Test]
        public void TestIfPrimaryKeysGetMapped()
        {
            // Author
            var authorPrimaryKeys = this.AuthorMap.PrimaryKeyPropertiesMetadata;
            var authorPrimaryKey = authorPrimaryKeys.FirstOrDefault();

            Assert.AreEqual(1, authorPrimaryKeys.Count);
            Assert.AreEqual("Id", authorPrimaryKey.ColumnName);
            Assert.AreEqual("Id", authorPrimaryKey.PropertyName);
            Assert.AreEqual(typeof(Guid), authorPrimaryKey.PropertyInfo.PropertyType);
            Assert.AreEqual(true, authorPrimaryKey.AutoValue);
        }

        public void TestIfOneToManyGetMapped()
        {
            // Author
            var authorRelations = this.AuthorMap.RelationProperties;
            
        }
    }
}
