﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MeasurementUnitsBrowserViewModel.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace BasicRdl.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using CDP4Common.CommonData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Composition;
    using CDP4Composition.Mvvm;
    using CDP4Composition.Navigation;
    using CDP4Composition.Navigation.Interfaces;
    using CDP4Composition.PluginSettingService;
    using CDP4Dal;
    using CDP4Dal.Events;
    using ReactiveUI;

    /// <summary>
    /// The purpose of the <see cref="MeasurementUnitsBrowserViewModel"/> is to represent the view-model for <see cref="MeasurementUnit"/>s
    /// </summary>
    public class MeasurementUnitsBrowserViewModel : BrowserViewModelBase<SiteDirectory>, IPanelViewModel
    {
        /// <summary>
        /// The Panel Caption
        /// </summary>
        private const string PanelCaption = "Measurement Units";

        /// <summary>
        /// Backing field for the <see cref="MeasurementUnits"/> property
        /// </summary>
        private readonly ReactiveList<MeasurementUnitRowViewModel> measurementUnits = new ReactiveList<MeasurementUnitRowViewModel>();

        /// <summary>
        /// Backing field for <see cref="CanCreateRdlElement"/>
        /// </summary>
        private bool canCreateRdlElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasurementUnitsBrowserViewModel"/> class.
        /// </summary>
        /// <param name="session">the associated session</param>
        /// <param name="siteDir">The unique <see cref="SiteDirectory"/></param>
        /// <param name="thingDialogNavigationService">The <see cref="IThingDialogNavigationService"/> that is used to navigate to a dialog of a specific <see cref="Thing"/></param>
        /// <param name="panelNavigationService">The <see cref="IPanelNavigationService"/> that is used to navigate to a panel</param>
        /// <param name="dialogNavigationService">The <see cref="IDialogNavigationService"/> that is used to navigate to a panel</param>
        /// <param name="pluginSettingsService">
        /// The <see cref="IPluginSettingsService"/> used to read and write plugin setting files.
        /// </param>
        public MeasurementUnitsBrowserViewModel(ISession session, SiteDirectory siteDir, IThingDialogNavigationService thingDialogNavigationService, IPanelNavigationService panelNavigationService, IDialogNavigationService dialogNavigationService, IPluginSettingsService pluginSettingsService)
            : base(siteDir, session, thingDialogNavigationService, panelNavigationService, dialogNavigationService, pluginSettingsService)
        {
            this.Caption = string.Format("{0}, {1}", PanelCaption, this.Thing.Name);
            this.ToolTip = string.Format("{0}\n{1}\n{2}", this.Thing.Name, this.Thing.IDalUri, this.Session.ActivePerson.Name);

            this.measurementUnits.ChangeTrackingEnabled = true;

            this.AddSubscriptions();
        }
        
        /// <summary>
        /// Gets the <see cref="MeasurementUnitRowViewModel"/> that are contained by this view-model
        /// </summary>
        public ReactiveList<MeasurementUnitRowViewModel> MeasurementUnits 
        {
            get
            {
                return this.measurementUnits;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a RDL element may be created
        /// </summary>
        public bool CanCreateRdlElement
        {
            get { return this.canCreateRdlElement; }
            private set { this.RaiseAndSetIfChanged(ref this.canCreateRdlElement, value); }
        }

        /// <summary>
        /// Gets the <see cref="ReactiveCommand"/> used to create a <see cref="SimpleUnit"/>
        /// </summary>
        public ReactiveCommand<object> CreateSimpleUnit { get; private set; }

        /// <summary>
        /// Gets the <see cref="ReactiveCommand"/> used to create a <see cref="DerivedUnit"/>
        /// </summary>
        public ReactiveCommand<object> CreateDerivedUnit { get; private set; }

        /// <summary>
        /// Gets the <see cref="ReactiveCommand"/> used to create a <see cref="LinearConversionUnit"/>
        /// </summary>
        public ReactiveCommand<object> CreateLinearConversionUnit { get; private set; }

        /// <summary>
        /// Gets the <see cref="ReactiveCommand"/> used to create a <see cref="PrefixedUnit"/>
        /// </summary>
        public ReactiveCommand<object> CreatePrefixedUnit { get; private set; }
        
        /// <summary>
        /// Add the necessary subscriptions for this view model.
        /// </summary>
        private void AddSubscriptions()
        {
            var addListener =
                CDPMessageBus.Current.Listen<ObjectChangedEvent>(typeof(MeasurementUnit))
                    .Where(objectChange => objectChange.EventKind == EventKind.Added && objectChange.ChangedThing.Cache == this.Session.Assembler.Cache)
                    .Select(x => x.ChangedThing as MeasurementUnit)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(this.AddMeasurementUnitRowViewModel);
            this.Disposables.Add(addListener);

            var removeListener =
                CDPMessageBus.Current.Listen<ObjectChangedEvent>(typeof(MeasurementUnit))
                    .Where(objectChange => objectChange.EventKind == EventKind.Removed && objectChange.ChangedThing.Cache == this.Session.Assembler.Cache)
                    .Select(x => x.ChangedThing as MeasurementUnit)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(this.RemoveMeasurementUnitRowViewModel);
            this.Disposables.Add(removeListener);

            var rdlUpdateListener =
                CDPMessageBus.Current.Listen<ObjectChangedEvent>(typeof(ReferenceDataLibrary))
                    .Where(objectChange => objectChange.EventKind == EventKind.Updated && objectChange.ChangedThing.Cache == this.Session.Assembler.Cache)
                    .Select(x => x.ChangedThing as ReferenceDataLibrary)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(this.RefreshContainerName);
            this.Disposables.Add(rdlUpdateListener);
        }

        /// <summary>
        /// Adds a <see cref="MeasurementUnitRowViewModel"/>
        /// </summary>
        /// <param name="measurementUnit">The associated <see cref="MeasurementUnit"/></param>
        private void AddMeasurementUnitRowViewModel(MeasurementUnit measurementUnit)
        {
            var row = new MeasurementUnitRowViewModel(measurementUnit, this.Session, this);
            this.MeasurementUnits.Add(row);
        }

        /// <summary>
        /// Removes a <see cref="MeasurementUnitRowViewModel"/> from the view model
        /// </summary>
        /// <param name="measurementUnit">
        /// The <see cref="MeasurementUnit"/> for which the row view model has to be removed
        /// </param>
        private void RemoveMeasurementUnitRowViewModel(MeasurementUnit measurementUnit)
        {
            var row = this.MeasurementUnits.SingleOrDefault(rowViewModel => rowViewModel.Thing == measurementUnit);
            if (row != null)
            {
                this.MeasurementUnits.Remove(row);
                row.Dispose();
            }
        }

        /// <summary>
        /// Refresh the displayed container name for the category rows
        /// </summary>
        /// <param name="rdl">
        /// The updated <see cref="ReferenceDataLibrary"/>.
        /// </param>
        private void RefreshContainerName(ReferenceDataLibrary rdl)
        {
            foreach (var measurementUnit in this.measurementUnits)
            {
                if (measurementUnit.Thing.Container != rdl)
                {
                    continue;
                }

                if (measurementUnit.ContainerRdl != rdl.ShortName)
                {
                    measurementUnit.ContainerRdl = rdl.ShortName;
                }
            }
        }

        /// <summary>
        /// Compute the permissions
        /// </summary>
        public override void ComputePermission()
        {
            base.ComputePermission();
            this.CanCreateRdlElement = this.Session.OpenReferenceDataLibraries.Any();
        }

        /// <summary>
        /// Populate the <see cref="ContextMenuItemViewModel"/>s of the current browser
        /// </summary>
        public override void PopulateContextMenu()
        {
            base.PopulateContextMenu();

            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Simple Unit", "", this.CreateSimpleUnit, MenuItemKind.Create, ClassKind.SimpleUnit));
            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Derived Unit", "", this.CreateDerivedUnit, MenuItemKind.Create, ClassKind.DerivedUnit));
            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Linear Conversion Unit", "", this.CreateLinearConversionUnit, MenuItemKind.Create, ClassKind.LinearConversionUnit));
            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Prefixed Unit", "", this.CreatePrefixedUnit, MenuItemKind.Create, ClassKind.PrefixedUnit));
        }

        /// <summary>
        /// Initializes the create <see cref="ReactiveCommand"/> that allow a user to create the different kinds of <see cref="MeasurementUnit"/>s
        /// </summary>
        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            this.CreateSimpleUnit = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanCreateRdlElement));
            this.CreateSimpleUnit.Subscribe(_ => this.ExecuteCreateCommand<SimpleUnit>());

            this.CreateDerivedUnit = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanCreateRdlElement));
            this.CreateDerivedUnit.Subscribe(_ => this.ExecuteCreateCommand<DerivedUnit>());

            this.CreateLinearConversionUnit = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanCreateRdlElement));
            this.CreateLinearConversionUnit.Subscribe(_ => this.ExecuteCreateCommand<LinearConversionUnit>());

            this.CreatePrefixedUnit = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanCreateRdlElement));
            this.CreatePrefixedUnit.Subscribe(_ => this.ExecuteCreateCommand<PrefixedUnit>());
        }

        /// <summary>
        /// Loads the <see cref="Thing"/>s from the cache when the browser is instantiated.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            var openDataLibrariesIids = this.Session.OpenReferenceDataLibraries.Select(y => y.Iid);
            foreach (var referenceDataLibrary in this.Thing.AvailableReferenceDataLibraries().Where(x => openDataLibrariesIids.Contains(x.Iid)))
            {
                foreach (var measurementUnit in referenceDataLibrary.Unit)
                {
                    this.AddMeasurementUnitRowViewModel(measurementUnit);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        /// a value indicating whether the class is being disposed of
        /// </param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (var unit in this.MeasurementUnits)
            {
                unit.Dispose();
            }
        }
    }
}