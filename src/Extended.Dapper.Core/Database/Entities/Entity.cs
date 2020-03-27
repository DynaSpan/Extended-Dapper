using System;
using Extended.Dapper.Core.Attributes.Entities;

namespace Extended.Dapper.Core.Database.Entities
{
    public abstract class Entity : BaseEntity
    {
        [UpdatedAt]
        public virtual DateTime? UpdatedAt { get; set; }

        [Deleted]
        public virtual bool Deleted { get; set; }
    }
}