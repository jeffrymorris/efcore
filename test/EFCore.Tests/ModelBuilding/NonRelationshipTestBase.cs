// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.TestUtilities.Xunit;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Xunit;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.ModelBuilding
{
    public abstract partial class ModelBuilderTest
    {
        public abstract class NonRelationshipTestBase : ModelBuilderTestBase
        {
            [ConditionalFact]
            public void Can_set_model_annotation()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                modelBuilder = modelBuilder.HasAnnotation("Fus", "Ro");

                Assert.NotNull(modelBuilder);
                Assert.Equal("Ro", model.FindAnnotation("Fus").Value);
            }

            [ConditionalFact]
            public void Model_is_readonly_after_Finalize()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.FinalizeModel();

                Assert.ThrowsAny<Exception>(() => modelBuilder.HasAnnotation("Fus", "Ro"));
            }

            [ConditionalFact]
            public virtual void Can_get_entity_builder_for_clr_type()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                var entityBuilder = modelBuilder.Entity<Customer>();

                Assert.NotNull(entityBuilder);
                Assert.Equal(typeof(Customer).FullName, model.FindEntityType(typeof(Customer)).Name);
            }

            [ConditionalFact]
            public virtual void Can_set_entity_key_from_clr_property()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                modelBuilder.Entity<Customer>().HasKey(b => b.Id);

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(1, entity.FindPrimaryKey().Properties.Count);
                Assert.Equal(Customer.IdProperty.Name, entity.FindPrimaryKey().Properties.First().Name);
            }

            [ConditionalFact]
            public virtual void Entity_key_on_shadow_property_is_discovered_by_convention()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<Order>().Property<int>("Id");
                modelBuilder.Entity<Customer>();
                modelBuilder.Ignore<Product>();

                var entity = modelBuilder.Model.FindEntityType(typeof(Order));

                modelBuilder.FinalizeModel();
                Assert.Equal("Id", entity.FindPrimaryKey().Properties.Single().Name);
            }

            [ConditionalFact]
            public virtual void Entity_key_on_secondary_property_is_discovered_by_convention_when_first_ignored()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<SelfRef>()
                    .Ignore(s => s.SelfRef1)
                    .Ignore(s => s.SelfRef2)
                    .Ignore(s => s.Id);

                modelBuilder.FinalizeModel();
                var entity = modelBuilder.Model.FindEntityType(typeof(SelfRef));
                Assert.Equal(nameof(SelfRef.SelfRefId), entity.FindPrimaryKey().Properties.Single().Name);
            }

            [ConditionalFact]
            public virtual void Can_set_entity_key_from_property_name_when_no_clr_property()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                modelBuilder.Entity<Customer>(
                    b =>
                    {
                        b.Property<int>(Customer.IdProperty.Name + 1);
                        b.Ignore(p => p.Details);
                        b.Ignore(p => p.Orders);
                        b.HasKey(Customer.IdProperty.Name + 1);
                    });

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(1, entity.FindPrimaryKey().Properties.Count);
                Assert.Equal(Customer.IdProperty.Name + 1, entity.FindPrimaryKey().Properties.First().Name);
            }

            [ConditionalFact]
            public virtual void Can_set_entity_key_from_clr_property_when_property_ignored()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>(
                    b =>
                    {
                        b.Ignore(Customer.IdProperty.Name);
                        b.HasKey(e => e.Id);
                    });

                var entity = modelBuilder.Model.FindEntityType(typeof(Customer));

                Assert.Equal(1, entity.FindPrimaryKey().Properties.Count);
                Assert.Equal(Customer.IdProperty.Name, entity.FindPrimaryKey().Properties.First().Name);
            }

            [ConditionalFact]
            public virtual void Can_set_composite_entity_key_from_clr_properties()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                modelBuilder
                    .Entity<Customer>()
                    .HasKey(
                        e => new { e.Id, e.Name });

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(2, entity.FindPrimaryKey().Properties.Count);
                Assert.Equal(Customer.IdProperty.Name, entity.FindPrimaryKey().Properties.First().Name);
                Assert.Equal(Customer.NameProperty.Name, entity.FindPrimaryKey().Properties.Last().Name);
            }

            [ConditionalFact]
            public virtual void Can_set_composite_entity_key_from_property_names_when_mixed_properties()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Ignore<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                modelBuilder.Entity<Customer>(
                    b =>
                    {
                        b.Property<string>(Customer.NameProperty.Name + "Shadow");
                        b.HasKey(Customer.IdProperty.Name, Customer.NameProperty.Name + "Shadow");
                    });

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(2, entity.FindPrimaryKey().Properties.Count);
                Assert.Equal(Customer.IdProperty.Name, entity.FindPrimaryKey().Properties.First().Name);
                Assert.Equal(Customer.NameProperty.Name + "Shadow", entity.FindPrimaryKey().Properties.Last().Name);
            }

            [ConditionalFact]
            public virtual void Can_set_entity_key_with_annotations()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                var keyBuilder = modelBuilder
                    .Entity<Customer>()
                    .HasKey(
                        e => new { e.Id, e.Name });

                keyBuilder.HasAnnotation("A1", "V1")
                    .HasAnnotation("A2", "V2");

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(
                    new[] { Customer.IdProperty.Name, Customer.NameProperty.Name }, entity.FindPrimaryKey().Properties.Select(p => p.Name));
                Assert.Equal("V1", keyBuilder.Metadata["A1"]);
                Assert.Equal("V2", keyBuilder.Metadata["A2"]);
            }

            [ConditionalFact]
            public virtual void Can_upgrade_candidate_key_to_primary_key()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<Customer>().Property<int>(Customer.IdProperty.Name);
                modelBuilder.Entity<Customer>().HasAlternateKey(b => b.Name);
                modelBuilder.Ignore<OrderDetails>();
                modelBuilder.Ignore<CustomerDetails>();
                modelBuilder.Ignore<Order>();

                var entity = modelBuilder.Model.FindEntityType(typeof(Customer));
                var key = entity.FindKey(entity.FindProperty(Customer.NameProperty));

                modelBuilder.Entity<Customer>().HasKey(b => b.Name);

                modelBuilder.FinalizeModel();

                var nameProperty = entity.FindPrimaryKey().Properties.Single();
                Assert.Equal(Customer.NameProperty.Name, nameProperty.Name);
                Assert.False(nameProperty.RequiresValueGenerator());
                Assert.Equal(ValueGenerated.Never, nameProperty.ValueGenerated);

                var idProperty = (IReadOnlyProperty)entity.FindProperty(Customer.IdProperty);
                Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);
            }

            [ConditionalFact]
            public virtual void Can_set_alternate_key_from_clr_property()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                modelBuilder.Entity<Customer>().HasAlternateKey(b => b.AlternateKey);

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(
                    Customer.AlternateKeyProperty.Name,
                    entity.GetKeys().First(key => key != entity.FindPrimaryKey()).Properties.First().Name);
            }

            [ConditionalFact]
            public virtual void Can_set_alternate_key_from_property_name_when_no_clr_property()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                modelBuilder.Entity<Customer>(
                    b =>
                    {
                        b.Property<int>(Customer.AlternateKeyProperty.Name + 1);
                        b.HasAlternateKey(Customer.AlternateKeyProperty.Name + 1);
                    });

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(
                    Customer.AlternateKeyProperty.Name + 1,
                    entity.GetKeys().First(key => key != entity.FindPrimaryKey()).Properties.First().Name);
            }

            [ConditionalFact]
            public virtual void Can_set_alternate_key_from_clr_property_when_property_ignored()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<Customer>(
                    b =>
                    {
                        b.Ignore(Customer.AlternateKeyProperty.Name);
                        b.HasAlternateKey(e => e.AlternateKey);
                    });

                var entity = modelBuilder.Model.FindEntityType(typeof(Customer));

                Assert.Equal(
                    Customer.AlternateKeyProperty.Name,
                    entity.GetKeys().First(key => key != entity.FindPrimaryKey()).Properties.First().Name);
            }

            [ConditionalFact]
            public virtual void Setting_alternate_key_makes_properties_required()
            {
                var modelBuilder = CreateModelBuilder();
                var entityBuilder = modelBuilder.Entity<Customer>();

                var entity = modelBuilder.Model.FindEntityType(typeof(Customer));
                var alternateKeyProperty = entity.FindProperty(nameof(Customer.Name));
                Assert.True(alternateKeyProperty.IsNullable);

                entityBuilder.HasAlternateKey(e => e.Name);

                Assert.False(alternateKeyProperty.IsNullable);
            }

            [ConditionalFact]
            public virtual void Can_set_entity_annotation()
            {
                var modelBuilder = CreateModelBuilder();

                var entityBuilder = modelBuilder
                    .Entity<Customer>()
                    .HasAnnotation("foo", "bar");

                Assert.Equal("bar", entityBuilder.Metadata["foo"]);
            }

            [ConditionalFact]
            public virtual void Can_set_property_annotation()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Ignore<Product>();
                modelBuilder
                    .Entity<Customer>()
                    .Property(c => c.Name).HasAnnotation("foo", "bar");

                var property = modelBuilder.FinalizeModel().FindEntityType(typeof(Customer)).FindProperty(nameof(Customer.Name));

                Assert.Equal("bar", property["foo"]);
            }

            [ConditionalFact]
            public virtual void Can_set_property_annotation_when_no_clr_property()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Ignore<Product>();
                modelBuilder
                    .Entity<Customer>()
                    .Property<string>(Customer.NameProperty.Name).HasAnnotation("foo", "bar");

                var property = modelBuilder.FinalizeModel().FindEntityType(typeof(Customer)).FindProperty(nameof(Customer.Name));

                Assert.Equal("bar", property["foo"]);
            }

            [ConditionalFact]
            public virtual void Can_set_property_annotation_by_type()
            {
                var modelBuilder = CreateModelBuilder(c => c.Properties<string>().HaveAnnotation("foo", "bar"));

                modelBuilder.Ignore<Product>();
                var propertyBuilder = modelBuilder
                    .Entity<Customer>()
                    .Property(c => c.Name).HasAnnotation("foo", "bar");

                var property = modelBuilder.FinalizeModel().FindEntityType(typeof(Customer)).FindProperty(nameof(Customer.Name));

                Assert.Equal("bar", property["foo"]);
            }

            [ConditionalFact]
            public virtual void Properties_are_required_by_default_only_if_CLR_type_is_nullable()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up);
                        b.Property(e => e.Down);
                        b.Property<int>("Charm");
                        b.Property<string>("Strange");
                        b.Property<int>("Top");
                        b.Property<string>("Bottom");
                    });

                var entityType = modelBuilder.FinalizeModel().FindEntityType(typeof(Quarks));

                Assert.False(entityType.FindProperty("Up").IsNullable);
                Assert.True(entityType.FindProperty("Down").IsNullable);
                Assert.False(entityType.FindProperty("Charm").IsNullable);
                Assert.True(entityType.FindProperty("Strange").IsNullable);
                Assert.False(entityType.FindProperty("Top").IsNullable);
                Assert.True(entityType.FindProperty("Bottom").IsNullable);
            }

            [ConditionalFact]
            public virtual void Properties_can_be_ignored()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Ignore(e => e.Up);
                        b.Ignore(e => e.Down);
                        b.Ignore("Charm");
                        b.Ignore("Strange");
                        b.Ignore("Top");
                        b.Ignore("Bottom");
                        b.Ignore("Shadow");
                    });

                var entityType = modelBuilder.FinalizeModel().FindEntityType(typeof(Quarks));
                Assert.Contains(nameof(Quarks.Id), entityType.GetProperties().Select(p => p.Name));
                Assert.DoesNotContain(nameof(Quarks.Up), entityType.GetProperties().Select(p => p.Name));
                Assert.DoesNotContain(nameof(Quarks.Down), entityType.GetProperties().Select(p => p.Name));
            }

            [ConditionalFact]
            public virtual void Properties_can_be_ignored_by_type()
            {
                var modelBuilder = CreateModelBuilder(c => c.IgnoreAny<Guid>());

                modelBuilder.Ignore<Product>();
                modelBuilder.Entity<Customer>();

                var entityType = modelBuilder.FinalizeModel().FindEntityType(typeof(Customer));
                Assert.Null(entityType.FindProperty(nameof(Customer.AlternateKey)));
            }

            [ConditionalFact]
            public virtual void Int32_cannot_be_ignored()
            {
                Assert.Equal(CoreStrings.UnconfigurableType("int", "Ignored"),
                    Assert.Throws<InvalidOperationException>(() => CreateModelBuilder(c => c.IgnoreAny<int>())).Message);
            }

            [ConditionalFact]
            public virtual void Object_cannot_be_ignored()
            {
                Assert.Equal(CoreStrings.UnconfigurableType("object", "Ignored"),
                    Assert.Throws<InvalidOperationException>(() => CreateModelBuilder(c => c.IgnoreAny<object>())).Message);
            }

            [ConditionalFact]
            public virtual void Can_ignore_a_property_that_is_part_of_explicit_entity_key()
            {
                var modelBuilder = CreateModelBuilder();

                var entityBuilder = modelBuilder.Entity<Customer>();
                entityBuilder.HasKey(e => e.Id);
                entityBuilder.Ignore(e => e.Id);

                Assert.Null(entityBuilder.Metadata.FindProperty(Customer.IdProperty.Name));
            }

            [ConditionalFact]
            public virtual void Can_ignore_shadow_properties_when_they_have_been_added_explicitly()
            {
                var modelBuilder = CreateModelBuilder();

                var entityBuilder = modelBuilder.Entity<Customer>();
                entityBuilder.Property<string>("Shadow");
                entityBuilder.Ignore("Shadow");

                Assert.Null(entityBuilder.Metadata.FindProperty("Shadow"));
            }

            [ConditionalFact]
            public virtual void Can_add_shadow_properties_when_they_have_been_ignored()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Ignore<Product>();
                modelBuilder.Entity<Customer>(
                    b =>
                    {
                        b.Ignore("Shadow");
                        b.Property<string>("Shadow");
                    });

                var model = modelBuilder.FinalizeModel();

                Assert.NotNull(model.FindEntityType(typeof(Customer)).FindProperty("Shadow"));
            }

            [ConditionalFact]
            public virtual void Can_override_navigations_as_properties()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Entity<Customer>();

                var customer = model.FindEntityType(typeof(Customer));
                Assert.NotNull(customer.FindNavigation(nameof(Customer.Orders)));

                modelBuilder.Entity<Customer>().Property(c => c.Orders);

                Assert.Null(customer.FindNavigation(nameof(Customer.Orders)));
                Assert.NotNull(customer.FindProperty(nameof(Customer.Orders)));
            }

            [ConditionalFact]
            public virtual void Ignoring_a_navigation_property_removes_discovered_entity_types()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>(
                    b =>
                    {
                        b.Ignore(c => c.Details);
                        b.Ignore(c => c.Orders);
                    });

                var model = modelBuilder.FinalizeModel();

                Assert.Single(model.GetEntityTypes());
            }

            [ConditionalFact]
            public virtual void Ignoring_a_navigation_property_removes_discovered_relationship()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>(
                    b =>
                    {
                        b.Ignore(c => c.Details);
                        b.Ignore(c => c.Orders);
                    });
                modelBuilder.Entity<CustomerDetails>(b => b.Ignore(c => c.Customer));

                var model = modelBuilder.FinalizeModel();

                Assert.Empty(model.GetEntityTypes().First().GetForeignKeys());
                Assert.Empty(model.GetEntityTypes().Last().GetForeignKeys());
                Assert.Equal(2, model.GetEntityTypes().Count());
            }

            [ConditionalFact]
            public virtual void Ignoring_a_base_type_removes_relationships()
            {
                var modelBuilder = CreateModelBuilder(c => c.IgnoreAny<INotifyPropertyChanged>());

                modelBuilder.Entity<Customer>();

                var model = modelBuilder.FinalizeModel();

                Assert.Empty(model.GetEntityTypes().Single().GetForeignKeys());
            }

            [ConditionalFact]
            public virtual void Properties_can_be_made_required()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up).IsRequired();
                        b.Property(e => e.Down).IsRequired();
                        b.Property<int>("Charm").IsRequired();
                        b.Property<string>("Strange").IsRequired();
                        b.Property<int>("Top").IsRequired();
                        b.Property<string>("Bottom").IsRequired();
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));

                Assert.False(entityType.FindProperty("Up").IsNullable);
                Assert.False(entityType.FindProperty("Down").IsNullable);
                Assert.False(entityType.FindProperty("Charm").IsNullable);
                Assert.False(entityType.FindProperty("Strange").IsNullable);
                Assert.False(entityType.FindProperty("Top").IsNullable);
                Assert.False(entityType.FindProperty("Bottom").IsNullable);
            }

            [ConditionalFact]
            public virtual void Properties_can_be_made_optional()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Down).IsRequired(false);
                        b.Property<string>("Strange").IsRequired(false);
                        b.Property<string>("Bottom").IsRequired(false);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));

                Assert.True(entityType.FindProperty("Down").IsNullable);
                Assert.True(entityType.FindProperty("Strange").IsNullable);
                Assert.True(entityType.FindProperty("Bottom").IsNullable);
            }

            [ConditionalFact]
            public virtual void Key_properties_cannot_be_made_optional()
            {
                Assert.Equal(
                    CoreStrings.KeyPropertyCannotBeNullable(nameof(Quarks.Down), nameof(Quarks), "{'" + nameof(Quarks.Down) + "'}"),
                    Assert.Throws<InvalidOperationException>(
                        () =>
                            CreateModelBuilder().Entity<Quarks>(
                                b =>
                                {
                                    b.HasAlternateKey(
                                        e => new { e.Down });
                                    b.Property(e => e.Down).IsRequired(false);
                                })).Message);
            }

            [ConditionalFact]
            public virtual void Non_nullable_properties_cannot_be_made_optional()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        Assert.Equal(
                            CoreStrings.CannotBeNullable("Up", "Quarks", "int"),
                            Assert.Throws<InvalidOperationException>(() => b.Property(e => e.Up).IsRequired(false)).Message);

                        Assert.Equal(
                            CoreStrings.CannotBeNullable("Charm", "Quarks", "int"),
                            Assert.Throws<InvalidOperationException>(() => b.Property<int>("Charm").IsRequired(false)).Message);

                        Assert.Equal(
                            CoreStrings.CannotBeNullable("Top", "Quarks", "int"),
                            Assert.Throws<InvalidOperationException>(() => b.Property<int>("Top").IsRequired(false)).Message);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));

                Assert.False(entityType.FindProperty("Up").IsNullable);
                Assert.False(entityType.FindProperty("Charm").IsNullable);
                Assert.False(entityType.FindProperty("Top").IsNullable);
            }

            [ConditionalFact]
            public virtual void Properties_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property<int>("Up");
                        b.Property<int>("Gluon");
                        b.Property<string>("Down");
                        b.Property<string>("Photon");
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = modelBuilder.FinalizeModel().FindEntityType(typeof(Quarks));

                Assert.False(entityType.FindProperty("Up").IsShadowProperty());
                Assert.False(entityType.FindProperty("Down").IsShadowProperty());
                Assert.True(entityType.FindProperty("Gluon").IsShadowProperty());
                Assert.True(entityType.FindProperty("Photon").IsShadowProperty());

                Assert.Equal(-1, entityType.FindProperty("Up").GetShadowIndex());
                Assert.Equal(-1, entityType.FindProperty("Down").GetShadowIndex());
                Assert.Equal(0, entityType.FindProperty("Gluon").GetShadowIndex());
                Assert.Equal(1, entityType.FindProperty("Photon").GetShadowIndex());
            }

            [ConditionalFact]
            public virtual void Properties_can_be_made_concurrency_tokens()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up).IsConcurrencyToken();
                        b.Property(e => e.Down).IsConcurrencyToken(false);
                        b.Property<int>("Charm").IsConcurrencyToken();
                        b.Property<string>("Strange").IsConcurrencyToken(false);
                        b.Property<int>("Top").IsConcurrencyToken();
                        b.Property<string>("Bottom").IsConcurrencyToken(false);
                        b.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotifications);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = modelBuilder.FinalizeModel().FindEntityType(typeof(Quarks));

                Assert.False(entityType.FindProperty(Customer.IdProperty.Name).IsConcurrencyToken);
                Assert.True(entityType.FindProperty("Up").IsConcurrencyToken);
                Assert.False(entityType.FindProperty("Down").IsConcurrencyToken);
                Assert.True(entityType.FindProperty("Charm").IsConcurrencyToken);
                Assert.False(entityType.FindProperty("Strange").IsConcurrencyToken);
                Assert.True(entityType.FindProperty("Top").IsConcurrencyToken);
                Assert.False(entityType.FindProperty("Bottom").IsConcurrencyToken);

                Assert.Equal(0, entityType.FindProperty(Customer.IdProperty.Name).GetOriginalValueIndex());
                Assert.Equal(3, entityType.FindProperty("Up").GetOriginalValueIndex());
                Assert.Equal(-1, entityType.FindProperty("Down").GetOriginalValueIndex());
                Assert.Equal(1, entityType.FindProperty("Charm").GetOriginalValueIndex());
                Assert.Equal(-1, entityType.FindProperty("Strange").GetOriginalValueIndex());
                Assert.Equal(2, entityType.FindProperty("Top").GetOriginalValueIndex());
                Assert.Equal(-1, entityType.FindProperty("Bottom").GetOriginalValueIndex());

                Assert.Equal(ChangeTrackingStrategy.ChangingAndChangedNotifications, entityType.GetChangeTrackingStrategy());
            }

            [ConditionalFact]
            public virtual void Properties_can_have_access_mode_set()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up);
                        b.Property(e => e.Down).HasField("_forDown").UsePropertyAccessMode(PropertyAccessMode.Field);
                        b.Property<int>("Charm").UsePropertyAccessMode(PropertyAccessMode.Property);
                        b.Property<string>("Strange").UsePropertyAccessMode(PropertyAccessMode.FieldDuringConstruction);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));

                Assert.Equal(PropertyAccessMode.PreferField, entityType.FindProperty("Up").GetPropertyAccessMode());
                Assert.Equal(PropertyAccessMode.Field, entityType.FindProperty("Down").GetPropertyAccessMode());
                Assert.Equal(PropertyAccessMode.Property, entityType.FindProperty("Charm").GetPropertyAccessMode());
                Assert.Equal(PropertyAccessMode.FieldDuringConstruction, entityType.FindProperty("Strange").GetPropertyAccessMode());
            }

            [ConditionalFact]
            public virtual void Access_mode_can_be_overridden_at_entity_and_property_levels()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);

                modelBuilder.Entity<Hob>(b =>
                {
                    b.HasKey(e => e.Id1);
                });
                modelBuilder.Ignore<Nob>();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.UsePropertyAccessMode(PropertyAccessMode.FieldDuringConstruction);
                        b.Property(e => e.Up).UsePropertyAccessMode(PropertyAccessMode.Property);
                        b.Property(e => e.Down).HasField("_forDown");
                    });

                var model = modelBuilder.FinalizeModel();
                Assert.Equal(PropertyAccessMode.Field, model.GetPropertyAccessMode());

                var hobsType = (IReadOnlyEntityType)model.FindEntityType(typeof(Hob));
                Assert.Equal(PropertyAccessMode.Field, hobsType.GetPropertyAccessMode());
                Assert.Equal(PropertyAccessMode.Field, hobsType.FindProperty("Id1").GetPropertyAccessMode());

                var quarksType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));
                Assert.Equal(PropertyAccessMode.FieldDuringConstruction, quarksType.GetPropertyAccessMode());
                Assert.Equal(PropertyAccessMode.FieldDuringConstruction, quarksType.FindProperty("Down").GetPropertyAccessMode());
                Assert.Equal(PropertyAccessMode.Property, quarksType.FindProperty("Up").GetPropertyAccessMode());
            }

            [ConditionalFact]
            public virtual void Properties_can_have_provider_type_set()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up);
                        b.Property(e => e.Down).HasConversion<byte[]>();
                        b.Property<int>("Charm").HasConversion(typeof(long), typeof(CustomValueComparer<int>));
                        b.Property<string>("Strange").HasConversion<byte[]>();
                        b.Property<string>("Strange").HasConversion((Type)null);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));

                var up = entityType.FindProperty("Up");
                Assert.Null(up.GetProviderClrType());
                Assert.IsType<ValueComparer.DefaultValueComparer<int>>(up.GetValueComparer());

                var down = entityType.FindProperty("Down");
                Assert.Same(typeof(byte[]), down.GetProviderClrType());
                Assert.IsType<ValueComparer.DefaultValueComparer<string>>(down.GetValueComparer());

                var charm = entityType.FindProperty("Charm");
                Assert.Same(typeof(long), charm.GetProviderClrType());
                Assert.IsType<CustomValueComparer<int>>(charm.GetValueComparer());

                var strange = entityType.FindProperty("Strange");
                Assert.Null(strange.GetProviderClrType());
                Assert.IsType<ValueComparer.DefaultValueComparer<string>>(strange.GetValueComparer());
            }

            [ConditionalFact]
            public virtual void Properties_can_have_provider_type_set_for_type()
            {
                var modelBuilder = CreateModelBuilder(c => c.Properties<string>().HaveConversion<byte[]>());

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up);
                        b.Property(e => e.Down);
                        b.Property<int>("Charm");
                        b.Property<string>("Strange");
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));

                Assert.Null(entityType.FindProperty("Up").GetProviderClrType());
                Assert.Same(typeof(byte[]), entityType.FindProperty("Down").GetProviderClrType());
                Assert.Null(entityType.FindProperty("Charm").GetProviderClrType());
                Assert.Same(typeof(byte[]), entityType.FindProperty("Strange").GetProviderClrType());
            }

            [ConditionalFact]
            public virtual void Properties_can_have_value_converter_set_non_generic()
            {
                var modelBuilder = CreateModelBuilder();

                ValueConverter stringConverter = new StringToBytesConverter(Encoding.UTF8);
                ValueConverter intConverter = new CastingConverter<int, long>();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up);
                        b.Property(e => e.Down).HasConversion(stringConverter);
                        b.Property<int>("Charm").HasConversion(intConverter);
                        b.Property<string>("Strange").HasConversion(stringConverter);
                        b.Property<string>("Strange").HasConversion((ValueConverter)null);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));

                Assert.Null(entityType.FindProperty("Up").GetValueConverter());
                Assert.Same(stringConverter, entityType.FindProperty("Down").GetValueConverter());
                Assert.Same(intConverter, entityType.FindProperty("Charm").GetValueConverter());
                Assert.Null(entityType.FindProperty("Strange").GetValueConverter());
            }

            [ConditionalFact]
            public virtual void Properties_can_have_value_converter_type_set()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up);
                        b.Property(e => e.Down).HasConversion(typeof(UTF8StringToBytesConverter));
                        b.Property<int>("Charm").HasConversion<CastingConverter<int, long>, CustomValueComparer<int>>();
                        b.Property<string>("Strange").HasConversion(typeof(UTF8StringToBytesConverter), typeof(CustomValueComparer<string>));
                        b.Property<string>("Strange").HasConversion((ValueConverter)null, null);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));

                Assert.Null(entityType.FindProperty("Up").GetValueConverter());

                var down = entityType.FindProperty("Down");
                Assert.IsType<UTF8StringToBytesConverter>(down.GetValueConverter());
                Assert.IsType<ValueComparer.DefaultValueComparer<string>>(down.GetValueComparer());

                var charm = entityType.FindProperty("Charm");
                Assert.IsType<CastingConverter<int, long>>(charm.GetValueConverter());
                Assert.IsType<CustomValueComparer<int>>(charm.GetValueComparer());

                Assert.Null(entityType.FindProperty("Strange").GetValueConverter());
                Assert.IsAssignableFrom<ValueComparer.DefaultValueComparer<string>>(entityType.FindProperty("Strange").GetValueComparer());
            }

            private class UTF8StringToBytesConverter : StringToBytesConverter
            {
                public UTF8StringToBytesConverter()
                    : base(Encoding.UTF8)
                {
                }
            }

            private class CustomValueComparer<T> : ValueComparer<T>
            {
                public CustomValueComparer()
                    : base(false)
                {
                }
            }

            [ConditionalFact]
            public virtual void Properties_can_have_value_converter_set_inline()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up);
                        b.Property(e => e.Down).HasConversion(v => v.ToCharArray(), v => new string(v));
                        b.Property<int>("Charm").HasConversion(v => (long)v, v => (int)v);
                    });

                var model = (IReadOnlyModel)modelBuilder.Model;
                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Null(entityType.FindProperty("Up").GetValueConverter());
                Assert.NotNull(entityType.FindProperty("Down").GetValueConverter());
                Assert.NotNull(entityType.FindProperty("Charm").GetValueConverter());
            }

            [ConditionalFact]
            public virtual void IEnumerable_properties_with_value_converter_set_are_not_discovered_as_navigations()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<DynamicProperty>(
                    b =>
                    {
                        b.Property(e => e.ExpandoObject).HasConversion(
                            v => (string)((IDictionary<string, object>)v)["Value"], v => DeserializeExpandoObject(v));

                        var comparer = new ValueComparer<ExpandoObject>(
                            (v1, v2) => v1.SequenceEqual(v2),
                            v => v.GetHashCode());

                        b.Property(e => e.ExpandoObject).Metadata.SetValueComparer(comparer);
                    });

                var model = modelBuilder.FinalizeModel();

                var entityType = (IReadOnlyEntityType)model.GetEntityTypes().Single();
                Assert.NotNull(entityType.FindProperty(nameof(DynamicProperty.ExpandoObject)).GetValueConverter());
                Assert.NotNull(entityType.FindProperty(nameof(DynamicProperty.ExpandoObject)).GetValueComparer());
            }

            private static ExpandoObject DeserializeExpandoObject(string value)
            {
                dynamic obj = new ExpandoObject();
                obj.Value = value;

                return obj;
            }

            private class ExpandoObjectConverter : ValueConverter<ExpandoObject, string>
            {
                public ExpandoObjectConverter()
                    : base(v => (string)((IDictionary<string, object>)v)["Value"], v => DeserializeExpandoObject(v))
                {
                }
            }

            private class ExpandoObjectComparer : ValueComparer<ExpandoObject>
            {
                public ExpandoObjectComparer()
                    : base((v1, v2) => v1.SequenceEqual(v2), v => v.GetHashCode())
                {
                }
            }

            [ConditionalFact]
            public virtual void Properties_can_have_value_converter_configured_by_type()
            {
                var modelBuilder = CreateModelBuilder(c =>
                {
                    c.Properties(typeof(IWrapped<>)).AreUnicode(false);
                    c.Properties<WrappedStringBase>().HaveMaxLength(20);
                    c.Properties<WrappedString>().HaveConversion(typeof(WrappedStringToStringConverter));
                });

                modelBuilder.Entity<WrappedStringEntity>();

                var model = modelBuilder.FinalizeModel();

                var entityType = (IReadOnlyEntityType)model.GetEntityTypes().Single();
                var wrappedProperty = entityType.FindProperty(nameof(WrappedStringEntity.WrappedString));
                Assert.False(wrappedProperty.IsUnicode());
                Assert.Equal(20, wrappedProperty.GetMaxLength());
                Assert.IsType<WrappedStringToStringConverter>(wrappedProperty.GetValueConverter());
                Assert.IsType<ValueComparer<WrappedString>>(wrappedProperty.GetValueComparer());
            }

            [ConditionalFact]
            public virtual void Value_converter_configured_on_base_type_is_not_applied()
            {
                var modelBuilder = CreateModelBuilder(c =>
                {
                    c.Properties<WrappedStringBase>().HaveConversion(typeof(WrappedStringToStringConverter));
                });

                modelBuilder.Entity<WrappedStringEntity>();

                Assert.Equal(CoreStrings.PropertyNotMapped(
                            nameof(WrappedStringEntity), nameof(WrappedStringEntity.WrappedString), nameof(WrappedString)),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder.FinalizeModel()).Message);
            }

            private interface IWrapped<T>
            {
                T Value { get; init; }
            }

            private abstract class WrappedStringBase : IWrapped<string>
            {
                public abstract string Value { get; init; }
            }

            private class WrappedString : WrappedStringBase
            {
                public override string Value { get; init; }
            }

            private class WrappedStringEntity
            {
                public int Id { get; set; }
                public WrappedString WrappedString { get; set; }
            }

            private class WrappedStringToStringConverter : ValueConverter<WrappedString, string>
            {
                public WrappedStringToStringConverter()
                    : base(v => v.Value, v => new WrappedString { Value = v })
                {
                }
            }

            [ConditionalFact]
            public virtual void Throws_for_conflicting_base_configurations_by_type()
            {
                var modelBuilder = CreateModelBuilder(c =>
                    {
                        c.Properties<WrappedString>();
                        c.IgnoreAny<IWrapped<string>>();
                    });

                Assert.Equal(CoreStrings.TypeConfigurationConflict(
                    nameof(WrappedString), "Property",
                    "IWrapped<string>", "Ignored"),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder.Entity<WrappedStringEntity>()).Message);
            }

            [ConditionalFact]
            public virtual void Value_converter_type_is_checked()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        Assert.Equal(
                            CoreStrings.ConverterPropertyMismatch("string", "Quarks", "Up", "int"),
                            Assert.Throws<InvalidOperationException>(
                                () => b.Property(e => e.Up).HasConversion(
                                    new StringToBytesConverter(Encoding.UTF8))).Message);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Quarks));
                Assert.Null(entityType.FindProperty("Up").GetValueConverter());
            }

            [ConditionalFact]
            public virtual void Properties_can_have_field_set()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property<int>("Up").HasField("_forUp");
                        b.Property(e => e.Down).HasField("_forDown");
                        b.Property<int?>("_forWierd").HasField("_forWierd");
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Equal("_forUp", entityType.FindProperty("Up").GetFieldName());
                Assert.Equal("_forDown", entityType.FindProperty("Down").GetFieldName());
                Assert.Equal("_forWierd", entityType.FindProperty("_forWierd").GetFieldName());
            }

            [ConditionalFact]
            public virtual void HasField_throws_if_field_is_not_found()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        Assert.Equal(
                            CoreStrings.MissingBackingField("_notFound", nameof(Quarks.Down), nameof(Quarks)),
                            Assert.Throws<InvalidOperationException>(() => b.Property(e => e.Down).HasField("_notFound")).Message);
                    });
            }

            [ConditionalFact]
            public virtual void HasField_throws_if_field_is_wrong_type()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        Assert.Equal(
                            CoreStrings.BadBackingFieldType("_forUp", "int", nameof(Quarks), nameof(Quarks.Down), "string"),
                            Assert.Throws<InvalidOperationException>(() => b.Property(e => e.Down).HasField("_forUp")).Message);
                    });
            }

            [ConditionalFact]
            public virtual void Properties_can_be_set_to_generate_values_on_Add()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.HasKey(e => e.Id);
                        b.Property(e => e.Up).ValueGeneratedOnAddOrUpdate();
                        b.Property(e => e.Down).ValueGeneratedNever();
                        b.Property<int>("Charm").Metadata.ValueGenerated = ValueGenerated.OnUpdateSometimes;
                        b.Property<string>("Strange").ValueGeneratedNever();
                        b.Property<int>("Top").ValueGeneratedOnAddOrUpdate();
                        b.Property<string>("Bottom").ValueGeneratedOnUpdate();
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Quarks));
                Assert.Equal(ValueGenerated.OnAdd, entityType.FindProperty(Customer.IdProperty.Name).ValueGenerated);
                Assert.Equal(ValueGenerated.OnAddOrUpdate, entityType.FindProperty("Up").ValueGenerated);
                Assert.Equal(ValueGenerated.Never, entityType.FindProperty("Down").ValueGenerated);
                Assert.Equal(ValueGenerated.OnUpdateSometimes, entityType.FindProperty("Charm").ValueGenerated);
                Assert.Equal(ValueGenerated.Never, entityType.FindProperty("Strange").ValueGenerated);
                Assert.Equal(ValueGenerated.OnAddOrUpdate, entityType.FindProperty("Top").ValueGenerated);
                Assert.Equal(ValueGenerated.OnUpdate, entityType.FindProperty("Bottom").ValueGenerated);
            }

            [ConditionalFact]
            public virtual void Properties_can_set_row_version()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.HasKey(e => e.Id);
                        b.Property(e => e.Up).IsRowVersion();
                        b.Property(e => e.Down).ValueGeneratedNever();
                        b.Property<int>("Charm").IsRowVersion();
                    });

                var model = modelBuilder.FinalizeModel();

                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Equal(ValueGenerated.OnAddOrUpdate, entityType.FindProperty("Up").ValueGenerated);
                Assert.Equal(ValueGenerated.Never, entityType.FindProperty("Down").ValueGenerated);
                Assert.Equal(ValueGenerated.OnAddOrUpdate, entityType.FindProperty("Charm").ValueGenerated);

                Assert.True(entityType.FindProperty("Up").IsConcurrencyToken);
                Assert.False(entityType.FindProperty("Down").IsConcurrencyToken);
                Assert.True(entityType.FindProperty("Charm").IsConcurrencyToken);
            }

            [ConditionalFact]
            public virtual void Can_set_max_length_for_properties()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up).HasMaxLength(0);
                        b.Property(e => e.Down).HasMaxLength(100);
                        b.Property<int>("Charm").HasMaxLength(0);
                        b.Property<string>("Strange").HasMaxLength(100);
                        b.Property<int>("Top").HasMaxLength(0);
                        b.Property<string>("Bottom").HasMaxLength(100);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Null(entityType.FindProperty(Customer.IdProperty.Name).GetMaxLength());
                Assert.Equal(0, entityType.FindProperty("Up").GetMaxLength());
                Assert.Equal(100, entityType.FindProperty("Down").GetMaxLength());
                Assert.Equal(0, entityType.FindProperty("Charm").GetMaxLength());
                Assert.Equal(100, entityType.FindProperty("Strange").GetMaxLength());
                Assert.Equal(0, entityType.FindProperty("Top").GetMaxLength());
                Assert.Equal(100, entityType.FindProperty("Bottom").GetMaxLength());
            }

            [ConditionalFact]
            public virtual void Can_set_max_length_for_property_type()
            {
                var modelBuilder = CreateModelBuilder(c =>
                {
                    c.Properties<int>().HaveMaxLength(0);
                    c.Properties<string>().HaveMaxLength(100);
                });

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property<int>("Charm");
                        b.Property<string>("Strange");
                        b.Property<int>("Top");
                        b.Property<string>("Bottom");
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Equal(0, entityType.FindProperty(Customer.IdProperty.Name).GetMaxLength());
                Assert.Equal(0, entityType.FindProperty("Up").GetMaxLength());
                Assert.Equal(100, entityType.FindProperty("Down").GetMaxLength());
                Assert.Equal(0, entityType.FindProperty("Charm").GetMaxLength());
                Assert.Equal(100, entityType.FindProperty("Strange").GetMaxLength());
                Assert.Equal(0, entityType.FindProperty("Top").GetMaxLength());
                Assert.Equal(100, entityType.FindProperty("Bottom").GetMaxLength());
            }

            [ConditionalFact]
            public virtual void Can_set_precision_and_scale_for_properties()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up).HasPrecision(1, 0);
                        b.Property(e => e.Down).HasPrecision(100, 10);
                        b.Property<int>("Charm").HasPrecision(1, 0);
                        b.Property<string>("Strange").HasPrecision(100, 10);
                        b.Property<int>("Top").HasPrecision(1, 0);
                        b.Property<string>("Bottom").HasPrecision(100, 10);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Null(entityType.FindProperty(Customer.IdProperty.Name).GetPrecision());
                Assert.Null(entityType.FindProperty(Customer.IdProperty.Name).GetScale());
                Assert.Equal(1, entityType.FindProperty("Up").GetPrecision());
                Assert.Equal(0, entityType.FindProperty("Up").GetScale());
                Assert.Equal(100, entityType.FindProperty("Down").GetPrecision());
                Assert.Equal(10, entityType.FindProperty("Down").GetScale());
                Assert.Equal(1, entityType.FindProperty("Charm").GetPrecision());
                Assert.Equal(0, entityType.FindProperty("Charm").GetScale());
                Assert.Equal(100, entityType.FindProperty("Strange").GetPrecision());
                Assert.Equal(10, entityType.FindProperty("Strange").GetScale());
                Assert.Equal(1, entityType.FindProperty("Top").GetPrecision());
                Assert.Equal(0, entityType.FindProperty("Top").GetScale());
                Assert.Equal(100, entityType.FindProperty("Bottom").GetPrecision());
                Assert.Equal(10, entityType.FindProperty("Bottom").GetScale());
            }

            [ConditionalFact]
            public virtual void Can_set_precision_and_scale_for_property_type()
            {
                var modelBuilder = CreateModelBuilder(c =>
                {
                    c.Properties<int>().HavePrecision(1, 0);
                    c.Properties<string>().HavePrecision(100, 10);
                });

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property<int>("Charm");
                        b.Property<string>("Strange");
                        b.Property<int>("Top");
                        b.Property<string>("Bottom");
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Equal(1, entityType.FindProperty(Customer.IdProperty.Name).GetPrecision());
                Assert.Equal(0, entityType.FindProperty(Customer.IdProperty.Name).GetScale());
                Assert.Equal(1, entityType.FindProperty("Up").GetPrecision());
                Assert.Equal(0, entityType.FindProperty("Up").GetScale());
                Assert.Equal(100, entityType.FindProperty("Down").GetPrecision());
                Assert.Equal(10, entityType.FindProperty("Down").GetScale());
                Assert.Equal(1, entityType.FindProperty("Charm").GetPrecision());
                Assert.Equal(0, entityType.FindProperty("Charm").GetScale());
                Assert.Equal(100, entityType.FindProperty("Strange").GetPrecision());
                Assert.Equal(10, entityType.FindProperty("Strange").GetScale());
                Assert.Equal(1, entityType.FindProperty("Top").GetPrecision());
                Assert.Equal(0, entityType.FindProperty("Top").GetScale());
                Assert.Equal(100, entityType.FindProperty("Bottom").GetPrecision());
                Assert.Equal(10, entityType.FindProperty("Bottom").GetScale());
            }

            [ConditionalFact]
            public virtual void Can_set_custom_value_generator_for_properties()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up).HasValueGenerator<CustomValueGenerator>();
                        b.Property(e => e.Down).HasValueGenerator(typeof(CustomValueGenerator));
                        b.Property<int>("Charm").HasValueGenerator((_, __) => new CustomValueGenerator());
                        b.Property<string>("Strange").HasValueGenerator<CustomValueGenerator>();
                        b.Property<int>("Top").HasValueGeneratorFactory(typeof(CustomValueGeneratorFactory));
                        b.Property<string>("Bottom").HasValueGeneratorFactory<CustomValueGeneratorFactory>();
                    });

                var model = modelBuilder.FinalizeModel();

                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Null(entityType.FindProperty(Customer.IdProperty.Name).GetValueGeneratorFactory());
                Assert.IsType<CustomValueGenerator>(entityType.FindProperty("Up").GetValueGeneratorFactory()(null, null));
                Assert.IsType<CustomValueGenerator>(entityType.FindProperty("Down").GetValueGeneratorFactory()(null, null));
                Assert.IsType<CustomValueGenerator>(entityType.FindProperty("Charm").GetValueGeneratorFactory()(null, null));
                Assert.IsType<CustomValueGenerator>(entityType.FindProperty("Strange").GetValueGeneratorFactory()(null, null));
                Assert.IsType<CustomValueGenerator>(entityType.FindProperty("Top").GetValueGeneratorFactory()(null, null));
                Assert.IsType<CustomValueGenerator>(entityType.FindProperty("Bottom").GetValueGeneratorFactory()(null, null));
            }

            private class CustomValueGenerator : ValueGenerator<int>
            {
                public override int Next(EntityEntry entry)
                {
                    throw new NotImplementedException();
                }

                public override bool GeneratesTemporaryValues
                    => false;
            }

            private class CustomValueGeneratorFactory : ValueGeneratorFactory
            {
                public override ValueGenerator Create(IProperty property, IEntityType entityType)
                    => new CustomValueGenerator();
            }

            [ConditionalFact]
            public virtual void Throws_for_bad_value_generator_type()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        Assert.Equal(
                            CoreStrings.BadValueGeneratorType(nameof(Random), nameof(ValueGenerator)),
                            Assert.Throws<ArgumentException>(() => b.Property(e => e.Down).HasValueGenerator(typeof(Random))).Message);
                    });
            }

            [ConditionalFact]
            public virtual void Throws_for_value_generator_that_cannot_be_constructed()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up).HasValueGenerator<BadCustomValueGenerator1>();
                        b.Property(e => e.Down).HasValueGenerator<BadCustomValueGenerator2>();
                    });

                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Equal(
                    CoreStrings.CannotCreateValueGenerator(nameof(BadCustomValueGenerator1), "HasValueGenerator"),
                    Assert.Throws<InvalidOperationException>(
                        () => entityType.FindProperty("Up").GetValueGeneratorFactory()(null, null)).Message);

                Assert.Equal(
                    CoreStrings.CannotCreateValueGenerator(nameof(BadCustomValueGenerator2), "HasValueGenerator"),
                    Assert.Throws<InvalidOperationException>(
                        () => entityType.FindProperty("Down").GetValueGeneratorFactory()(null, null)).Message);
            }

            private class BadCustomValueGenerator1 : CustomValueGenerator
            {
                public BadCustomValueGenerator1(string foo)
                {
                }
            }

            private abstract class BadCustomValueGenerator2 : CustomValueGenerator
            {
            }

            [ConditionalFact]
            public virtual void Throws_for_collection_of_string()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<StringCollectionEntity>();

                Assert.Equal(
                    CoreStrings.PropertyNotAdded(
                        nameof(StringCollectionEntity), nameof(StringCollectionEntity.Property), "ICollection<string>"),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder.FinalizeModel()).Message);
            }

            protected class StringCollectionEntity
            {
                public ICollection<string> Property { get; set; }
            }

            [ConditionalFact]
            public virtual void Object_cannot_be_configured_as_property()
            {
                Assert.Equal(CoreStrings.UnconfigurableType("object", "Property"),
                    Assert.Throws<InvalidOperationException>(() => CreateModelBuilder(c => c.Properties<object>())).Message);
            }

            [ConditionalFact]
            public virtual void Property_bag_cannot_be_configured_as_property()
            {
                Assert.Equal(CoreStrings.UnconfigurableType("Dictionary<string, object>", "Property"),
                    Assert.Throws<InvalidOperationException>(() => CreateModelBuilder(c => c.Properties<Dictionary<string, object>>())).Message);

                Assert.Equal(CoreStrings.UnconfigurableType("IDictionary<string, object>", "Property"),
                    Assert.Throws<InvalidOperationException>(() => CreateModelBuilder(c => c.Properties<IDictionary<string, object>>())).Message);
            }

            [ConditionalFact]
            protected virtual void Mapping_throws_for_non_ignored_array()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<OneDee>();

                Assert.Equal(
                    CoreStrings.PropertyNotAdded(
                        typeof(OneDee).ShortDisplayName(), "One", typeof(int[]).ShortDisplayName()),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder.FinalizeModel()).Message);
            }

            [ConditionalFact]
            protected virtual void Mapping_ignores_ignored_array()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<OneDee>().Ignore(e => e.One);

                var model = modelBuilder.FinalizeModel();

                Assert.Null(model.FindEntityType(typeof(OneDee)).FindProperty("One"));
            }

            [ConditionalFact]
            protected virtual void Mapping_throws_for_non_ignored_two_dimensional_array()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<TwoDee>();

                Assert.Equal(
                    CoreStrings.PropertyNotAdded(
                        typeof(TwoDee).ShortDisplayName(), "Two", typeof(int[,]).ShortDisplayName()),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder.FinalizeModel()).Message);
            }

            [ConditionalFact]
            protected virtual void Mapping_ignores_ignored_two_dimensional_array()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<TwoDee>().Ignore(e => e.Two);

                var model = modelBuilder.FinalizeModel();

                Assert.Null(model.FindEntityType(typeof(TwoDee)).FindProperty("Two"));
            }

            [ConditionalFact]
            protected virtual void Mapping_throws_for_non_ignored_three_dimensional_array()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<ThreeDee>();

                Assert.Equal(
                    CoreStrings.PropertyNotAdded(
                        typeof(ThreeDee).ShortDisplayName(), "Three", typeof(int[,,]).ShortDisplayName()),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder.FinalizeModel()).Message);
            }

            [ConditionalFact]
            protected virtual void Mapping_ignores_ignored_three_dimensional_array()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<ThreeDee>().Ignore(e => e.Three);

                var model = modelBuilder.FinalizeModel();

                Assert.Null(model.FindEntityType(typeof(ThreeDee)).FindProperty("Three"));
            }

            protected class OneDee
            {
                public int Id { get; set; }

                public int[] One { get; set; }
            }

            protected class TwoDee
            {
                public int Id { get; set; }

                public int[,] Two { get; set; }
            }

            protected class ThreeDee
            {
                public int Id { get; set; }

                public int[,,] Three { get; set; }
            }

            [ConditionalFact]
            public virtual void Can_set_unicode_for_properties()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up).IsUnicode();
                        b.Property(e => e.Down).IsUnicode(false);
                        b.Property<int>("Charm").IsUnicode();
                        b.Property<string>("Strange").IsUnicode(false);
                        b.Property<int>("Top").IsUnicode();
                        b.Property<string>("Bottom").IsUnicode(false);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.Null(entityType.FindProperty(Customer.IdProperty.Name).IsUnicode());
                Assert.True(entityType.FindProperty("Up").IsUnicode());
                Assert.False(entityType.FindProperty("Down").IsUnicode());
                Assert.True(entityType.FindProperty("Charm").IsUnicode());
                Assert.False(entityType.FindProperty("Strange").IsUnicode());
                Assert.True(entityType.FindProperty("Top").IsUnicode());
                Assert.False(entityType.FindProperty("Bottom").IsUnicode());
            }

            [ConditionalFact]
            public virtual void Can_set_unicode_for_property_type()
            {
                var modelBuilder = CreateModelBuilder(c =>
                {
                    c.Properties<int>().AreUnicode();
                    c.Properties<string>().AreUnicode(false);
                });

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property<int>("Charm");
                        b.Property<string>("Strange");
                        b.Property<int>("Top");
                        b.Property<string>("Bottom");
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Quarks));

                Assert.True(entityType.FindProperty(Customer.IdProperty.Name).IsUnicode());
                Assert.True(entityType.FindProperty("Up").IsUnicode());
                Assert.False(entityType.FindProperty("Down").IsUnicode());
                Assert.True(entityType.FindProperty("Charm").IsUnicode());
                Assert.False(entityType.FindProperty("Strange").IsUnicode());
                Assert.True(entityType.FindProperty("Top").IsUnicode());
                Assert.False(entityType.FindProperty("Bottom").IsUnicode());
            }

            [ConditionalFact]
            public virtual void PropertyBuilder_methods_can_be_chained()
            {
                CreateModelBuilder()
                    .Entity<Quarks>()
                    .Property(e => e.Up)
                    .IsRequired()
                    .HasAnnotation("A", "V")
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .ValueGeneratedOnAdd()
                    .ValueGeneratedOnAddOrUpdate()
                    .ValueGeneratedOnUpdate()
                    .IsUnicode()
                    .HasMaxLength(100)
                    .HasPrecision(10, 1)
                    .HasValueGenerator<CustomValueGenerator>()
                    .HasValueGenerator(typeof(CustomValueGenerator))
                    .HasValueGeneratorFactory<CustomValueGeneratorFactory>()
                    .HasValueGeneratorFactory(typeof(CustomValueGeneratorFactory))
                    .HasValueGenerator((_, __) => null)
                    .IsRequired();
            }

            [ConditionalFact]
            public virtual void Can_add_index()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Ignore<Product>();
                modelBuilder
                    .Entity<Customer>()
                    .HasIndex(ix => ix.Name);

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Customer));

                var index = entityType.GetIndexes().Single();
                Assert.Equal(Customer.NameProperty.Name, index.Properties.Single().Name);
            }

            [ConditionalFact]
            public virtual void Can_add_index_when_no_clr_property()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Ignore<Product>();
                modelBuilder
                    .Entity<Customer>(
                        b =>
                        {
                            b.Property<int>("Index");
                            b.HasIndex("Index");
                        });

                var model = modelBuilder.FinalizeModel();
                var entityType = model.FindEntityType(typeof(Customer));

                var index = entityType.GetIndexes().Single();
                Assert.Equal("Index", index.Properties.Single().Name);
            }

            [ConditionalFact]
            public virtual void Can_add_multiple_indexes()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Ignore<Product>();
                var entityBuilder = modelBuilder.Entity<Customer>();
                entityBuilder.HasIndex(ix => ix.Id).IsUnique();
                entityBuilder.HasIndex(ix => ix.Name).HasAnnotation("A1", "V1");
                entityBuilder.HasIndex(ix => ix.Id, "Named");

                var model = modelBuilder.FinalizeModel();

                var entityType = model.FindEntityType(typeof(Customer));
                var idProperty = entityType.FindProperty(nameof(Customer.Id));
                var nameProperty = entityType.FindProperty(nameof(Customer.Name));

                Assert.Equal(3, entityType.GetIndexes().Count());
                var firstIndex = entityType.FindIndex(idProperty);
                Assert.True(firstIndex.IsUnique);
                var secondIndex = entityType.FindIndex(nameProperty);
                Assert.False(secondIndex.IsUnique);
                Assert.Equal("V1", secondIndex["A1"]);
                var namedIndex = entityType.FindIndex("Named");
                Assert.False(namedIndex.IsUnique);
            }

            [ConditionalFact]
            public virtual void Can_add_contained_indexes()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Ignore<Product>();
                var entityBuilder = modelBuilder.Entity<Customer>();
                var firstIndexBuilder = entityBuilder.HasIndex(
                    ix => new { ix.Id, ix.AlternateKey }).IsUnique();
                var secondIndexBuilder = entityBuilder.HasIndex(
                    ix => new { ix.Id });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Customer));

                Assert.Equal(2, entityType.GetIndexes().Count());
                Assert.True(firstIndexBuilder.Metadata.IsUnique);
                Assert.False(secondIndexBuilder.Metadata.IsUnique);
            }

            [ConditionalFact]
            public virtual void Can_set_primary_key_by_convention_for_user_specified_shadow_property()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;

                var entityBuilder = modelBuilder.Entity<EntityWithoutId>();

                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(EntityWithoutId));

                Assert.Null(entityType.FindPrimaryKey());

                entityBuilder.Property<int>("Id");

                Assert.NotNull(entityType.FindPrimaryKey());
                AssertEqual(new[] { "Id" }, entityType.FindPrimaryKey().Properties.Select(p => p.Name));
            }

            [ConditionalFact]
            public virtual void Can_ignore_explicit_interface_implementation_property()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<EntityBase>().HasNoKey().Ignore(e => ((IEntityBase)e).Target);

                Assert.DoesNotContain(
                    nameof(IEntityBase.Target),
                    modelBuilder.Model.FindEntityType(typeof(EntityBase)).GetProperties().Select(p => p.Name));

                modelBuilder.Entity<EntityBase>().Property(e => ((IEntityBase)e).Target);

                Assert.Contains(
                    nameof(IEntityBase.Target),
                    modelBuilder.Model.FindEntityType(typeof(EntityBase)).GetProperties().Select(p => p.Name));
            }

            [ConditionalFact]
            public virtual void Can_set_key_on_an_entity_with_fields()
            {
                var modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder();

                modelBuilder.Entity<EntityWithFields>().HasKey(e => e.Id);

                var model = modelBuilder.FinalizeModel();
                var entity = model.FindEntityType(typeof(EntityWithFields));
                var primaryKey = entity.FindPrimaryKey();
                Assert.NotNull(primaryKey);
                var property = Assert.Single(primaryKey.Properties);
                Assert.Equal(nameof(EntityWithFields.Id), property.Name);
                Assert.Null(property.PropertyInfo);
                Assert.NotNull(property.FieldInfo);
            }

            [ConditionalFact]
            public virtual void Can_set_composite_key_on_an_entity_with_fields()
            {
                var modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder();

                modelBuilder.Entity<EntityWithFields>().HasKey(e => new { e.TenantId, e.CompanyId });

                var model = modelBuilder.FinalizeModel();
                var entity = model.FindEntityType(typeof(EntityWithFields));
                var primaryKeyProperties = entity.FindPrimaryKey().Properties;
                Assert.Equal(2, primaryKeyProperties.Count);
                var first = primaryKeyProperties[0];
                var second = primaryKeyProperties[1];
                Assert.Equal(nameof(EntityWithFields.TenantId), first.Name);
                Assert.Null(first.PropertyInfo);
                Assert.NotNull(first.FieldInfo);
                Assert.Equal(nameof(EntityWithFields.CompanyId), second.Name);
                Assert.Null(second.PropertyInfo);
                Assert.NotNull(second.FieldInfo);
            }

            [ConditionalFact]
            public virtual void Can_set_alternate_key_on_an_entity_with_fields()
            {
                var modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder();

                modelBuilder.Entity<EntityWithFields>().HasAlternateKey(e => e.CompanyId);

                var entity = modelBuilder.Model.FindEntityType(typeof(EntityWithFields));
                var properties = entity.GetProperties();
                Assert.Single(properties);
                var property = properties.Single();
                Assert.Equal(nameof(EntityWithFields.CompanyId), property.Name);
                Assert.Null(property.PropertyInfo);
                Assert.NotNull(property.FieldInfo);
                var keys = entity.GetKeys();
                var key = Assert.Single(keys);
                Assert.Equal(properties, key.Properties);
            }

            [ConditionalFact]
            public virtual void Can_set_composite_alternate_key_on_an_entity_with_fields()
            {
                var modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder();

                modelBuilder.Entity<EntityWithFields>().HasAlternateKey(e => new { e.TenantId, e.CompanyId });

                var keys = modelBuilder.Model.FindEntityType(typeof(EntityWithFields)).GetKeys();
                Assert.Single(keys);
                var properties = keys.Single().Properties;
                Assert.Equal(2, properties.Count);
                var first = properties[0];
                var second = properties[1];
                Assert.Equal(nameof(EntityWithFields.TenantId), first.Name);
                Assert.Null(first.PropertyInfo);
                Assert.NotNull(first.FieldInfo);
                Assert.Equal(nameof(EntityWithFields.CompanyId), second.Name);
                Assert.Null(second.PropertyInfo);
                Assert.NotNull(second.FieldInfo);
            }

            [ConditionalFact]
            public virtual void Can_call_Property_on_an_entity_with_fields()
            {
                var modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder();

                modelBuilder.Entity<EntityWithFields>().Property(e => e.Id);

                var model = modelBuilder.FinalizeModel();
                var properties = model.FindEntityType(typeof(EntityWithFields)).GetProperties();
                var property = Assert.Single(properties);
                Assert.Equal(nameof(EntityWithFields.Id), property.Name);
                Assert.Null(property.PropertyInfo);
                Assert.NotNull(property.FieldInfo);
            }

            [ConditionalFact]
            public virtual void Can_set_index_on_an_entity_with_fields()
            {
                var modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder();

                modelBuilder.Entity<EntityWithFields>().HasNoKey().HasIndex(e => e.CompanyId);

                var model = modelBuilder.FinalizeModel();
                var indexes = model.FindEntityType(typeof(EntityWithFields)).GetIndexes();
                var index = Assert.Single(indexes);
                var property = Assert.Single(index.Properties);
                Assert.Null(property.PropertyInfo);
                Assert.NotNull(property.FieldInfo);
            }

            [ConditionalFact]
            public virtual void Can_set_composite_index_on_an_entity_with_fields()
            {
                var modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder();

                modelBuilder.Entity<EntityWithFields>().HasNoKey().HasIndex(e => new { e.TenantId, e.CompanyId });

                var model = modelBuilder.FinalizeModel();
                var indexes = model.FindEntityType(typeof(EntityWithFields)).GetIndexes();
                var index = Assert.Single(indexes);
                Assert.Equal(2, index.Properties.Count);
                var properties = index.Properties;
                var first = properties[0];
                var second = properties[1];
                Assert.Equal(nameof(EntityWithFields.TenantId), first.Name);
                Assert.Null(first.PropertyInfo);
                Assert.NotNull(first.FieldInfo);
                Assert.Equal(nameof(EntityWithFields.CompanyId), second.Name);
                Assert.Null(second.PropertyInfo);
                Assert.NotNull(second.FieldInfo);
            }

            [ConditionalFact]
            public virtual void Can_ignore_a_field_on_an_entity_with_fields()
            {
                var modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder();

                modelBuilder.Entity<EntityWithFields>()
                    .Ignore(e => e.CompanyId)
                    .HasKey(e => e.Id);

                var model = modelBuilder.FinalizeModel();
                var entity = model.FindEntityType(typeof(EntityWithFields));
                var property = Assert.Single(entity.GetProperties());
                Assert.Equal(nameof(EntityWithFields.Id), property.Name);
            }

            [ConditionalFact]
            public virtual void Can_ignore_a_field_on_a_keyless_entity_with_fields()
            {
                var modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder();

                modelBuilder.Entity<KeylessEntityWithFields>()
                    .HasNoKey()
                    .Ignore(e => e.FirstName)
                    .Property(e => e.LastName);

                var model = modelBuilder.FinalizeModel();
                var entity = model.FindEntityType(typeof(KeylessEntityWithFields));
                var property = Assert.Single(entity.GetProperties());
                Assert.Equal(nameof(KeylessEntityWithFields.LastName), property.Name);
            }

            [ConditionalFact]
            public virtual void Can_add_seed_data_objects()
            {
                var modelBuilder = CreateModelBuilder();
                var model = modelBuilder.Model;
                modelBuilder.Ignore<Theta>();
                modelBuilder.Entity<Beta>(
                    c =>
                    {
                        c.HasData(
                            new Beta { Id = -1 });
                        var customers = new List<Beta> { new() { Id = -2 } };
                        c.HasData(customers);
                    });

                var finalModel = modelBuilder.FinalizeModel();

                var customer = finalModel.FindEntityType(typeof(Beta));
                var data = customer.GetSeedData();
                Assert.Equal(2, data.Count());
                Assert.Equal(-1, data.First()[nameof(Beta.Id)]);
                Assert.Equal(-2, data.Last()[nameof(Beta.Id)]);

                var _ = finalModel.ToDebugString();
            }

            [ConditionalFact]
            public virtual void Can_add_seed_data_anonymous_objects()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Ignore<Theta>();
                modelBuilder.Entity<Beta>(
                    c =>
                    {
                        c.HasData(
                            new { Id = -1 });
                        var customers = new List<object> { new { Id = -2 } };
                        c.HasData(customers);
                    });

                var model = modelBuilder.FinalizeModel();

                var customer = model.FindEntityType(typeof(Beta));
                var data = customer.GetSeedData();
                Assert.Equal(2, data.Count());
                Assert.Equal(-1, data.First().Values.Single());
                Assert.Equal(-2, data.Last().Values.Single());
            }

            [ConditionalFact]
            public virtual void Private_property_is_not_discovered_by_convention()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Ignore<Alpha>();
                modelBuilder.Entity<Gamma>();

                var model = modelBuilder.FinalizeModel();

                Assert.Empty(
                    model.FindEntityType(typeof(Gamma)).GetProperties()
                        .Where(p => p.Name == "PrivateProperty"));
            }

            [ConditionalFact]
            public virtual void Can_add_seed_data_objects_indexed_property()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<IndexedClass>(
                    b =>
                    {
                        b.IndexerProperty<int>("Required");
                        b.IndexerProperty<string>("Optional");
                        var d = new IndexedClass { Id = -1 };
                        d["Required"] = 2;
                        b.HasData(d);
                    });

                var model = modelBuilder.FinalizeModel();

                var entityType = model.FindEntityType(typeof(IndexedClass));
                var data = Assert.Single(entityType.GetSeedData());
                Assert.Equal(-1, data["Id"]);
                Assert.Equal(2, data["Required"]);
                Assert.Null(data["Optional"]);
            }

            [ConditionalFact]
            public virtual void Can_add_seed_data_anonymous_objects_indexed_property()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<IndexedClass>(
                    b =>
                    {
                        b.IndexerProperty<int>("Required");
                        b.IndexerProperty<string>("Optional");
                        b.HasData(new { Id = -1, Required = 2 });
                    });

                var model = modelBuilder.FinalizeModel();

                var entityType = model.FindEntityType(typeof(IndexedClass));
                var data = Assert.Single(entityType.GetSeedData());
                Assert.Equal(-1, data["Id"]);
                Assert.Equal(2, data["Required"]);
                Assert.False(data.ContainsKey("Optional"));
            }

            [ConditionalFact]
            public virtual void Can_add_seed_data_objects_indexed_property_dictionary()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<IndexedClassByDictionary>(
                    b =>
                    {
                        b.IndexerProperty<int>("Required");
                        b.IndexerProperty<string>("Optional");
                        var d = new IndexedClassByDictionary { Id = -1 };
                        d["Required"] = 2;
                        b.HasData(d);
                    });

                var model = modelBuilder.FinalizeModel();

                var entityType = model.FindEntityType(typeof(IndexedClassByDictionary));
                var data = Assert.Single(entityType.GetSeedData());
                Assert.Equal(-1, data["Id"]);
                Assert.Equal(2, data["Required"]);
                Assert.Null(data["Optional"]);
            }

            [ConditionalFact]
            public virtual void Can_add_seed_data_anonymous_objects_indexed_property_dictionary()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<IndexedClassByDictionary>(
                    b =>
                    {
                        b.IndexerProperty<int>("Required");
                        b.IndexerProperty<string>("Optional");
                        b.HasData(new { Id = -1, Required = 2 });
                    });

                var model = modelBuilder.FinalizeModel();

                var entityType = model.FindEntityType(typeof(IndexedClassByDictionary));
                var data = Assert.Single(entityType.GetSeedData());
                Assert.Equal(-1, data["Id"]);
                Assert.Equal(2, data["Required"]);
                Assert.False(data.ContainsKey("Optional"));
            }

            [ConditionalFact] //Issue#12617
            [UseCulture("de-DE")]
            public virtual void EntityType_name_is_stored_culture_invariantly()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Entityß>();
                modelBuilder.Entity<Entityss>();

                var model = modelBuilder.FinalizeModel();

                Assert.Equal(2, model.GetEntityTypes().Count());
                Assert.Equal(2, model.FindEntityType(typeof(Entityss)).GetNavigations().Count());
            }

            protected class Entityß
            {
                public int Id { get; set; }
            }

            protected class Entityss
            {
                public int Id { get; set; }
                public Entityß Navigationß { get; set; }
                public Entityß Navigationss { get; set; }
            }

            [ConditionalFact]
            public virtual void Can_add_shared_type_entity_type()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.SharedTypeEntity<Dictionary<string, object>>("Shared1", b =>
                {
                    b.IndexerProperty<int>("Key");
                    b.HasKey("Key");
                });

                modelBuilder.SharedTypeEntity<Dictionary<string, object>>("Shared2", b => b.IndexerProperty<int>("Id"));

                Assert.Equal(
                    CoreStrings.ClashingSharedType(typeof(Dictionary<string, object>).ShortDisplayName()),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder.Entity<Dictionary<string, object>>()).Message);

                var model = modelBuilder.FinalizeModel();
                Assert.Equal(2, model.GetEntityTypes().Count());

                var shared1 = model.FindEntityType("Shared1");
                Assert.NotNull(shared1);
                Assert.True(shared1.HasSharedClrType);
                Assert.Null(shared1.FindProperty("Id"));

                var shared2 = model.FindEntityType("Shared2");
                Assert.NotNull(shared2);
                Assert.True(shared2.HasSharedClrType);
                Assert.NotNull(shared2.FindProperty("Id"));

                var indexer = shared1.FindIndexerPropertyInfo();
                Assert.True(model.IsIndexerMethod(indexer.GetMethod));
                Assert.True(model.IsIndexerMethod(indexer.SetMethod));
                Assert.Same(indexer, shared2.FindIndexerPropertyInfo());
            }

            [ConditionalFact]
            public virtual void Cannot_add_shared_type_when_non_shared_exists()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>();

                Assert.Equal(
                    CoreStrings.ClashingNonSharedType("Shared1", nameof(Customer)),
                    Assert.Throws<InvalidOperationException>(() => modelBuilder.SharedTypeEntity<Customer>("Shared1")).Message);
            }
        }
    }
}
