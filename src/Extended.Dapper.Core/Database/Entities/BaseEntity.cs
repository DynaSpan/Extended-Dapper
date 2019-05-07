using System;
using System.ComponentModel.DataAnnotations;

namespace Extended.Dapper.Core.Database.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
    }
}