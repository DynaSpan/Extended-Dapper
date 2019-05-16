using System;
using System.ComponentModel.DataAnnotations;
using Extended.Dapper.Attributes.Entities;

namespace Extended.Dapper.Core.Database.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        [AutoValue]
        public Guid Id { get; set; }
    }
}