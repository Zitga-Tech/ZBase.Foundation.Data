﻿using System;

namespace ZBase.Foundation.Data.Authoring
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class TableAttribute : Attribute
    {
        /// <summary>
        /// How the names of the sheet and its properties are serialized.
        /// </summary>
        public NamingStrategy NamingStrategy { get; }

        public Type[] Converters { get; }

        public TableAttribute(params Type[] converters)
        {
            Converters = converters;
        }

        public TableAttribute(NamingStrategy namingStrategy, params Type[] converters)
        {
            NamingStrategy = namingStrategy;
            Converters = converters;
        }
    }
}
