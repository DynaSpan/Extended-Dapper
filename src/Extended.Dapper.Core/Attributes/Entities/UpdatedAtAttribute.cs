using System;

namespace Extended.Dapper.Core.Attributes.Entities
{
    /// <summary>
    /// When applied to a datetime field, will automatically
    /// update the timestamp on insertion or update
    /// </summary>
    public sealed class UpdatedAtAttribute : Attribute
    {

    }
}