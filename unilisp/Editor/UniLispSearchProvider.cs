using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Search;

namespace UniLisp
{
    public static class SpatialProvider
    {
        internal static string type = "lisp";
        internal static string displayName = "Lisp";

        internal static LispContext m_Context;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                active = false,
                filterId = "lisp:",
                onEnable = OnEnable,
                fetchItems = (context, items, provider) => SearchItems(context, provider),
                /*
                fetchLabel = FetchLabel,
                fetchDescription = FetchDescription,
                fetchThumbnail = FetchThumbnail,
                fetchPreview = FetchPreview,
                trackSelection = TrackSelection,
                */
                isExplicitProvider = true,
            };
        }

        #region OnEnable
        static void OnEnable()
        {
            m_Context = new LispContext();
        }
        #endregion

        #region SearchItems
        static IEnumerator SearchItems(SearchContext ctx, SearchProvider provider)
        {
            var exprStr = ctx.searchQuery;
            if (string.IsNullOrEmpty(exprStr))
                yield return null;

            if (exprStr == "env:")
            {
                foreach(var r in m_Context.globalEntries)
                    yield return provider.CreateItem(r.Key, r.Key, r.Value.ToString(), null, null);
            }
            else
            {
                yield return Evaluate(ctx, provider);
            }
            
        }

        static IEnumerator Evaluate(SearchContext ctx, SearchProvider provider)
        {
            var exprStr = ctx.searchQuery;
            if (!exprStr.StartsWith("\"") && !exprStr.StartsWith("(") && exprStr.Contains(' '))
            {
                exprStr = $"({exprStr})";
            }

            LispValue result = new LispValue();
            var errorLabel = "";
            var errorDescription = "";
            try
            {
                result = m_Context.Eval(exprStr);
            }
            catch (LispSyntaxException e)
            {
                errorLabel = "Syntax Error";
                errorDescription = e.Message;
            }
            catch (LispRuntimeException e)
            {
                errorLabel = "Runtime Error";
                errorDescription = e.Message;
            }
            catch (System.Exception e)
            {
                throw e;
            }

            if (string.IsNullOrEmpty(errorLabel))
            {
                if (result.type == LispType.List)
                {
                    foreach (var v in result.listValue)
                    {
                        if (v.type == LispType.EoF)
                            continue;
                        var itemStr = v.ToString();
                        yield return provider.CreateItem(itemStr, itemStr, v.type.ToString(), null, null);
                    }
                }
                else if (result.type != LispType.EoF)
                {
                    var itemStr = result.ToString();
                    yield return provider.CreateItem(itemStr, itemStr, result.type.ToString(), null, null);
                }
            }
            else
            {
                yield return provider.CreateItem(errorLabel, errorLabel, errorDescription, null, null);
            }
        }

        #endregion


        [MenuItem("Window/Search/Lisp", priority = 1269)]
        internal static void OpenLispEditor()
        {
            var searchContext = SearchService.CreateContext("lisp", string.Empty);
            SearchService.ShowWindow(new SearchViewState(searchContext)
            {
                flags = SearchViewFlags.DisableInspectorPreview |
                        SearchViewFlags.ListView,
                title = L10n.Tr("Lisp")
            });
        }
    }
}

