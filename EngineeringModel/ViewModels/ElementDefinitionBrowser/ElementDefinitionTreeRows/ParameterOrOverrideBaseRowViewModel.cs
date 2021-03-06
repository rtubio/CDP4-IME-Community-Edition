﻿// -------------------------------------------------------------------------------------------------
// <copyright file="ParameterOrOverrideBaseRowViewModel.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace CDP4EngineeringModel.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.Helpers;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;
    using CDP4Composition.Mvvm;
    using CDP4Dal;
    using CDP4Dal.Events;
    using ReactiveUI;

    /// <summary>
    /// The row representing a <see cref="ParameterOrOverrideBase"/>
    /// </summary>
    public abstract class ParameterOrOverrideBaseRowViewModel : ParameterBaseRowViewModel<ParameterOrOverrideBase>
    {
        #region Fields
        /// <summary>
        /// The active participant.
        /// </summary>
        protected Participant activeParticipant;

        /// <summary>
        /// Backing field for <see cref="IsPublishable"/>
        /// </summary>
        private bool isPublishable;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterOrOverrideBaseRowViewModel"/> class
        /// </summary>
        /// <param name="parameterOrOverrideBase">
        /// The associated <see cref="ParameterOrOverrideBase"/>
        /// </param>
        /// <param name="session">
        /// The associated <see cref="ISession"/>
        /// </param>
        /// <param name="containerViewModel">
        /// The container Row.
        /// </param>
        /// <param name="isReadOnly">
        /// A value indicating whether this row shall be made read-only in the current context.
        /// </param>
        protected ParameterOrOverrideBaseRowViewModel(ParameterOrOverrideBase parameterOrOverrideBase, ISession session, IViewModelBase<Thing> containerViewModel, bool isReadOnly)
            : base(parameterOrOverrideBase, session, containerViewModel, isReadOnly)
        {
            var engineeringModel = (EngineeringModel)this.Thing.TopContainer;
            this.activeParticipant = engineeringModel.GetActiveParticipant(this.Session.ActivePerson);
            this.SetOwnerValue();
        }
        #endregion

        /// <summary>
        /// Gets a value indicating whether this <see cref="ParameterOrOverrideBase"/> has publishable values
        /// </summary>
        public bool IsPublishable
        {
            get { return this.isPublishable; }
            private set { this.RaiseAndSetIfChanged(ref this.isPublishable, value); }
        }

        #region Base Override

        /// <summary>
        /// Sets the values of this row in case where the <see cref="ParameterOrOverrideBase"/> is neither option-dependent nor state-dependent and is a <see cref="ScalarParameterType"/>
        /// </summary>
        public override void SetProperties()
        {
            var valueset = this.GetValueSet();
            if (valueset == null)
            {
                this.LogNoValueSetError();
                return;
            }

            this.SetProperties(valueset);
            this.CheckPublishabledStatus();

            if (this.valueSetListener.Any())
            {
                return;
            }

            var listener = CDPMessageBus.Current.Listen<ObjectChangedEvent>(valueset)
                            .Where(objectChange => objectChange.EventKind == EventKind.Updated && objectChange.ChangedThing.RevisionNumber > this.RevisionNumber)
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Subscribe(_ => this.SetProperties());
            this.valueSetListener.Add(listener);
        }

        /// <summary>
        /// Creates a clone of the edited <see cref="ParameterValueSetBase"/> and writes it to the data-source.
        /// </summary>
        /// <param name="newValue">The new value</param>
        /// <param name="fieldName">The property name</param>
        public override void CreateCloneAndWrite(object newValue, string fieldName)
        {
            var valueset = this.GetValueSet();
            if (valueset == null)
            {
                return;
            }

            var clone = valueset.Clone(false);
            this.UpdateValueSets(clone);
            this.EndInlineEdit(clone);
        }

        /// <summary>
        /// The object changed event handler
        /// </summary>
        /// <param name="objectChange">The <see cref="ObjectChangedEvent"/></param>
        protected override void ObjectChangeEventHandler(ObjectChangedEvent objectChange)
        {
            base.ObjectChangeEventHandler(objectChange);
            this.UpdateProperties();
        }

        /// <summary>
        /// Create subscription to listen to updates of the value sets
        /// </summary>
        /// <remarks>
        /// These subscriptions are necessary to update the publishable status of this row
        /// </remarks>
        protected override void CreateValueSetsSubscription()
        {
            foreach (var valueSet in this.Thing.ValueSets)
            {
                var listener = CDPMessageBus.Current.Listen<ObjectChangedEvent>(valueSet)
                                   .Where(objectChange => objectChange.EventKind == EventKind.Updated)
                                   .ObserveOn(RxApp.MainThreadScheduler)
                                   .Subscribe(x => this.CheckPublishabledStatus());
                this.valueSetListener.Add(listener);
            }
        }

        /// <summary>
        /// Set the listener for the owner
        /// </summary>
        protected override void SetOwnerListener()
        {
            var listener = CDPMessageBus.Current.Listen<ObjectChangedEvent>(this.Thing.Owner)
                                   .Where(objectChange => objectChange.EventKind == EventKind.Updated)
                                   .ObserveOn(RxApp.MainThreadScheduler)
                                   .Subscribe(x => { this.OwnerName = this.Thing.Owner.Name; this.OwnerShortName = this.Thing.Owner.ShortName; });

            this.OwnerListener = new KeyValuePair<DomainOfExpertise, IDisposable>(this.Thing.Owner, listener);
        }

        #endregion

        /// <summary>
        /// Update the properties of this row
        /// </summary>
        /// <remarks>
        /// Refreshes the container view-model of this row, i.e. <see cref="ElementDefinitionRowViewModel"/> or <see cref="ElementUsageRowViewModel"/> to re-draw its rows
        /// </remarks>
        private void UpdateProperties()
        {
            this.SetOwnerValue();

            // refresh the container row if this is replaced by a subscription
            Tuple<DomainOfExpertise, Participant> tuple;
            this.Session.OpenIterations.TryGetValue(this.Thing.GetContainerOfType<Iteration>(), out tuple);
            if (this.Thing.ParameterSubscription.Any(x => x.Owner == tuple.Item1))
            {
                this.RefreshContainerRows();
            }
        }

        /// <summary>
        /// Set the owner value
        /// </summary>
        private void SetOwnerValue()
        {
            if (this.Owner != null)
            {
                this.OwnerName = this.Owner.Name;
                this.OwnerShortName = this.Owner.ShortName;
            }

            if (this.OwnerListener.Key != this.Thing.Owner)
            {
                this.OwnerListener.Value.Dispose();
                this.SetOwnerListener();
            }
        }

        /// <summary>
        /// Refresh the rows contained by the container of this row.
        /// </summary>
        private void RefreshContainerRows()
        {
            var containerUsageRow = this.ContainerViewModel as ElementUsageRowViewModel;
            if (containerUsageRow == null)
            {
                var elementDefinitionRow = this.ContainerViewModel as ElementDefinitionRowViewModel;
                if (elementDefinitionRow != null)
                {
                    elementDefinitionRow.UpdateChildren();
                }

                return;
            }

            containerUsageRow.UpdateChildren();
        }

        /// <summary>
        /// Gets the single <see cref="ParameterValueSetBase"/> (not dependent, not state dependent)
        /// </summary>
        /// <returns>The <see cref="ParameterValueSetBase"/></returns>
        private ParameterValueSetBase GetValueSet()
        {
            return (ParameterValueSetBase)this.Thing.ValueSets.FirstOrDefault();
        }


        /// <summary>
        /// Sets the properties of this <see cref="ParameterOrOverrideBaseRowViewModel"/> from a <see cref="ParameterValueSetBase"/>.
        /// </summary>
        /// <param name="valueSet">
        /// The <see cref="ParameterValueSetBase"/>.
        /// </param>
        private void SetProperties(ParameterValueSetBase valueSet)
        {
            this.Value = valueSet.ActualValue.Any() ? valueSet.ActualValue.First() : "-";
            if (this.ContainedRows.Count == 0)
            {
                this.ScaleShortName = this.Thing.Scale == null ? "-" : this.Thing.Scale.ShortName;
            }
            this.Switch = valueSet.ValueSwitch;
            this.Formula = valueSet.Formula.Any() ? valueSet.Formula.First() : "-";
            this.Computed = valueSet.Computed.Any() ? valueSet.Computed.First() : "-";
            this.Manual = valueSet.Manual.Any()
                              ? valueSet.Manual.First().ToValueSetObject(this.ParameterType)
                              : ValueSetConverter.DefaultObject(this.ParameterType);
            this.Reference = valueSet.Reference.Any()
                                 ? valueSet.Reference.First().ToValueSetObject(this.ParameterType)
                                 : ValueSetConverter.DefaultObject(this.ParameterType);
            this.State = valueSet.ActualState == null ? "-" : valueSet.ActualState.ShortName;
            this.Option = valueSet.ActualOption;
            this.Published = valueSet.Published.Any() ? valueSet.Published.First() : "-";
        }

        /// <summary>
        /// Check if the current <see cref="ParameterValueSetBase"/> is publishable (the published and actual values are different)
        /// </summary>
        private void CheckPublishabledStatus()
        {
            foreach (ParameterValueSetBase valueset in this.Thing.ValueSets)
            {
                for (var i = 0; i < valueset.Published.Count(); i++)
                {
                    if (valueset.Published[i] != valueset.ActualValue[i])
                    {
                        this.IsPublishable = true;
                        return;
                    }
                }
            }

            this.IsPublishable = false;
        }

        /// <summary>
        /// The logs the error if the <see cref="Thing"/> is a <see cref="Parameter"/> or a <see cref="ParameterOverride"/> without value set.
        /// </summary>
        private void LogNoValueSetError()
        {
            var elementBase = (ElementBase)this.Thing.Container;
            logger.Error(
                "No value set found for the {0} {1} with id: {2} contained in the {3} {4}",
                this.Thing.ClassKind,
                this.Thing.ParameterType.Name,
                this.Thing.Iid,
                elementBase.ClassKind,
                elementBase.Name);
        }

        #region Update Value Set Methods
        /// <summary>
        /// The update component values.
        /// </summary>
        /// <param name="valueSet">
        /// The value set.
        /// </param>
        public void UpdateValueSets(ParameterValueSetBase valueSet)
        {
            var actualOption = valueSet.ActualOption;
            var actualState = valueSet.ActualState;

            if (actualOption != null)
            {
                var optionRow = this.ContainedRows.Cast<ParameterOptionRowViewModel>().Single(x => x.ActualOption == actualOption);
                if (actualState != null)
                {
                    var actualStateRow = optionRow.ContainedRows.Cast<ParameterStateRowViewModel>().Single(x => x.ActualState == actualState);
                    this.UpdateScalarOrCompoundValueSet(valueSet, actualStateRow);
                }
                else
                {
                    this.UpdateScalarOrCompoundValueSet(valueSet, optionRow);
                }
            }
            else
            {
                if (actualState != null)
                {
                    var actualStateRow = this.ContainedRows.Cast<ParameterStateRowViewModel>().Single(x => x.ActualState == actualState);
                    this.UpdateScalarOrCompoundValueSet(valueSet, actualStateRow);
                }
                else
                {
                    this.UpdateScalarOrCompoundValueSet(valueSet);
                }
            }
        }

        /// <summary>
        /// Call the correct update method depending on kind of parameter type (scalar, compound)
        /// </summary>
        /// <param name="valueSet">The <see cref="ParameterValueSetBase"/> to update</param>
        /// <param name="row">The <see cref="ParameterValueBaseRowViewModel"/> containing the information, or if null the current row</param>
        private void UpdateScalarOrCompoundValueSet(ParameterValueSetBase valueSet, ParameterValueBaseRowViewModel row = null)
        {
            if (this.IsCompoundType)
            {
                this.UpdateCompoundValueSet(valueSet, row);
            }
            else
            {
                this.UpdateScalarValueSet(valueSet, row);
            }
        }

        /// <summary>
        /// Update value-set for a scalar parameter.
        /// </summary>
        /// <param name="valueSet">The value set to update</param>
        /// <param name="row">The value row containing the information. If null the data is retrieved from the current row.</param>
        /// <remarks>
        /// If <paramref name="row"/> is null, it means the parameter is not compound, not option dependent and not state dependent.
        /// </remarks>
        private void UpdateScalarValueSet(ParameterValueSetBase valueSet, ParameterValueBaseRowViewModel row = null)
        {
            valueSet.ValueSwitch = row == null ? this.Switch.Value : row.Switch.Value;
            valueSet.Computed = row == null ? new ValueArray<string>(new List<string> { this.Computed }) : new ValueArray<string>(new List<string> { row.Computed });
            valueSet.Manual = row == null ? new ValueArray<string>(new List<string> { this.Manual.ToValueSetString(this.ParameterType) }) : new ValueArray<string>(new List<string> { row.Manual.ToValueSetString(this.ParameterType) });
            valueSet.Reference = row == null ? new ValueArray<string>(new List<string> { this.Reference.ToValueSetString(this.ParameterType) }) : new ValueArray<string>(new List<string> { ValueSetConverter.ToValueSetString(row.Reference, this.ParameterType) });
        }

        /// <summary>
        /// Update value-set for a compound parameter.
        /// </summary>
        /// <param name="valueSet">The value set to update</param>
        /// <param name="row">The value row containing the information. If null the data is retrieved from the current row.</param>
        /// <remarks>
        /// If <paramref name="row"/> is null, it means the parameter is not compound, not option dependent and not state dependent.
        /// </remarks>
        private void UpdateCompoundValueSet(ParameterValueSetBase valueSet, ParameterValueBaseRowViewModel row = null)
        {
            var componentRows = (row == null)
                ? this.ContainedRows.Cast<ParameterComponentValueRowViewModel>().ToList()
                : row.ContainedRows.Cast<ParameterComponentValueRowViewModel>().ToList();

            valueSet.Computed = new ValueArray<string>(new string[componentRows.Count]);
            valueSet.Manual = new ValueArray<string>(new string[componentRows.Count]);
            valueSet.Reference = new ValueArray<string>(new string[componentRows.Count]);
            valueSet.ValueSwitch = componentRows[0].Switch.Value; // all the switches should have the same value
            for (var i = 0; i < componentRows.Count; i++)
            {
                valueSet.Computed[i] = componentRows[i].Computed;
                valueSet.Manual[i] = componentRows[i].Manual.ToValueSetString(this.ParameterType);
                valueSet.Reference[i] = componentRows[i].Reference.ToValueSetString(this.ParameterType);
            }
        }

        #endregion
    }
}