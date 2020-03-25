using System;

namespace Extended.Dapper.Core.Attributes.Entities
{
    /// <summary>
    /// When applied to a datetime field, will automatically
    /// update the timestamp on insertion or update
    /// </summary>
    public sealed class UpdatedAtAttribute : Attribute
    {
        public bool UseUTC { get; }

        /// <summary>
        /// Will update this field with the current timestamp at update
        /// </summary>
        /// <param name="useUTC">Should we use UTC time or local time? Defaults to UTC</param>
        public UpdatedAtAttribute(bool useUTC = true)
        {
            this.UseUTC = useUTC;
        }
    }
}