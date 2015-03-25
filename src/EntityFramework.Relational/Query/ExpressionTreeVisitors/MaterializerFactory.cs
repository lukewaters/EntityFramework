// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Relational.Query.Expressions;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;
using Remotion.Linq.Clauses;

namespace Microsoft.Data.Entity.Relational.Query.ExpressionTreeVisitors
{
    public class MaterializerFactory
    {
        private readonly IEntityMaterializerSource _entityMaterializerSource;

        public MaterializerFactory([NotNull] IEntityMaterializerSource entityMaterializerSource)
        {
            Check.NotNull(entityMaterializerSource, nameof(entityMaterializerSource));

            _entityMaterializerSource = entityMaterializerSource;
        }

        public virtual Expression CreateMaterializer(
            [NotNull] IEntityType entityType,
            [NotNull] SelectExpression selectExpression,
            [NotNull] Func<IProperty, SelectExpression, int> projectionAdder,
            [CanBeNull] IQuerySource querySource)
        {
            Check.NotNull(entityType, nameof(entityType));
            Check.NotNull(selectExpression, nameof(selectExpression));
            Check.NotNull(projectionAdder, nameof(projectionAdder));

            var valueReaderParameter
                = Expression.Parameter(typeof(IValueReader));

            var concreteEntityTypes
                = entityType.GetConcreteTypesInHierarchy().ToArray();

            var indexMap = new int[concreteEntityTypes[0].GetProperties().Count()];
            var propertyIndex = 0;

            foreach (var property in concreteEntityTypes[0].GetProperties())
            {
                indexMap[propertyIndex++]
                    = projectionAdder(property, selectExpression);
            }

            var materializer
                = _entityMaterializerSource
                    .CreateMaterializeExpression(
                        concreteEntityTypes[0], valueReaderParameter, indexMap);

            if (concreteEntityTypes.Length == 1
                && concreteEntityTypes[0].RootType() == concreteEntityTypes[0])
            {
                return Expression.Lambda<Func<IValueReader, object>>(materializer, valueReaderParameter);
            }

            var discriminatorProperty
                = concreteEntityTypes[0].Relational().DiscriminatorProperty;

            var discriminatorColumn
                = selectExpression.Projection
                    .OfType<AliasExpression>()
                    .Single(c => c.ColumnProperty == discriminatorProperty);

            var firstDiscriminatorValue
                = Expression.Constant(
                    concreteEntityTypes[0].Relational().DiscriminatorValue);

            var discriminatorPredicate
                = Expression.Equal(discriminatorColumn, firstDiscriminatorValue);

            if (concreteEntityTypes.Length == 1)
            {
                selectExpression.Predicate
                    = new DiscriminatorPredicateExpression(discriminatorPredicate, querySource);

                return Expression.Lambda<Func<IValueReader, object>>(materializer, valueReaderParameter);
            }

            var discriminatorValueVariable
                = Expression.Variable(discriminatorProperty.ClrType);

            var returnLabelTarget = Expression.Label(typeof(object));

            var blockExpressions
                = new Expression[]
                    {
                        Expression.Assign(
                            discriminatorValueVariable,
                            _entityMaterializerSource
                                .CreateReadValueExpression(
                                    valueReaderParameter,
                                    discriminatorProperty.ClrType,
                                    discriminatorProperty.Index)),
                        Expression.IfThenElse(
                            Expression.Equal(discriminatorValueVariable, firstDiscriminatorValue),
                            Expression.Return(returnLabelTarget, materializer),
                            Expression.Throw(
                                Expression.Call(
                                    _createUnableToDiscriminateException,
                                    Expression.Constant(concreteEntityTypes[0])))),
                        Expression.Label(
                            returnLabelTarget,
                            Expression.Default(returnLabelTarget.Type))
                    };

            foreach (var concreteEntityType in concreteEntityTypes.Skip(1))
            {
                indexMap = new int[concreteEntityType.GetProperties().Count()];
                propertyIndex = 0;

                foreach (var property in concreteEntityType.GetProperties())
                {
                    indexMap[propertyIndex++]
                        = projectionAdder(property, selectExpression);
                }

                var discriminatorValue
                    = Expression.Constant(
                        concreteEntityType.Relational().DiscriminatorValue);

                materializer
                    = _entityMaterializerSource
                        .CreateMaterializeExpression(concreteEntityType, valueReaderParameter, indexMap);

                blockExpressions[1]
                    = Expression.IfThenElse(
                        Expression.Equal(discriminatorValueVariable, discriminatorValue),
                        Expression.Return(returnLabelTarget, materializer),
                        blockExpressions[1]);

                discriminatorPredicate
                    = Expression.OrElse(
                        Expression.Equal(discriminatorColumn, discriminatorValue),
                        discriminatorPredicate);
            }

            selectExpression.Predicate
                = new DiscriminatorPredicateExpression(discriminatorPredicate, querySource);

            return Expression.Lambda<Func<IValueReader, object>>(
                Expression.Block(new[] { discriminatorValueVariable }, blockExpressions),
                valueReaderParameter);
        }

        private static readonly MethodInfo _createUnableToDiscriminateException
            = typeof(MaterializerFactory).GetTypeInfo()
                .GetDeclaredMethod(nameof(CreateUnableToDiscriminateException));

        [UsedImplicitly]
        private static Exception CreateUnableToDiscriminateException(IEntityType entityType)
        {
            return new InvalidOperationException(Strings.UnableToDiscriminate(entityType.Name));
        }
    }
}
