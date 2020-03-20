using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database.Entities;

namespace Extended.Dapper.Tests.Models
{
    [Table("Category")]
    public class Category : BaseEntity
    {
        [OneToMany(typeof(Book), "CategoryId")]
        public ICollection<Book> Books { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        [IgnoreOnInsert]
        public string EditedBy { get; set; }
    }
}
