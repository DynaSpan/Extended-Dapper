using System;
using System.Collections.Generic;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database.Entities;

namespace Extended.Dapper.Tests.Models
{
    public class Category : BaseEntity
    {
        [OneToMany(typeof(Book), "CategoryId")]
        public ICollection<Book> Books { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
