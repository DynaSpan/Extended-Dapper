using System;

namespace Extended.Dapper.Repositories.Entities
{
    public abstract class BaseEntity
    {
        //[Key]
        public Guid Id { get; set; }
    }
}