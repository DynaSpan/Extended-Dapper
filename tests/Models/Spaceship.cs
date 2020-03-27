using System;
using System.ComponentModel.DataAnnotations;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Attributes.Entities.Relations;

namespace Extended.Dapper.Tests.Models
{
    public class Spaceship
    {
        [Key]
        [AutoValue]
        public int Id { get; set; }

        [AutoValue]
        public Guid ExternalId { get; set; }

        [ManyToOne(typeof(Author), "OwnerId", true)]
        public Author Owner { get; set; }

        public string Name { get; set; }

        public int BuildYear { get; set; }
    }
}