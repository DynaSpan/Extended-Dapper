using System;
using System.Linq;
using Extended.Dapper.Core.Database;
using Extended.Dapper.Core.Repository;
using Extended.Dapper.Core.Reflection;
using Extended.Dapper.Core.Sql;
using Extended.Dapper.Core.Sql.QueryProviders;
using Extended.Dapper.Tests.Models;
using NUnit.Framework;
using System.Collections.Generic;

namespace Extended.Dapper.Tests.Query
{
    [TestFixture]
    public class TestSelectQuery
    {
        private EntityRepository<Book> BookRepository { get; set; }
        private EntityRepository<Author> AuthorRepository { get; set; }
        private EntityRepository<Category> CategoryRepository { get; set; }

        [SetUp]
        public void Setup()
        {
            var databaseSettings = new DatabaseSettings()
            {
                Host = "172.20.0.10",
                User = "dapper",
                Password = "extended-dapper-sql-password",
                Database = "dapper",
                DatabaseProvider = DatabaseProvider.MySQL
            };

            SqlQueryProviderHelper.SetProvider(DatabaseProvider.MySQL, databaseSettings);
            SqlQueryProviderHelper.Verbose = true;
            var sqlGenerator = new SqlGenerator(DatabaseProvider.MySQL);
            
            var databaseFactory = new DatabaseFactory(databaseSettings);
            
            BookRepository = new EntityRepository<Book>(databaseFactory);
            AuthorRepository = new EntityRepository<Author>(databaseFactory);
            CategoryRepository = new EntityRepository<Category>(databaseFactory);
        }

        /// <summary>
        /// Currently used for literal testing, not unittesting
        /// </summary>
        [Test]
        public void TestModelMapping()
        {
            var books = (BookRepository.GetAll(b => b.ReleaseYear == 2009 || b.ReleaseYear == 2019, b => b.Author, b => b.Category).Result);

            foreach (var book in books)
            {
                Console.WriteLine(book);
            }

            Console.WriteLine("==============");

            var authors = (AuthorRepository.GetAll(a => a.BirthYear == 1984, a => a.Books).Result);

            foreach (var author in authors)
            {
                Console.WriteLine(author);
            }

            Console.WriteLine("==============");

            // Get other author by ID
            var otherAuthor = AuthorRepository.GetById(new Guid("6ba27ef2-fb90-4f85-ba23-6934cf5a04ec"), a => a.Books).Result;

            Console.WriteLine(otherAuthor);
        }

        [Test]
        public void TestById()
        {
            var book = BookRepository.GetById("3D285B02-A8CA-4468-9172-EB3A073C488C", b => b.Author).Result;

            Console.WriteLine(book);
        }

        [Test]
        public void TestInsert()
        {
            var newAuthor = new Author(){
                Name = "Spees Kees",
                BirthYear = 2652,
                Country = "Republic of Earth Citizens, Mars, Solar System, Milky Way Galaxy"
            };
            var newBook = new Book() {
                Author = newAuthor,
                Name = "The birth of Spees",
                ReleaseYear = 2687,
                Category = new Category() {
                    Name = "Test",
                    Description = "Blaat"
                }
            };

            var book = BookRepository.Insert(newBook).Result;

            Console.WriteLine(book.Id);
            Console.WriteLine(book);
        }

        [Test]
        public void TestInsert2()
        {
            var category = CategoryRepository.Get(c => c.Name == "Romance").Result;

            var newBook1 = new Book() {
                Name = "Moi Book #1",
                ReleaseYear = 2020,
                Category = category
            };
            var newBook2 = new Book() {
                Name = "Moi Book #2",
                ReleaseYear = 2010,
                Category = category
            };

            var bookList = new List<Book>();
            bookList.Add(newBook1);
            bookList.Add(newBook2);

            var newAuthor = new Author() {
                Name = "Pietje Piet",
                BirthYear = 1990,
                Country = "Ergens & nergens",
                Books = bookList
            };

            var author = AuthorRepository.Insert(newAuthor).Result;

            Console.WriteLine(author);
        }

        [Test]
        public void TestUpdate()
        {
            var book = BookRepository.Get(b => b.Name == "Brief History of Space", b => b.Author).Result;
            var otherAuthor = AuthorRepository.Get(a => a.Name == "Mili Drosje").Result;

            Console.WriteLine(book);

            book.ReleaseYear = 1988;
            book.Author = otherAuthor;

            var res = BookRepository.Update(book, b => b.Author).Result;
        }

        [Test]
        public void TestUpdate2()
        {
            var author = AuthorRepository.Get(a => a.Name == "Pietje Piet", a => a.Books).Result;

            author.Books.Remove(author.Books.First());

            Console.WriteLine(author);

            var res = AuthorRepository.Update(author, a => a.Books).Result;

            Console.WriteLine(res);
        }

        [Test]
        public void TestDelete()
        {
            var category = CategoryRepository.Get(c => c.Name == "Romance").Result;

            var newBook1 = new Book() {
                Name = "trotse boernlevn",
                ReleaseYear = 2020,
                Category = category
            };
            var newBook2 = new Book() {
                Name = "mien merk; jon dier",
                ReleaseYear = 2010,
                Category = category
            };

            var bookList = new List<Book>();
            bookList.Add(newBook1);
            bookList.Add(newBook2);

            var newAuthor = new Author() {
                Name = "Harry Barry",
                BirthYear = 1940,
                Country = "Hutje op de hei",
                Books = bookList
            };

            var res = AuthorRepository.Insert(newAuthor).Result;

            // Delete same author
            var res2 = AuthorRepository.Delete(res).Result;

            Console.WriteLine(res2);
        }
    }
}
