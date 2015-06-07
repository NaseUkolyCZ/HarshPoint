﻿using System;
using System.Linq;
using System.Linq.Expressions;
using HarshPoint.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace HarshPoint
{
    public interface IHarshCloneable
    {
        Object Clone();
    }

    public static class HarshCloneable
    {
        public static T With<T>(this T cloneable, Action<T> modifier)
            where T : IHarshCloneable
        {
            if (cloneable == null)
            {
                throw Error.ArgumentNull(nameof(cloneable));
            }

            if (modifier == null)
            {
                throw Error.ArgumentNull(nameof(modifier));
            }

            var clone = (T)cloneable.Clone();
            modifier(clone);
            return clone;
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static T With<T, TValue>(this T cloneable, Expression<Func<T, TValue>> expression, TValue value)
            where T : IHarshCloneable
        {
            if (cloneable == null)
            {
                throw Error.ArgumentNull(nameof(cloneable));
            }

            if (expression == null)
            {
                throw Error.ArgumentNull(nameof(expression));
            }

            var oldValue = expression.Compile()(cloneable);

            if (Equals(oldValue, value))
            {
                return cloneable;
            }

            var field = expression.TryExtractSingleFieldAccess();
            if (field != null)
            {
                return With(
                    cloneable,
                    clone => field.SetValue(clone, value)
                );
            }

            var property = expression.TryExtractSinglePropertyAccess();
            if (property != null)
            {
                return With(
                    cloneable,
                    clone => property.SetValue(clone, value)
                );
            }

            throw Error.ArgumentOutOfRangeFormat(
                nameof(expression),
                SR.HarshCloneable_ExpressionNotFieldOrProperty,
                expression
            );
        }
    }
}