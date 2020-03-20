using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Extended.Dapper.Core.Attributes.Entities.Relations;
using Extended.Dapper.Core.Database.Entities;

namespace Extended.Dapper.Tests.Models
{
    [Table("Author")]
    public class Author : Entity
    {
        public string Name { get; set; }

        public int BirthYear { get; set; }

        public string Country { get; set; }

        [OneToMany(typeof(Book), "AuthorId")]
        public ICollection<Book> Books { get; set; }

        public override string ToString()
        {
            var returnString = string.Format("{0} ({1}), {2}", Name, BirthYear, Country);

            if (Books != null)
            {
                foreach (var book in Books)
                {
                    returnString = returnString + Environment.NewLine + " - " + book.Name + " (" + book.ReleaseYear + ")";
                }
            }

            return returnString;
        }
    }
}
