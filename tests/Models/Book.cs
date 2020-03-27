using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Attributes.Entities.Relations;

namespace Extended.Dapper.Tests.Models
{
    [Table("Book")]
    public class Book : ExternalIdEntity
    {
        public string Name { get; set; }

        public int ReleaseYear { get; set; }

        [NotMapped]
        public double CalculatedReviewScore { get; set; }

        [ManyToOne(typeof(Author), "AuthorId")]
        public Author Author { get; set; }

        [ManyToOne(typeof(Author), "CoAuthorId", true)]
        public Author CoAuthor { get; set; }

        [ManyToOne(typeof(Category), "CategoryId", true)]
        public Category Category { get; set; }

        [IgnoreOnUpdate]
        public string OriginalName { get; set; }

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
