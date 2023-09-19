using System;

namespace NotionTaskSync.Tests
{
    /// <summary>
    /// Extension methods that make it easier to invoke the existing string‑extension related tests.
    /// Provides a fluent API for executing test suites on <see cref="StringExtensionsTests"/>.
    /// </summary>
    public static class StringExtensionsTestsExtensions
    {
        /// <summary>
        /// Executes the truncate test.
        /// </summary>
        /// <param name="test">The test instance to execute the method on.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> is null.</exception>
        public static void RunTruncateTest(this StringExtensionsTests test)
        {
            ArgumentNullException.ThrowIfNull(test);
            test.Truncate_StringLongerThanMaxLength_ReturnsTruncatedWithDefaultSuffix();
        }

        /// <summary>
        /// Executes both sanitize‑for‑filename tests.
        /// </summary>
        /// <param name="test">The test instance to execute the methods on.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> is null.</exception>
        public static void RunSanitizeTests(this StringExtensionsTests test)
        {
            ArgumentNullException.ThrowIfNull(test);
            test.SanitizeForFilename_EmptyString_ReturnsUntitled();
            test.SanitizeForFilename_StringWithSpaces_ReplacesSpacesWithUnderscores();
        }

        /// <summary>
        /// Executes the Pascal‑to‑snake‑case test.
        /// </summary>
        /// <param name="test">The test instance to execute the method on.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> is null.</exception>
        public static void RunToSnakeCaseTest(this StringExtensionsTests test)
        {
            ArgumentNullException.ThrowIfNull(test);
            test.ToSnakeCase_PascalCaseString_ReturnsLowercaseWithUnderscores();
        }

        /// <summary>
        /// Executes the slug‑generation test.
        /// </summary>
        /// <param name="test">The test instance to execute the method on.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> is null.</exception>
        public static void RunToSlugTest(this StringExtensionsTests test)
        {
            ArgumentNullException.ThrowIfNull(test);
            test.ToSlug_StringWithPunctuationAndSpaces_ReturnsCleanHyphenatedSlug();
        }
    }
}