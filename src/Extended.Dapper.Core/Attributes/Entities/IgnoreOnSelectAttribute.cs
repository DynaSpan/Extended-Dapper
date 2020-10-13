using System;

namespace Extended.Dapper.Core.Attributes.Entities
{
    /// <summary>
    /// Will not select this field when executing Get(), GetAll() or GetById()
    /// </summary>
    public sealed class IgnoreOnSelectAttribute : Attribute
    {

    }
}