using System;
using System.ComponentModel.DataAnnotations;

namespace Extended.Dapper.Repositories.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
    }
}