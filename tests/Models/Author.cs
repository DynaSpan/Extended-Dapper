using System;
using System.Collections.Generic;
using Extended.Dapper.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database.Entities;

namespace Extended.Dapper.Tests.Models
{
    public class Author : Entity
    {
        public string Name { get; set; }

        public int BirthYear { get; set; }

        public string Country { get; set; }

        [OneToMany("Book", "Id", "AuthorId")]
        public ICollection<Book> Books { get; set; }
    }
}
