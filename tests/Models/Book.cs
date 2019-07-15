using System;
using System.Text;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database.Entities;

namespace Extended.Dapper.Tests.Models
{
    public class Book : Entity
    {
        public string Name { get; set; }

        public int ReleaseYear { get; set; }

        [ManyToOne(typeof(Author), "AuthorId")]
        public Author Author { get; set; }

        [ManyToOne(typeof(Author), "CoAuthorId", true)]
        public Author CoAuthor { get; set; }

        [ManyToOne(typeof(Category), "CategoryId", true)]
        public Category Category { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} ({1})", Name, ReleaseYear);
            sb.AppendLine("");
            sb.AppendFormat("Category: {0} - Author: {1}", Category?.Name, Author?.Name);

            if (CoAuthor != null)
                sb.AppendFormat(" - Co-Author: {0}", CoAuthor.Name);

            return sb.ToString();
        }
    }
}
