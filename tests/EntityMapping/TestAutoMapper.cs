using System;
using System.Linq;
using System.Transactions;
using AutoMapper;
using Extended.Dapper.Core.Repository;
using Extended.Dapper.Core.Sql.QueryProviders;
using Extended.Dapper.Tests.Helpers;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;

namespace Extended.Dapper.Tests.EntityMapping
{
    /// <summary>
    /// This class tests if AutoMapper works nicely with
    /// Extended.Dapper &amp; Dapper
    /// </summary>
    [TestFixture]
    public class TestAutoMapper
    {
        protected EntityRepository<Book> BookRepository { get; set; }
        protected EntityRepository<LegacyBook> LegacyBookRepository { get; set; }
        protected IMapper Mapper { get; set; }

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            SqlQueryProviderHelper.Verbose = true;
            DatabaseHelper.CreateDatabase();

            BookRepository = new EntityRepository<Book>(DatabaseHelper.GetDatabaseFactory());
            LegacyBookRepository = new EntityRepository<LegacyBook>(DatabaseHelper.GetLegacyDatabaseFactory());

            this.SetUpAutoMapper();
        }

        /// <summary>
        /// Clear database before every test
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            DatabaseHelper.ClearDatabase();
        }

        /// <summary>
        /// This tests if inserting a mapped object works as expected
        /// </summary>
        [Test]
        public void TestIfInsertMappingWorks()
        {
            var book = ModelHelper.GetBookModel(BookModelType.PaleBlueDot);

            // Insert this book into normal repository
            var bookEntity = this.BookRepository.Insert(book).Result;
            var bookCount = this.BookRepository.GetAll().Result.Count();

            Assert.AreNotEqual(null, bookEntity, "Could not insert Book");
            Assert.AreEqual(1, bookCount, "Book was not properly inserted");

            // Map book to legacyBook
            var legacyBook = this.Mapper.Map<LegacyBook>(bookEntity);
            var legacyBookEntity = this.LegacyBookRepository.Insert(legacyBook, true).Result;
            var legacyBookCount = this.LegacyBookRepository.GetAll().Result.Count();

            Assert.AreNotEqual(null, legacyBookEntity, "Could not properly insert AutoMapper mapped object");
            Assert.AreEqual(1, legacyBookCount, "Legacy Book not properly inserted");
        }

        /// <summary>
        /// This tests if inserting a mapped object works as expected
        /// when it's done within a transaction scope
        /// </summary>
        // [Test]
        // public void TestIfInsertMappingWorksWithinTransactionScope()
        // {
        //     var book = ModelHelper.GetBookModel(BookModelType.PaleBlueDot);
        //     TransactionScope transScope = null;

        //     try
        //     {
        //         transScope = this.CreateReadCommittedTransactionScope();

        //         // Insert this book into normal repository
        //         var bookEntity = this.BookRepository.Insert(book).Result;
                
        //         Assert.AreNotEqual(null, bookEntity, "Could not insert Book");
                
        //         // Map book to legacyBook
        //         var legacyBook = this.Mapper.Map<LegacyBook>(bookEntity);
        //         var legacyBookEntity = this.LegacyBookRepository.Insert(legacyBook, true).Result;

        //         Assert.AreNotEqual(null, legacyBookEntity, "Could not properly insert AutoMapper mapped object");

        //         transScope.Complete();
        //         transScope.Dispose();

        //         var bookCount = this.BookRepository.GetAll().Result.Count();
        //         var legacyBookCount = this.LegacyBookRepository.GetAll().Result.Count();

        //         Assert.AreEqual(1, bookCount, "Book was not properly inserted");
        //         Assert.AreEqual(1, legacyBookCount, "Legacy Book not properly inserted");
        //     }
        //     catch (Exception)
        //     {
        //         try {
        //             transScope?.Dispose();
        //         } catch (Exception) { }

        //         throw;
        //     }
        //     finally
        //     {
        //         try {
        //             transScope?.Dispose();
        //         } catch (Exception) { }
        //     }
        // }

        /// <summary>
        /// Sets up AutoMapper
        /// </summary>
        private void SetUpAutoMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg => {
                cfg.CreateMap<Book, LegacyBook>();
                cfg.CreateMap<LegacyBook, Book>();
            });

            this.Mapper = mapperConfig.CreateMapper();
        }

        /// <summary>
        /// Creates a transactionscope with ReadCommitted Isolation, the same level as sql server
        /// </summary>
        private TransactionScope CreateReadCommittedTransactionScope()
        {
            var options = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.DefaultTimeout
            };

            return new TransactionScope(TransactionScopeOption.Required, options);
        }
    }
}