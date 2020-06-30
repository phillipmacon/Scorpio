﻿using System.Collections.Generic;

namespace System.Linq.Expressions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDelegate"></typeparam>
    public sealed class TranslatePathMapper<TDelegate>
    {
        private readonly Expression<TDelegate> _predicate;
        private readonly List<LambdaExpression> _expressions = new List<LambdaExpression>();

        internal TranslatePathMapper(Expression<TDelegate> predicate)
        {
            _predicate = predicate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTranslatedSource"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public TranslatePathMapper<TDelegate> Map<TSource, TTranslatedSource>(Expression<Func<TTranslatedSource, TSource>> path)
        {
            _expressions.Add(path);
            return this;
        }

        internal (Expression expression, ParameterExpression[] parameters) MergeExpressionAndParameters()
        {
            var parameters = new ParameterExpression[_predicate.Parameters.Count];
            _predicate.Parameters.CopyTo(parameters, 0);
            var expression = _predicate.Body;
            for (int i = 0; i < _predicate.Parameters.Count; i++)
            {
                var s = _predicate.Parameters[i];
                var path = _expressions.Find(e => e.ReturnType == s.Type);
                if (path == null)
                {
                    continue;
                }
                parameters[i] = path.Parameters[0];
                var binder = new ReplaceExpressionVisitor(s, path.Body);
                expression = binder.Visit(expression);
            }
            return (expression, parameters);
        }

    }
}
