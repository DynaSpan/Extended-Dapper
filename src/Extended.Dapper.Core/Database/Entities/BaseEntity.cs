using System;
using System.ComponentModel.DataAnnotations;
using Extended.Dapper.Core.Attributes.Entities;

namespace Extended.Dapper.Core.Database.Entities
{
    public abstract class BaseEntity : IEquatable<BaseEntity>
    {
        [Key]
        [AutoValue]
        public Guid Id { get; set; }

        public bool Equals(BaseEntity other)
        {
            if (other == null)
                return false;

            return this.Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            var objOther = obj as BaseEntity;
            return this.Equals(objOther);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}