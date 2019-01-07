﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace TestRunner
{
    public class AvailabilityTests
    {
        public static readonly Type ArrayList = typeof(ArrayList);
        public static readonly Type BlockingCollection = typeof(BlockingCollection<int>);
        public static readonly Type Comparer = typeof(Comparer);
        public static readonly Type ConcurrentBag = typeof(ConcurrentBag<int>);
        public static readonly Type ContractAbbreviatorAttribute = typeof(ContractAbbreviatorAttribute);
        public static readonly Type DisplayAttribute = typeof(DisplayAttribute);
        public static readonly Type DynamicAttribute = typeof(DynamicAttribute);
        public static readonly Type Hashtable = typeof(Hashtable);
        public static readonly Type HostProtectionAttribute = typeof(HostProtectionAttribute);
        public static readonly Type HostProtectionResource = typeof(HostProtectionResource);
        public static readonly Type IFormatterConverter = typeof(IFormatterConverter);
        public static readonly Type PureAttribute = typeof(PureAttribute);
        public static readonly Type ReliabilityContractAttribute = typeof(ReliabilityContractAttribute);
        public static readonly Type SecurityAction = typeof(SecurityAction);
        public static readonly Type SecurityElement = typeof(SecurityElement);
        public static readonly Type SecurityPermissionAttribute = typeof(SecurityPermissionAttribute);
        public static readonly Type SerializableAttribute = typeof(SerializableAttribute);
        public static readonly Type SerializationInfo = typeof(SerializationInfo);
        public static readonly Type StreamingContext = typeof(StreamingContext);
    }
}