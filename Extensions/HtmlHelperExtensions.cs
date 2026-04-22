using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Linq.Expressions;

// Purpose: Contains application code for HtmlHelperExtensions and its related runtime behavior.
namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public static class HtmlHelperExtensions
    {
        public static string ValidationCssClassFor<TModel, TValue>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TValue>> expression)
        {
            return string.Empty;
        }
    }
}
