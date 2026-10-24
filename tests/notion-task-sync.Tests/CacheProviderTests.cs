#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Unit tests for CacheProvider class.
// =============================================================================

namespace NotionTaskSync.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NotionTaskSync.Caching;
using Xunit;

public class CacheProviderTests
{
    [Fact]
    public void Set_WithoutExpiration_StoresValueUsingDefaultExpiration()
    {
        var cache = new CacheProvider(NullLogger<CacheProvider>.Instance);

        cache.Set("key", "value");

        cache.Get<string>("key").Should().Be("value");
    }
}
