using System;
using Extended.Dapper.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database.Entities;

namespace Extended.Dapper.Tests.Models
{
    public class Book : Entity
    {
        public string Name { get; set; }

        [ManyToOne("Author", "AuthorId", "Id")]
        public Author Author { get; set; }
    }
}
