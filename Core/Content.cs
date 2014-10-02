
using System;
using System.Configuration;
using System.Web;
using System.Collections.Generic;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Contains the Contents.
    /// </summary>
    public static class Content {

        /// <summary>
        /// Gets a pseudo cache item value.
        /// </summary>
        /// <param name="name">The name of the item to retrieve the value of.</param>
        /// <returns>The value of the item, or <c>null</c>.</returns>
        public static string GetPseudoCacheValue(string name) {
            return Cache.GetPseudoCacheValue(name);
        }

        /// <summary>
        /// Sets a pseudo cache item value, only if the content cache is enabled.
        /// </summary>
        /// <param name="name">The name of the item to store the value of.</param>
        /// <param name="value">The value of the item.</param>
        public static void SetPseudoCacheValue(string name, string value) {
            if(!Settings.DisableCache) {
                Cache.SetPseudoCacheValue(name, value);
            }
        }

        /// <summary>
        /// Clears the pseudo cache.
        /// </summary>
        public static void ClearPseudoCache() {
            Cache.ClearPseudoCache();
            Redirections.Clear();
        }

        /// <summary>
        /// Reads the Content of a Page.
        /// </summary>
        /// <param name="pageInfo">The Page.</param>
        /// <param name="cached">Specifies whether the page has to be cached or not.</param>
        /// <returns>The Page Content.</returns>
        public static PageContent GetPageContent(PageInfo pageInfo, bool cached) {
            PageContent result = Cache.GetPageContent(pageInfo);
            if(result == null) {
                result = pageInfo.Provider.GetContent(pageInfo);
                if(result!= null && !result.IsEmpty()) {
                    if(cached && !pageInfo.NonCached && !Settings.DisableCache) {
                        Cache.SetPageContent(pageInfo, result);
                    }
                }
            }

            // result should NEVER be null
            if(result == null) {
                Log.LogEntry("PageContent could not be retrieved for page " + pageInfo.FullName + " - returning empty", EntryType.Error, Log.SystemUsername);
                result = PageContent.GetEmpty(pageInfo);
            }

            return result;
        }

        /// <summary>
        /// Gets the formatted Page Content, properly handling content caching and the Formatting Pipeline.
        /// </summary>
        /// <param name="page">The Page to get the formatted Content of.</param>
        /// <param name="cached">Specifies whether the formatted content has to be cached or not.</param>
        /// <returns>The formatted content.</returns>
        public static string GetFormattedPageContent(PageInfo page, bool cached) {
            return GetFormattedPageContent(page, cached, new List<string>());
        }

        /// <summary>
        /// Gets the formatted Page Content, properly handling content caching and the Formatting Pipeline.
        /// </summary>
        /// <param name="page">The Page to get the formatted Content of.</param>
        /// <param name="cached">Specifies whether the formatted content has to be cached or not.</param>
        /// <param name="words">Highlight words</param>
        /// <returns>The formatted content.</returns>
        public static string GetFormattedPageContent(PageInfo page, bool cached, List<string> words)
        {
            string content = Cache.GetFormattedPageContent(page);
            if(content == null) {
                PageContent pg = GetPageContent(page, cached);
                string[] linkedPages;
                content = FormattingPipeline.FormatWithPhase1And2(pg.Content, false, FormattingContext.PageContent, page, out linkedPages);
                pg.LinkedPages = linkedPages;
                if(!pg.IsEmpty() && cached && !page.NonCached && !Settings.DisableCache) {
                    Cache.SetFormattedPageContent(page, content);
                }
            }

            content = HighlightContent(content, words, false);

            return FormattingPipeline.FormatWithPhase3(content, FormattingContext.PageContent, page);
        }

        private static readonly Regex FullCodeRegex = new Regex(@"\<pre class='brush(.|\n|\r)+?\<\/pre\>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex AHrefRegex = new Regex(@"\<a (.|\n|\r)+?\<\/a\>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>
        /// Highlight page content
        /// </summary>
        /// <param name="content">Page content</param>
        /// <param name="words">Words to highlight</param>
        /// <param name="title">Words are from title</param>
        /// <returns>Highlighted content</returns>
        public static string HighlightContent(string content, List<string> words, bool title)
        {
            return words.OrderBy(w => w.Length).Aggregate(content, (current, word) => HighlightText(current, word, title));
        }

        /// <summary>
        /// Checks whether words beginnings are in tag img.
        /// </summary>
        /// <param name="str">Whole text.</param>
        /// <param name="pos">Position to check.</param>
        /// <returns><c>true</c> if position is in tag img.</returns>
        public static bool IsInPicture(string str, int pos)
        {
            string myText = str;
            const string MyReg = "<img";
            const string MyReg1 = "/>";
            MatchCollection myMatch = Regex.Matches(myText, MyReg);
            foreach (Match i in myMatch)
            {
                if (i.Index < pos)
                {
                    foreach (Match match in Regex.Matches(myText, MyReg1).Cast<Match>())
                    {
                        if (match.Index > pos)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Highlight page content.
        /// </summary>
        /// <param name="content">Page content</param>
        /// <param name="word">Word to highlight</param>
        /// <param name="title">Word from title</param>
        /// <returns>Highlighted content</returns>
        private static string HighlightText(string content, string word, bool title)
        {
            const string highlightTitleOpen = "<b class=\"searchkeywordtitle\">";
            const string highlightOpen = "<b class=\"searchkeyword\">";
            const string highlightClose = "</b>";

            var noWikiBegin = new List<int>();
            var noWikiEnd = new List<int>();

            var matches = FullCodeRegex.Matches(content);

            foreach (Match match in matches)
            {
                noWikiBegin.Add(match.Index);
                noWikiEnd.Add(match.Index + match.Length);
            }

            var aHrefBegin = new List<int>();
            var aHrefEnd = new List<int>();

            var matchesAhref = AHrefRegex.Matches(content);

            foreach (Match match in matchesAhref)
            {
                aHrefBegin.Add(match.Index);
                aHrefEnd.Add(match.Index + match.Length);
            }

            var currentPosition = content.Length - 1;

            string newContent = string.Empty;

            // At the page bottom generator will put the follwoing:

            // <!-- START GreenIcicle code syntax highlighter -->
            // <link href='/sh/styles/shCore.css' rel='stylesheet' type='text/css'/>
            // <link href='/sh/styles/shThemeDefault.css' rel='stylesheet' type='text/css'/>
            // <script src='/sh/scripts/shCore.js' type='text/javascript'></script>
            // <script src='/sh/scripts/shBrushJScript.js' type='text/javascript'></script>
            // <script src='/sh/scripts/shBrushCSharp.js' type='text/javascript'></script>
            // <script language='javascript'>
            // SyntaxHighlighter.config.clipboardSwf = '/sh/scripts/clipboard.swf'
            // SyntaxHighlighter.all();
            // </script>
            // <!-- END GreenIcicle code syntax highlighter -->

            // If a word is in box for syntax highlighter, syntax highlighter plugin will be down.
            // To avoid this, this text block will be excluded from the text to be highlighted.
            var lastPart = content.LastIndexOf("<!-- START GreenIcicle code syntax highlighter -->",
                                           StringComparison.Ordinal);
            if (lastPart != -1)
            {
                newContent = content.Substring(lastPart);
                content = content.Remove(lastPart);
            }

            while (currentPosition >= 0)
            {
                var position = content.LastIndexOf(word, StringComparison.InvariantCultureIgnoreCase);
                if (position == -1)
                {
                    break;
                }

                bool needReplace = true;

                // Check whether a word is in hyperref. If so, it will be ignored.
                var inHref = aHrefBegin.Where((t, j) => (t < position) && (aHrefEnd[j] > position)).Any();

                // Check whether a word is in image link.
                var inImg = IsInPicture(content + newContent, position);

                if (inImg || inHref)
                {
                    needReplace = false;
                }
                else
                {
                    for (int i = 0; i < noWikiBegin.Count; i++)
                    {
                        var begin = noWikiBegin[i];
                        var end = noWikiEnd[i];

                        if ((begin < position) && (position < end))
                        {
                            needReplace = false;
                            break;
                        }
                    }
                }

                if (needReplace)
                {
                    var currentWord = content.Substring(position, word.Length);
                    newContent = string.Format("{0}{1}{2}{3}{4}", title ? highlightTitleOpen : highlightOpen, currentWord, highlightClose, content.Substring(position + word.Length), newContent);
                    content = content.Remove(position);
                }
                else
                {
                    newContent = content.Substring(position) + newContent;
                    content = content.Remove(position);
                }

                currentPosition = position;
            }

            return content + newContent;
        }

        /// <summary>
        /// Invalidates the cached Content of a Page.
        /// </summary>
        /// <param name="pageInfo">The Page to invalidate the cached content of.</param>
        public static void InvalidatePage(PageInfo pageInfo) {
            Cache.RemovePage(pageInfo);
            Redirections.WipePageOut(pageInfo);
        }

        /// <summary>
        /// Invalidates all the cache Contents.
        /// </summary>
        public static void InvalidateAllPages() {
            Cache.ClearPageCache();
            Redirections.Clear();
        }

    }

}
