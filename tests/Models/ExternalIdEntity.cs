using System;
using System.ComponentModel.DataAnnotations;
using Extended.Dapper.Core.Attributes.Entities;
using Extended.Dapper.Core.Database.Entities;

namespace Extended.Dapper.Tests.Models
{
    public class ExternalIdEntity : Entity
    {
        [Key]
        [AutoValue]
        public override int Id { get; set; }

        [AutoValue]
        public Guid ExternalId { get; set; }
    }
}