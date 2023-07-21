using System;

namespace NotionTaskSync.Tests
{
    /// <summary>
    /// Extension methods that make it easier to invoke the existing string‑extension related tests.
    /// </summary>
    public static class StringExtensionsTestsExtensions
    {
        /// <summary>
        /// Executes the truncate test.
        /// </summary>
        public static void RunTruncateTest(this StringExtensionsTests test)
        {
            test.Truncate_StringLongerThanMaxLength_ReturnsTruncatedWithDefaultSuffix();
        }

        /// <summary>
        /// Executes both sanitize‑for‑filename tests.
        /// </summary>
        public static void RunSanitizeTests(this StringExtensionsTests test)
        {
            test.SanitizeForFilename_EmptyString_ReturnsUntitled();
            test.SanitizeForFilename_StringWithSpaces_ReplacesSpacesWithUnderscores();
        }

        /// <summary>
        /// Executes the Pascal‑to‑snake‑case test.
        /// </summary>
        public static void RunToSnakeCaseTest(this StringExtensionsTests test)
        {
            test.ToSnakeCase_PascalCaseString_ReturnsLowercaseWithUnderscores();
        }

        /// <summary>
        /// Executes the slug‑generation test.
        /// </summary>
        public static void RunToSlugTest(this StringExtensionsTests test)
        {
            test.ToSlug_StringWithPunctuationAndSpaces_ReturnsCleanHyphenatedSlug();
        }
    }
}
