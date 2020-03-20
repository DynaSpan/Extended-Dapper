using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database.Entities;

namespace Extended.Dapper.Tests.Models
{
    [Table("LegacyBook")]
    public class LegacyBook : Entity
    {
        public string Name { get; set; }

        public int ReleaseYear { get; set; }

        [ManyToOne(typeof(Author), "AuthorId")]
        public Author Author { get; set; }

        [ManyToOne(typeof(Category), "CategoryId", true)]
        public Category Category { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} ({1})", Name, ReleaseYear);
            sb.AppendLine("");
            sb.AppendFormat("Category: {0} - Author: {1}", Category?.Name, Author?.Name);

            return sb.ToString();
        }
    }
}
