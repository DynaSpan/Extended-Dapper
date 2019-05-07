using System;
using Extended.Dapper.Attributes.Entities;

namespace Extended.Dapper.Core.Database.Entities
{
    public abstract class Entity : BaseEntity
    {
        [UpdatedAt]
        public DateTime? LastEditDate { get; set; }

        [Deleted]
        public bool Deleted { get; set; }
    }
}