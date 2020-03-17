using System;

namespace Extended.Dapper.Core.Attributes.Entities
{
    /// <summary>
    /// Will not insert this field when executing an insert query
    /// </summary>
    public sealed class IgnoreOnInsertAttribute : Attribute
    {

    }
}