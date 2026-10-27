// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Design",
    "CA1063:Implement IDisposable Correctly",
    Justification = "IDisposable is primarily used for mock verification in tests and does not need a correct implementation.",
    Scope = "namespaceanddescendants",
    Target = "~N:VPay.Gateway.Data.Sql.Tests")]
[assembly: SuppressMessage(
    "Major Code Smell",
    "S3881:\"IDisposable\" should be implemented correctly",
    Justification = "IDisposable is primarily used for mock verification in tests and does not need a correct implementation.",
    Scope = "namespaceanddescendants",
    Target = "~N:VPay.Gateway.Data.Sql.Tests")]
