using System;
using Extended.Dapper.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database.Entities;

namespace Extended.Dapper.Tests.Models
{
    public class Book : Entity
    {
        public string Name { get; set; }

        public int ReleaseYear { get; set; }

        [ManyToOne("Book", "AuthorId", "Id")]
        public Author Author { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1}) - Author: {2}", Name, ReleaseYear, Author?.Name);
        }
    }
}
