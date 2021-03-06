// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Metadata.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class SkipNavigation : PropertyBase, IMutableSkipNavigation, IConventionSkipNavigation
    {
        private ConfigurationSource _configurationSource;
        private ConfigurationSource? _foreignKeyConfigurationSource;
        private ConfigurationSource? _inverseConfigurationSource;

        // Warning: Never access these fields directly as access needs to be thread-safe
        private IClrCollectionAccessor _collectionAccessor;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SkipNavigation(
            [NotNull] string name,
            [CanBeNull] PropertyInfo propertyInfo,
            [CanBeNull] FieldInfo fieldInfo,
            [NotNull] EntityType declaringEntityType,
            [NotNull] EntityType targetEntityType,
            [CanBeNull] ForeignKey foreignKey,
            bool collection,
            bool onDependent,
            ConfigurationSource configurationSource)
            : base(name, propertyInfo, fieldInfo)
        {
            Check.NotNull(declaringEntityType, nameof(declaringEntityType));
            Check.NotNull(targetEntityType, nameof(targetEntityType));

            DeclaringEntityType = declaringEntityType;
            TargetEntityType = targetEntityType;
            IsCollection = collection;
            IsOnDependent = onDependent;
            _configurationSource = configurationSource;
            Builder = new InternalSkipNavigationBuilder(this, targetEntityType.Model.Builder);

            if (foreignKey != null)
            {
                var expectedEntityType = IsOnDependent ? foreignKey.DeclaringEntityType : foreignKey.PrincipalEntityType;
                if (expectedEntityType != DeclaringEntityType)
                {
                    var message = IsOnDependent
                        ? CoreStrings.SkipNavigationWrongDependentType(
                            Name, DeclaringEntityType.DisplayName(), expectedEntityType.DisplayName(), foreignKey.Properties.Format())
                        : CoreStrings.SkipNavigationWrongPrincipalType(
                            Name, DeclaringEntityType.DisplayName(), expectedEntityType.DisplayName(), foreignKey.Properties.Format());
                    throw new InvalidOperationException(message);
                }

                ProcessForeignKey(foreignKey);
                UpdateForeignKeyConfigurationSource(configurationSource);
            }
        }

        private void ProcessForeignKey(ForeignKey foreignKey)
        {
            ForeignKey = foreignKey;

            if (foreignKey.ReferencingSkipNavigations == null)
            {
                foreignKey.ReferencingSkipNavigations = new SortedSet<SkipNavigation>(SkipNavigationComparer.Instance) { this };
            }
            else
            {
                foreignKey.ReferencingSkipNavigations.Add(this);
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override Type ClrType => this.GetIdentifyingMemberInfo()?.GetMemberType();

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual InternalSkipNavigationBuilder Builder { get; [param: CanBeNull] set; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual EntityType DeclaringEntityType { get; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual EntityType TargetEntityType { get; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override TypeBase DeclaringType => DeclaringEntityType;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual EntityType AssociationEntityType => IsOnDependent ? ForeignKey?.PrincipalEntityType : ForeignKey?.DeclaringEntityType;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ForeignKey ForeignKey { get; [param: CanBeNull] private set; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual SkipNavigation Inverse { get; [param: CanBeNull] private set; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool IsCollection { get; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool IsOnDependent { get; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource GetConfigurationSource() => _configurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool UpdateConfigurationSource(ConfigurationSource configurationSource)
        {
            var oldConfigurationSource = _configurationSource;
            _configurationSource = configurationSource.Max(_configurationSource);
            return _configurationSource != oldConfigurationSource;
        }
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ForeignKey SetForeignKey([CanBeNull] ForeignKey foreignKey, ConfigurationSource configurationSource)
        {
            var oldForeignKey = ForeignKey;
            var isChanging = foreignKey != ForeignKey;

            if (oldForeignKey != null)
            {
                oldForeignKey.ReferencingSkipNavigations.Remove(this);
            }

            if (foreignKey == null)
            {
                ForeignKey = null;
                _foreignKeyConfigurationSource = null;

                return isChanging
                    ? (ForeignKey)DeclaringEntityType.Model.ConventionDispatcher
                        .OnSkipNavigationForeignKeyChanged(Builder, foreignKey, oldForeignKey)
                    : foreignKey;
            }

            var expectedEntityType = IsOnDependent ? foreignKey.DeclaringEntityType : foreignKey.PrincipalEntityType;
            if (expectedEntityType != DeclaringEntityType)
            {
                var message = IsOnDependent
                    ? CoreStrings.SkipNavigationForeignKeyWrongDependentType(
                        foreignKey.Properties.Format(), Name, DeclaringEntityType.DisplayName(), expectedEntityType.DisplayName())
                    : CoreStrings.SkipNavigationForeignKeyWrongPrincipalType(
                        foreignKey.Properties.Format(), Name, DeclaringEntityType.DisplayName(), expectedEntityType.DisplayName());
                throw new InvalidOperationException(message);
            }

            ProcessForeignKey(foreignKey);
            UpdateForeignKeyConfigurationSource(configurationSource);

            if (Inverse?.AssociationEntityType != null
                && Inverse.AssociationEntityType != AssociationEntityType)
            {
                throw new InvalidOperationException(CoreStrings.SkipInverseMismatchedForeignKey(
                   foreignKey.Properties.Format(),
                   Name, AssociationEntityType.DisplayName(),
                   Inverse.Name, Inverse.AssociationEntityType.DisplayName()));
            }

            return isChanging
                ? (ForeignKey)DeclaringEntityType.Model.ConventionDispatcher
                    .OnSkipNavigationForeignKeyChanged(Builder, foreignKey, oldForeignKey)
                : foreignKey;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource? GetForeignKeyConfigurationSource()
            => _foreignKeyConfigurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void UpdateForeignKeyConfigurationSource(ConfigurationSource configurationSource)
            => _foreignKeyConfigurationSource = _foreignKeyConfigurationSource.Max(configurationSource);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual SkipNavigation SetInverse([CanBeNull] SkipNavigation inverse, ConfigurationSource configurationSource)
        {
            var oldInverse = Inverse;
            var isChanging = inverse != Inverse;
            if (inverse == null)
            {
                Inverse = null;
                _inverseConfigurationSource = null;

                return isChanging
                    ? (SkipNavigation)DeclaringEntityType.Model.ConventionDispatcher
                        .OnSkipNavigationInverseChanged(Builder, inverse, oldInverse)
                    : inverse;
            }

            if (inverse.DeclaringEntityType != TargetEntityType)
            {
                throw new InvalidOperationException(CoreStrings.SkipNavigationWrongInverse(
                    inverse.Name, inverse.DeclaringEntityType.DisplayName(), Name, TargetEntityType.DisplayName()));
            }

            if (inverse.AssociationEntityType != null
                && AssociationEntityType != null
                && inverse.AssociationEntityType != AssociationEntityType)
            {
                throw new InvalidOperationException(CoreStrings.SkipInverseMismatchedAssociationType(
                    inverse.Name, inverse.AssociationEntityType.DisplayName(), Name, AssociationEntityType.DisplayName()));
            }

            Inverse = inverse;
            UpdateInverseConfigurationSource(configurationSource);

            return isChanging
                ? (SkipNavigation)DeclaringEntityType.Model.ConventionDispatcher
                    .OnSkipNavigationInverseChanged(Builder, inverse, oldInverse)
                : inverse;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource? GetInverseConfigurationSource()
            => _inverseConfigurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void UpdateInverseConfigurationSource(ConfigurationSource configurationSource)
            => _inverseConfigurationSource = _inverseConfigurationSource.Max(configurationSource);

        /// <summary>
        ///     Runs the conventions when an annotation was set or removed.
        /// </summary>
        /// <param name="name"> The key of the set annotation. </param>
        /// <param name="annotation"> The annotation set. </param>
        /// <param name="oldAnnotation"> The old annotation. </param>
        /// <returns> The annotation that was set. </returns>
        protected override IConventionAnnotation OnAnnotationSet(
            string name, IConventionAnnotation annotation, IConventionAnnotation oldAnnotation)
            => DeclaringType.Model.ConventionDispatcher.OnSkipNavigationAnnotationChanged(
                Builder, name, annotation, oldAnnotation);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IClrCollectionAccessor CollectionAccessor
            => NonCapturingLazyInitializer.EnsureInitialized(
                ref _collectionAccessor, this, n => new ClrCollectionAccessorFactory().Create(n));

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual DebugView DebugView
            => new DebugView(
                () => this.ToDebugString(MetadataDebugStringOptions.ShortDefault),
                () => this.ToDebugString(MetadataDebugStringOptions.LongDefault));

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        [DebuggerStepThrough]
        public override string ToString() => this.ToDebugString(MetadataDebugStringOptions.SingleLineDefault);

        IConventionSkipNavigationBuilder IConventionSkipNavigation.Builder
        {
            [DebuggerStepThrough]
            get => Builder;
        }

        IEntityType INavigationBase.DeclaringEntityType
        {
            [DebuggerStepThrough]
            get => DeclaringEntityType;
        }

        IEntityType INavigationBase.TargetEntityType
        {
            [DebuggerStepThrough]
            get => TargetEntityType;
        }

        IForeignKey ISkipNavigation.ForeignKey
        {
            [DebuggerStepThrough]
            get => ForeignKey;
        }

        [DebuggerStepThrough]
        IMutableForeignKey IMutableSkipNavigation.SetForeignKey([CanBeNull] IMutableForeignKey foreignKey)
            => SetForeignKey((ForeignKey)foreignKey, ConfigurationSource.Explicit);

        [DebuggerStepThrough]
        IConventionForeignKey IConventionSkipNavigation.SetForeignKey([CanBeNull] IConventionForeignKey foreignKey, bool fromDataAnnotation)
            => SetForeignKey((ForeignKey)foreignKey, fromDataAnnotation ? ConfigurationSource.DataAnnotation : ConfigurationSource.Convention);

        ISkipNavigation ISkipNavigation.Inverse
        {
            [DebuggerStepThrough]
            get => Inverse;
        }

        [DebuggerStepThrough]
        IMutableSkipNavigation IMutableSkipNavigation.SetInverse([CanBeNull] IMutableSkipNavigation inverse)
            => SetInverse((SkipNavigation)inverse, ConfigurationSource.Explicit);

        [DebuggerStepThrough]
        IConventionSkipNavigation IConventionSkipNavigation.SetInverse([CanBeNull] IConventionSkipNavigation inverse, bool fromDataAnnotation)
            => SetInverse((SkipNavigation)inverse, fromDataAnnotation ? ConfigurationSource.DataAnnotation : ConfigurationSource.Convention);
    }
}
