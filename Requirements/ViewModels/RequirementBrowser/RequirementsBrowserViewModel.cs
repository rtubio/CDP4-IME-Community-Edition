﻿// -------------------------------------------------------------------------------------------------
// <copyright file="RequirementsBrowserViewModel.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace CDP4Requirements.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.ReportingData;
    using CDP4Dal.Operations;
    using CDP4Common.SiteDirectoryData;
    using CDP4CommonView.ViewModels;
    using CDP4Composition;
    using CDP4Composition.DragDrop;
    using CDP4Composition.Mvvm;
    using CDP4Composition.Navigation;
    using CDP4Composition.Navigation.Interfaces;
    using CDP4Composition.PluginSettingService;
    using CDP4Dal;
    using CDP4Dal.Events;
    using CDP4Requirements.Views;
    using NLog;
    using ReactiveUI;

    /// <summary>
    /// The View-Model for the <see cref="RequirementsBrowser"/>
    /// </summary>
    public class RequirementsBrowserViewModel : ModellingThingBrowserViewModelBase, IPanelViewModel, IDropTarget
    {
        #region Fields

        /// <summary>
        /// The logger for the current class
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Backing field for <see cref="CanCreateReqSpec"/>
        /// </summary>
        private bool canCreateReqSpec;

        /// <summary>
        /// Backing field for <see cref="CanCreateRelationship"/>
        /// </summary>
        private bool canCreateRelationship;

        /// <summary>
        /// Backing field for <see cref="CanCreateRequirement"/>
        /// </summary>
        private bool canCreateRequirement;

        /// <summary>
        /// Backing field for <see cref="CanCreateRequirementGroup"/>
        /// </summary>
        private bool canCreateRequirementGroup;
        
        /// <summary>
        /// Backing field for <see cref="CurrentModel"/>
        /// </summary>
        private string currentModel;

        /// <summary>
        /// Backing field for <see cref="CurrentIteration"/>
        /// </summary>
        private int currentIteration;

        /// <summary>
        /// The Panel Caption
        /// </summary>
        private const string PanelCaption = "Requirements";

        /// <summary>
        /// A list of <see cref="RequirementsSpecificationEditorViewModel"/> that have been opened from the current view-model
        /// </summary>
        private readonly List<RequirementsSpecificationEditorViewModel> openRequirementsSpecificationEditorViewModels;
        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsBrowserViewModel"/> class
        /// </summary>
        /// <param name="iteration">The <see cref="Iteration"/> that contains <see cref="RequirementsSpecification"/></param>
        /// <param name="session">The session</param>
        /// <param name="thingDialogNavigationService">The <see cref="IThingDialogNavigationService"/> that is used to navigate to a dialog of a specific <see cref="Thing"/></param>
        /// <param name="panelNavigationService">The <see cref="IPanelNavigationService"/> that is used to navigate to a panel</param>
        /// <param name="dialogNavigationService">The <see cref="IDialogNavigationService"/></param>
        /// <param name="pluginSettingsService">
        /// The <see cref="IPluginSettingsService"/> used to read and write plugin setting files.
        /// </param>
        public RequirementsBrowserViewModel(Iteration iteration, ISession session, IThingDialogNavigationService thingDialogNavigationService, IPanelNavigationService panelNavigationService, IDialogNavigationService dialogNavigationService, IPluginSettingsService pluginSettingsService)
            : base(iteration, session, thingDialogNavigationService, panelNavigationService, dialogNavigationService, pluginSettingsService)
        {
            this.Caption = string.Format("{0}, iteration_{1}", PanelCaption, this.Thing.IterationSetup.IterationNumber);
            this.ToolTip = string.Format("{0}\n{1}\n{2}", ((EngineeringModel)this.Thing.Container).EngineeringModelSetup.Name, this.Thing.IDalUri, this.Session.ActivePerson.Name);

            this.ReqSpecificationRows = new ReactiveList<RequirementsSpecificationRowViewModel>();
            var model = (EngineeringModel)this.Thing.Container;
            this.ActiveParticipant = model.GetActiveParticipant(this.Session.ActivePerson);

            this.ComputeUserDependentPermission();

            this.UpdateRequirementSpecificationsRows();

            this.AddSubscriptions();
            this.UpdateProperties();

            this.openRequirementsSpecificationEditorViewModels = new List<RequirementsSpecificationEditorViewModel>();
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the active <see cref="Participant"/>
        /// </summary>
        public Participant ActiveParticipant { get; private set; }

        /// <summary>
        /// Gets the current model caption to be displayed in the browser
        /// </summary>
        public string CurrentModel
        {
            get { return this.currentModel; }
            private set { this.RaiseAndSetIfChanged(ref this.currentModel, value); }
        }

        /// <summary>
        /// Gets the current iteration caption to be displayed in the browser
        /// </summary>
        public int CurrentIteration
        {
            get { return this.currentIteration; }
            private set { this.RaiseAndSetIfChanged(ref this.currentIteration, value); }
        }

        /// <summary>
        /// Gets the view model current <see cref="EngineeringModelSetup"/>
        /// </summary>
        public EngineeringModelSetup CurrentEngineeringModelSetup
        {
            get { return this.Thing.IterationSetup.GetContainerOfType<EngineeringModelSetup>(); }
        }

        /// <summary>
        /// Gets the <see cref="RequirementsSpecificationRowViewModel"/> rows
        /// </summary>
        public ReactiveList<RequirementsSpecificationRowViewModel> ReqSpecificationRows { get; private set; }

        /// <summary>
        /// Gets the <see cref="ICommand"/> to create a <see cref="BinaryRelationship"/>
        /// </summary>
        public ReactiveCommand<object> CreateRelationshipCommand { get; private set; }

        /// <summary>
        /// Gets the Create Requirement command
        /// </summary>
        public ReactiveCommand<object> CreateRequirementCommand { get; private set; }

        /// <summary>
        /// Gets the Create Requirement Group command
        /// </summary>
        public ReactiveCommand<object> CreateRequirementGroupCommand { get; private set; }
        
        /// <summary>
        /// Gets the Navigate To <see cref="RequirementsSpecification"/> Editor Command
        /// </summary>
        public ReactiveCommand<object> NavigateToRequirementsSpecificationEditorCommand { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the create <see cref="RequirementsSpecification"/> command can be executed
        /// </summary>
        public bool CanCreateReqSpec
        {
            get { return this.canCreateReqSpec; }
            private set { this.RaiseAndSetIfChanged(ref this.canCreateReqSpec, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the create <see cref="BinaryRelationship"/> command can be executed
        /// </summary>
        public bool CanCreateRelationship
        {
            get { return this.canCreateRelationship; }
            private set { this.RaiseAndSetIfChanged(ref this.canCreateRelationship, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the create <see cref="Requirement"/> command can be executed
        /// </summary>
        public bool CanCreateRequirement
        {
            get { return this.canCreateRequirement; }
            private set { this.RaiseAndSetIfChanged(ref this.canCreateRequirement, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the create <see cref="RequirementsGroup"/> command can be executed
        /// </summary>
        public bool CanCreateRequirementGroup
        {
            get { return this.canCreateRequirementGroup; }
            private set { this.RaiseAndSetIfChanged(ref this.canCreateRequirementGroup, value); }
        }
        #endregion

        #region IDragSource, IDropTarget

        /// <summary>
        /// Updates the current drag state.
        /// </summary>
        /// <param name="dropInfo">
        ///  Information about the drag operation.
        /// </param>
        /// <remarks>
        /// To allow a drop at the current drag position, the <see cref="DropInfo.Effects"/> property on 
        /// <paramref name="dropInfo"/> should be set to a value other than <see cref="DragDropEffects.None"/>
        /// and <see cref="DropInfo.Payload"/> should be set to a non-null value.
        /// </remarks>
        public void DragOver(IDropInfo dropInfo)
        {
            logger.Trace("drag over {0}", dropInfo.TargetItem);
            var droptarget = dropInfo.TargetItem as IDropTarget;
            if (droptarget == null)
            {
                dropInfo.Effects = DragDropEffects.None;
                return;
            }

            droptarget.DragOver(dropInfo);
        }

        /// <summary>
        /// Performs the drop operation
        /// </summary>
        /// <param name="dropInfo">
        /// Information about the drop operation.
        /// </param>
        public async Task Drop(IDropInfo dropInfo)
        {
            var droptarget = dropInfo.TargetItem as IDropTarget;
            if (droptarget == null)
            {
                return;
            }

            try
            {
                this.IsBusy = true;
                await droptarget.Drop(dropInfo);
            }
            catch (Exception ex)
            {
                this.Feedback = ex.Message;
            }
            finally
            {
                this.IsBusy = false;
            }
        }
        #endregion

        #region private method

        /// <summary>
        /// Updates the browser with the current <see cref="RequirementsSpecification"/>s in this <see cref="Iteration"/>
        /// </summary>
        private void UpdateRequirementSpecificationsRows()
        {
            var currentReqSpec = this.ReqSpecificationRows.Select(x => x.Thing).ToList();
            var updatedReqSpec = this.Thing.RequirementsSpecification;

            var added = updatedReqSpec.Except(currentReqSpec).ToList();
            var removed = currentReqSpec.Except(updatedReqSpec).ToList();

            foreach (var requirementsSpecification in added)
            {
                this.AddSpecificationRow(requirementsSpecification);
            }

            foreach (var requirementsSpecification in removed)
            {
                this.RemoveSpecificationRow(requirementsSpecification);
            }
        }

        /// <summary>
        /// Add a row representing a <see cref="RequirementsSpecification"/>
        /// </summary>
        /// <param name="spec">The <see cref="RequirementsSpecification"/></param>
        private void AddSpecificationRow(RequirementsSpecification spec)
        {
            if (!this.ReqSpecificationRows.Select(x => x.Thing).Contains(spec))
            {
                var row = new RequirementsSpecificationRowViewModel(spec, this.Session, this);
                this.ReqSpecificationRows.Add(row);
            }
        }

        /// <summary>
        /// Removes a row representing a <see cref="RequirementsSpecification"/>
        /// </summary>
        /// <param name="spec">The <see cref="RequirementsSpecification"/></param>
        private void RemoveSpecificationRow(RequirementsSpecification spec)
        {
            var row = this.ReqSpecificationRows.SingleOrDefault(x => x.Thing == spec);
            if (row != null)
            {
                this.ReqSpecificationRows.Remove(row);
                row.Dispose();
            }
        }

        /// <summary>
        /// Compute the user-dependent permissions
        /// </summary>
        private void ComputeUserDependentPermission()
        {
            this.CanCreateReqSpec = this.PermissionService.CanWrite(ClassKind.RequirementsSpecification, this.Thing);
            this.CanCreateRelationship = this.PermissionService.CanWrite(ClassKind.BinaryRelationship, this.Thing);
        }

        /// <summary>
        /// Add the necessary subscriptions for this view model.
        /// </summary>
        private void AddSubscriptions()
        {
            var engineeringModelSetupSubscription = CDPMessageBus.Current.Listen<ObjectChangedEvent>(this.CurrentEngineeringModelSetup)
                    .Where(objectChange => objectChange.EventKind == EventKind.Updated && objectChange.ChangedThing.RevisionNumber > this.RevisionNumber && objectChange.ChangedThing.Cache == this.Session.Assembler.Cache)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => this.UpdateProperties());
            this.Disposables.Add(engineeringModelSetupSubscription);

            var domainOfExpertiseSubscription = CDPMessageBus.Current.Listen<ObjectChangedEvent>(typeof(DomainOfExpertise))
                    .Where(objectChange => objectChange.EventKind == EventKind.Updated && objectChange.ChangedThing.RevisionNumber > this.RevisionNumber && objectChange.ChangedThing.Cache == this.Session.Assembler.Cache)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => this.UpdateProperties());
            this.Disposables.Add(domainOfExpertiseSubscription);

            var iterationSetupSubscription = CDPMessageBus.Current.Listen<ObjectChangedEvent>(this.Thing.IterationSetup)
                    .Where(objectChange => objectChange.EventKind == EventKind.Updated && objectChange.ChangedThing.RevisionNumber > this.RevisionNumber && objectChange.ChangedThing.Cache == this.Session.Assembler.Cache)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => this.UpdateProperties());
            this.Disposables.Add(iterationSetupSubscription);
        }

        /// <summary>
        /// Update the properties of this view-model
        /// </summary>
        private void UpdateProperties()
        {
            this.CurrentModel = this.CurrentEngineeringModelSetup.Name;
            this.CurrentIteration = this.Thing.IterationSetup.IterationNumber;

            var iterationDomainPair = this.Session.OpenIterations.SingleOrDefault(x => x.Key == this.Thing);
            if (iterationDomainPair.Equals(default(KeyValuePair<Iteration, Tuple<DomainOfExpertise, Participant>>)))
            {
                this.DomainOfExpertise = "None";
            }
            else
            {
                this.DomainOfExpertise = (iterationDomainPair.Value == null || iterationDomainPair.Value.Item1 == null)
                                        ? "None"
                                        : string.Format("{0} [{1}]", iterationDomainPair.Value.Item1.Name, iterationDomainPair.Value.Item1.ShortName);
            }
        }

        /// <summary>
        /// Execute the <see cref="CreateRequirementCommand"/>
        /// </summary>
        private void ExecuteCreateRequirement()
        {
            var req = new Requirement();
            var reqGroup = this.SelectedThing.Thing as RequirementsGroup;

            var reqSpecification = this.SelectedThing.Thing as RequirementsSpecification ?? this.SelectedThing.Thing.GetContainerOfType<RequirementsSpecification>();
            if (reqGroup != null)
            {
                req.Group = reqGroup;
            }

            this.ExecuteCreateCommand(req, reqSpecification);
        }
        #endregion

        #region override method
        /// <summary>
        /// Initialize the <see cref="ICommand"/>s
        /// </summary>
        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            this.CreateCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanCreateReqSpec));
            this.CreateCommand.Subscribe(_ => this.ExecuteCreateCommand<RequirementsSpecification>(this.Thing));

            this.CreateRelationshipCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanCreateRelationship));
            this.CreateRelationshipCommand.Subscribe(_ => this.ExecuteCreateCommand<BinaryRelationship>(this.Thing));

            this.CreateRequirementGroupCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanCreateRequirement));
            this.CreateRequirementGroupCommand.Subscribe(_ => this.ExecuteCreateCommand<RequirementsGroup>(this.SelectedThing.Thing));

            this.CreateRequirementCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanCreateRequirementGroup));
            this.CreateRequirementCommand.Subscribe(_ => this.ExecuteCreateRequirement());

            this.NavigateToRequirementsSpecificationEditorCommand = ReactiveCommand.Create();
            this.NavigateToRequirementsSpecificationEditorCommand.Subscribe(_ => this.ExecuteNavigateToRequirementsSpecificationEditor());
        }

        /// <summary>
        /// The <see cref="ObjectChangedEvent"/> Handler that is invoked upon a update on the current iteration
        /// </summary>
        /// <param name="objectChange">The <see cref="ObjectChangedEvent"/> containing this <see cref="Iteration"/></param>
        protected override void ObjectChangeEventHandler(ObjectChangedEvent objectChange)
        {
            base.ObjectChangeEventHandler(objectChange);
            this.UpdateRequirementSpecificationsRows();
            this.UpdateProperties();
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
            foreach (var reqSpec in this.ReqSpecificationRows)
            {
                reqSpec.Dispose();
            }

            foreach (var vm in this.openRequirementsSpecificationEditorViewModels)
            {
                this.PanelNavigationService.Close(vm, true);
                vm.Dispose();
            }
        }

        /// <summary>
        /// Compute the permissions
        /// </summary>
        public override void ComputePermission()
        {
            base.ComputePermission();

            if (this.SelectedThing == null)
            {
                return;
            }

            var reqSpecRow = this.SelectedThing as RequirementsSpecificationRowViewModel;
            if (reqSpecRow != null)
            {
                this.CanCreateRequirement = this.PermissionService.CanWrite(ClassKind.Requirement, reqSpecRow.Thing);
                this.CanCreateRequirementGroup = this.PermissionService.CanWrite(ClassKind.RequirementsGroup, reqSpecRow.Thing);
            }

            var reqGroupRow = this.SelectedThing as RequirementsGroupRowViewModel;
            if (reqGroupRow != null)
            {
                this.CanCreateRequirement = this.PermissionService.CanWrite(ClassKind.Requirement, reqGroupRow.Thing.GetContainerOfType<RequirementsSpecification>());
                this.CanCreateRequirementGroup = this.PermissionService.CanWrite(ClassKind.RequirementsGroup, reqGroupRow.Thing);
            }
        }

        /// <summary>
        /// Populate the context menu
        /// </summary>
        public override void PopulateContextMenu()
        {
            base.PopulateContextMenu();
            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Binary Relationship", "", this.CreateRelationshipCommand, MenuItemKind.Create, ClassKind.BinaryRelationship));

            if (this.SelectedThing == null)
            {
                this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Requirement Specification", "", this.CreateCommand, MenuItemKind.Create, ClassKind.RequirementsSpecification));
                return;
            }

            var reqSpecRow = this.SelectedThing as RequirementsSpecificationRowViewModel;
            if (reqSpecRow != null)
            {
                this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Requirement Specification", "", this.CreateCommand, MenuItemKind.Create, ClassKind.RequirementsSpecification));
                this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Requirement", "", this.CreateRequirementCommand, MenuItemKind.Create, ClassKind.Requirement));
                this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Requirement Group", "", this.CreateRequirementGroupCommand, MenuItemKind.Create, ClassKind.RequirementsGroup));                
                this.ContextMenu.Add(new ContextMenuItemViewModel("Open Requirement Specification Editor", "", this.NavigateToRequirementsSpecificationEditorCommand, MenuItemKind.Navigate, ClassKind.RequirementsSpecification));
            }

            var reqGroupRow = this.SelectedThing as RequirementsGroupRowViewModel;
            if (reqGroupRow != null)
            {
                this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Requirement", "", this.CreateRequirementCommand, MenuItemKind.Create, ClassKind.Requirement));
                this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Requirement Group", "", this.CreateRequirementGroupCommand, MenuItemKind.Create, ClassKind.RequirementsGroup));
            }

            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Model Note", "", this.CreateEngineeringModelDataNoteCommand, MenuItemKind.Create, ClassKind.EngineeringModelDataNote));
            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Change Request", "", this.CreateChangeRequestCommand, MenuItemKind.Create, ClassKind.ChangeRequest));
            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Review Item Discrepancy", "", this.CreateReviewItemDiscrepancyCommand, MenuItemKind.Create, ClassKind.ReviewItemDiscrepancy));
            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Request for Deviation", "", this.CreateRequestForDeviationCommand, MenuItemKind.Create, ClassKind.RequestForDeviation));
            this.ContextMenu.Add(new ContextMenuItemViewModel("Create a Request for Waiver", "", this.CreateRequestForWaiverCommand, MenuItemKind.Create, ClassKind.RequestForWaiver));
        }

        /// <summary>
        /// Open the annotation floating dialog
        /// </summary>
        /// <param name="annotation">The associated <see cref="ModellingAnnotationItem"/></param>
        protected override void ExecuteOpenAnnotationWindow(ModellingAnnotationItem annotation)
        {
            var vm = new AnnotationFloatingDialogViewModel(annotation, this.Session);
            this.DialogNavigationService.NavigateFloating(vm);
        }

        /// <summary>
        /// Executes the navigation to the <see cref="RequirementsSpecificationEditorViewModel"/>
        /// </summary>
        private void ExecuteNavigateToRequirementsSpecificationEditor()
        {
            var requirementsSpecification = this.SelectedThing.Thing as RequirementsSpecification;

            if (requirementsSpecification != null)
            {
                var vm = new RequirementsSpecificationEditorViewModel(requirementsSpecification, this.Session, this.ThingDialogNavigationService, this.PanelNavigationService, this.DialogNavigationService, this.PluginSettingsService);
                this.openRequirementsSpecificationEditorViewModels.Add(vm);
                this.PanelNavigationService.Open(vm, true);                
            }
        }
        /// <summary>
        /// Execute the creation of a <see cref="ModellingAnnotationItem"/>
        /// </summary>
        protected void ExecuteCreateEngineeringModelDataNoteCommand(EngineeringModelDataNote engineeringModelDataNote, Participant participant, DomainOfExpertise owner)
        {
            if (this.SelectedThing == null)
            {
                return;
            }

            engineeringModelDataNote.Author = participant;
            var annotatedThing = new ModellingThingReference(this.SelectedThing.Thing);
            engineeringModelDataNote.RelatedThing.Add(annotatedThing);
            engineeringModelDataNote.PrimaryAnnotatedThing = annotatedThing;

            var transactionContext = TransactionContextResolver.ResolveContext(this.Thing);
            var model = this.Thing.TopContainer as EngineeringModel;
            if (model == null)
            {
                throw new InvalidOperationException("A modelling annotation item can only be created in the context of a Engineering Model.");
            }

            var containerClone = model.Clone(false);
            var transaction = new ThingTransaction(transactionContext, containerClone);
            this.ThingDialogNavigationService.Navigate(engineeringModelDataNote, transaction, this.Session, true, ThingDialogKind.Create, this.ThingDialogNavigationService, containerClone);
        }
        #endregion
    }
}