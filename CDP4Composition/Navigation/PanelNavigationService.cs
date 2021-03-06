﻿// ------------------------------------------------------------------------------------------------
// <copyright file="PanelNavigationService.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace CDP4Composition.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using Attributes;
    using CDP4Common.CommonData;
    using CDP4Dal;
    using CDP4Dal.Composition;
    using Events;
    using Interfaces;
    using Microsoft.Practices.Prism.Regions;
    using NLog;

    /// <summary>
    /// The panel navigation service class that provides services to open a docking panel given a <see cref="Thing"/> or a <see cref="IPanelViewModel"/>
    /// </summary>
    [Export(typeof(IPanelNavigationService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class PanelNavigationService : IPanelNavigationService
    {
        /// <summary>
        /// The fully qualified name of the PropertyGrid view-model
        /// </summary>
        private const string PropertyViewModel = "CDP4PropertyGrid.ViewModels.PropertyGridViewModel";

        /// <summary>
        /// The <see cref="IRegionManager"/> of the application
        /// </summary>
        private readonly IRegionManager regionManager;
        
        /// <summary>
        /// The logger for the current class
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="PanelNavigationService"/> class
        /// </summary>
        /// <param name="panelViewKinds">
        /// The injected list of <see cref="IPanelView"/> that can be navigated to.
        /// </param>
        /// <param name="panelViewModelKinds">
        /// The MEF injected Panel view models that can be navigated to.
        /// </param>
        /// <param name="regionManager">
        /// the <see cref="IRegionManager"/> that is used to manage the regions
        /// </param>
        /// <param name="panelViewModelDecorated">
        /// The MEF injected <see cref="IPanelViewModel"/> which are decorated with <see cref="INameMetaData"/> and can be navigated to.
        /// </param>
        [ImportingConstructor]
        public PanelNavigationService([ImportMany] IEnumerable<Lazy<IPanelView, IRegionMetaData>> panelViewKinds, [ImportMany] IEnumerable<IPanelViewModel> panelViewModelKinds, IRegionManager regionManager, [ImportMany] IEnumerable<Lazy<IPanelViewModel, INameMetaData>> panelViewModelDecorated)
        {
            var sw = new Stopwatch();
            sw.Start();
            logger.Debug("Instantiating the PanelNavigationService");

            this.regionManager = regionManager;
            this.PanelViewKinds = new Dictionary<string, Lazy<IPanelView, IRegionMetaData>>();

            // TODO T2428 : PanelViewModelKinds seems to be always empty and is used only one time in the Open(Thing thing, ISession session) method. We should probably refactor this part of the code.
            this.PanelViewModelKinds = new Dictionary<string, IPanelViewModel>();

            this.ViewModelViewPairs = new Dictionary<IPanelViewModel, IPanelView>();
            this.PanelViewModelDecorated = new Dictionary<string, Lazy<IPanelViewModel, INameMetaData>>();
            
            foreach (var panelView in panelViewKinds)
            {
                var panelViewName = panelView.Value.ToString();

                this.PanelViewKinds.Add(panelViewName, panelView);
                logger.Trace("Add panelView {0} ", panelViewName);
            }
            
            foreach (var panelViewModel in panelViewModelKinds)
            {
                var panelViewModelName = panelViewModel.ToString();

                this.PanelViewModelKinds.Add(panelViewModelName, panelViewModel);
                logger.Trace("Add panelViewModel {0} ", panelViewModelName);
            }

            foreach (var panelViewModel in panelViewModelDecorated)
            {
                var panelViewModelName = panelViewModel.Value.ToString();

                var panelViewModelDescribeName = panelViewModel.Metadata.Name;
                this.PanelViewModelDecorated.Add(panelViewModelDescribeName, panelViewModel);

                logger.Trace("Add panelViewModel {0} ", panelViewModelName);
            }

            // sets the event handler for the different regions
            try
            {
                this.regionManager.Regions[RegionNames.LeftPanel].Views.CollectionChanged += this.ViewCollectionChangedEventHandler;
            }
            catch (KeyNotFoundException)
            {
                logger.Debug("The {0} does not exist", RegionNames.LeftPanel);
            }

            try
            {
                this.regionManager.Regions[RegionNames.EditorPanel].Views.CollectionChanged += this.ViewCollectionChangedEventHandler;
            }
            catch (KeyNotFoundException)
            {
                logger.Debug("The {0} does not exist", RegionNames.EditorPanel);
            }

            try
            {
                this.regionManager.Regions[RegionNames.RightPanel].Views.CollectionChanged += this.ViewCollectionChangedEventHandler;
            }
            catch (KeyNotFoundException)
            {
                logger.Debug("The {0} does not exist", RegionNames.RightPanel);
            }

            sw.Stop();
            logger.Debug("The PanelNavigationService was instantiated in {0} [ms]", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Gets the list of <see cref="IPanelView"/> in the application
        /// </summary>
        public Dictionary<string, Lazy<IPanelView, IRegionMetaData>> PanelViewKinds { get; private set; }

        /// <summary>
        /// Gets the list of <see cref="IPanelView"/> in the application
        /// </summary>
        public Dictionary<string, IPanelViewModel> PanelViewModelKinds { get; private set; }

        /// <summary>
        /// Gets the {<see cref="IPanelViewModel"/>, <see cref="IPanelView"/>} pairs that are in the regions
        /// </summary>
        public Dictionary<IPanelViewModel, IPanelView> ViewModelViewPairs { get; private set; }

        /// <summary>
        /// Gets the list of the <see cref="IPanelViewModel"/> which are decorated with <see cref="INameMetaData"/>.
        /// </summary>
        public Dictionary<string, Lazy<IPanelViewModel, INameMetaData>> PanelViewModelDecorated { get; private set; }

        /// <summary>
        /// Opens the <see cref="IPanelView"/> associated to the <see cref="IPanelViewModel"/>
        /// </summary>
        /// <param name="viewModel">The <see cref="IPanelViewModel"/></param>
        private void Open(IPanelViewModel viewModel)
        {
            var lazyView = this.GetViewType(viewModel);

            var parameters = new object[] { true };
            var view = Activator.CreateInstance(lazyView.Value.GetType(), parameters) as IPanelView;
            view.DataContext = viewModel;
            
            this.ViewModelViewPairs.Add(viewModel, view);

            var region = this.regionManager.Regions[lazyView.Metadata.Region];
            region.Add(view, view.ToString() + Guid.NewGuid());

            logger.Trace("Navigated to Panel {0}", viewModel);
        }

        /// <summary>
        /// Opens the view associated to the provided view-model
        /// </summary>
        /// <param name="viewModel">
        /// The <see cref="IPanelViewModel"/> for which the associated view needs to be opened
        /// </param>
        /// <param name="useRegionManager">
        /// A value indicating whether handling the opening of the view shall be handled by the region manager. In case this region manager does not handle
        /// this it will be event-based using the <see cref="CDPMessageBus"/>.
        /// </param>
        /// <remarks>
        /// The data context of the view is the <see cref="IPanelViewModel"/>
        /// </remarks>
        public void Open(IPanelViewModel viewModel, bool useRegionManager)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel), "The IPanelViewModel may not be null");
            }
            
            if (useRegionManager)
            {
                this.Open(viewModel);
            }
            else
            {
                IPanelView view;
                this.ViewModelViewPairs.TryGetValue(viewModel, out view);

                string regionName = string.Empty;

                if (view == null)
                {
                    var lazyView = this.GetViewType(viewModel);
                    regionName = lazyView.Metadata.Region;

                    var parameters = new object[] { true };
                    view = Activator.CreateInstance(lazyView.Value.GetType(), parameters) as IPanelView;
                    view.DataContext = viewModel;
                    this.ViewModelViewPairs.Add(viewModel, view);
                }

                var openPanelEvent = new NavigationPanelEvent(viewModel, view, PanelStatus.Open, regionName);
                CDPMessageBus.Current.SendMessage(openPanelEvent);
            }            
        }

        /// <summary>
        /// Opens the <see cref="Thing"/> in a property panel
        /// </summary>
        /// <param name="thing">The <see cref="Thing"/> which properties are displayed</param>
        /// <param name="session">The <see cref="ISession"/> associated to the <see cref="Thing"/></param>
        public void Open(Thing thing, ISession session)
        {
            IPanelViewModel vm;
            if (!this.PanelViewModelKinds.TryGetValue(PropertyViewModel, out vm))
            {
                logger.Warn("The plugin for the Property panel could not be found.");
                return;
            }
            
            var viewModelType = vm.GetType();
            var propGridVmInstance = Activator.CreateInstance(viewModelType, thing, session) as IPanelViewModel;

            var existentViewModel = this.ViewModelViewPairs.Keys.SingleOrDefault(x => x.GetType() == viewModelType);

            if (existentViewModel != null)
            {
                // Updates the view-model of the property-grid
                var existentView = this.ViewModelViewPairs[existentViewModel];
                this.ViewModelViewPairs.Remove(existentViewModel);
                
                existentView.DataContext = propGridVmInstance;
                this.ViewModelViewPairs.Add(propGridVmInstance, existentView);
            }
        }

        /// <summary>
        /// Opens the view associated to a view-model. The view-model is identified by its <see cref="INameMetaData.Name"/>.
        /// </summary>
        /// <param name="viewModelName">The name we want to compare to the <see cref="INameMetaData.Name"/> of the view-models.</param>
        /// <param name="session">The <see cref="ISession"/> associated.</param>
        /// <param name="useRegionManager">A value indicating whether handling the opening of the view shall be handled by the region manager.
        /// In case this region manager does not handle this, it will be event-based using the <see cref="CDPMessageBus"/>.</param>
        /// <param name="thingDialogNavigationService">The <see cref="IThingDialogNavigationService"/>.</param>
        /// <param name="dialogNavigationService">The <see cref="IDialogNavigationService"/>.</param>
        public void Open(string viewModelName, ISession session, bool useRegionManager, IThingDialogNavigationService thingDialogNavigationService, IDialogNavigationService dialogNavigationService)
        {
            Lazy<IPanelViewModel, INameMetaData> returned;
            if (!this.PanelViewModelDecorated.TryGetValue(viewModelName, out returned))
            {
                throw new ArgumentOutOfRangeException(string.Format("The ViewModel with the human readable name {0} could not be found", viewModelName));
            }

            var siteDirectory = session.RetrieveSiteDirectory();
            // TODO T2429 : check that the view model is associated to a site directory
            var parameters = new object[] { session, siteDirectory, thingDialogNavigationService, this, dialogNavigationService};
            var viewModel = Activator.CreateInstance(returned.Value.GetType(), parameters) as IPanelViewModel;
            this.Open(viewModel, useRegionManager);
        }

        /// <summary>
        /// Closes the <see cref="IPanelView"/> associated to the <see cref="IPanelViewModel"/>
        /// </summary>
        /// <param name="viewModel">The <see cref="IPanelViewModel"/></param>
        private void Close(IPanelViewModel viewModel)
        {
            logger.Debug("Starting to Close view-model {0} of type {1}", viewModel.Caption, viewModel);

            IPanelView view;
            if (this.ViewModelViewPairs.TryGetValue(viewModel, out view))
            {
                var viewRegion = this.GetViewType(viewModel).Metadata.Region;
                var region = this.regionManager.Regions[viewRegion];
                region.Remove(view);

                logger.Debug("Closed view-model {0} of type {1}", viewModel.Caption, viewModel);
            }
        }

        /// <summary>
        /// Closes the <see cref="IPanelView"/> associated to the <see cref="IPanelViewModel"/>
        /// </summary>
        /// <param name="viewModel">
        /// The view-model that is to be closed.
        /// </param>
        /// <param name="useRegionManager">
        /// A value indicating whether handling the opening of the view shall be handled by the region manager. In case this region manager does not handle
        /// this it will be event-based using the <see cref="CDPMessageBus"/>.
        /// </param>
        public void Close(IPanelViewModel viewModel, bool useRegionManager)
        {
            if (useRegionManager)
            {
                this.Close(viewModel);                
            }
            else
            {
                IPanelView view;
                if (this.ViewModelViewPairs.TryGetValue(viewModel, out view))
                {
                    this.CleanUpPanelsAndSendCloseEvent(viewModel, view);
                }
            }
        }

        /// <summary>
        /// Closes all the <see cref="IPanelView"/> associated to a data-source
        /// </summary>
        /// <param name="datasourceUri">The string representation of the data-source's uri</param>
        public void Close(string datasourceUri)
        {
            logger.Debug("Starting to close all view-models related to data-source {0}", datasourceUri);

            var openViewModel = this.ViewModelViewPairs.Keys.Where(x => x.DataSource == datasourceUri).ToList();
            foreach (var panelViewModel in openViewModel)
            {
                this.Close(panelViewModel);
            }

            logger.Debug("All view-models related to data-source {0} closed", datasourceUri);
        }

        /// <summary>
        /// Closes all the <see cref="IPanelView"/> which associated <see cref="IPanelViewModel"/> is of a certain Type
        /// </summary>
        /// <param name="viewModelType">The <see cref="Type"/> of the <see cref="IPanelViewModel"/> to close</param>
        public void Close(Type viewModelType)
        {
            var panelToClose = this.ViewModelViewPairs.Keys.Where(vm => vm.GetType() == viewModelType).ToList();
            foreach (var panel in panelToClose)
            {
                var viewRegion = this.GetViewType(panel).Metadata.Region;
                var region = this.regionManager.Regions[viewRegion];
                region.Remove(this.ViewModelViewPairs[panel]);
            }
        }

        /// <summary>
        /// Gets the fully qualified name of the <see cref="IPanelView"/> associated to the <see cref="IPanelViewModel"/>
        /// </summary>
        /// <remarks>
        /// We assume here that for a <see cref="IPanelViewModel"/> with a fully qualified name xxx.yyy.ViewModels.DialogViewModel, the counterpart view is xxx.yyy.Views.Dialog
        /// </remarks>
        /// <param name="viewModel">The <see cref="IPanelViewModel"/></param>
        /// <returns>The Fully qualified Name</returns>
        private Lazy<IPanelView, IRegionMetaData> GetViewType(IPanelViewModel viewModel)
        {
            var fullyQualifiedName = viewModel.ToString().Replace(".ViewModels.", ".Views.");

            // remove "ViewModel" from the name to get the View Name
            var viewName = System.Text.RegularExpressions.Regex.Replace(fullyQualifiedName, "ViewModel$", "");
            
            Lazy<IPanelView, IRegionMetaData> returned;
            if (!this.PanelViewKinds.TryGetValue(viewName, out returned))
            {
                throw new ArgumentOutOfRangeException(string.Format("The View associated to the viewModel {0} could not be found\nMake sure the view has the proper attributes", viewModel));
            }

            return returned;
        }

        /// <summary>
        /// Handles the view CollectionChanged event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">the <see cref="NotifyCollectionChangedEventArgs"/></param>
        private void ViewCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            foreach (var view in e.OldItems)
            {
                var viewPanel = view as IPanelView;

                if (viewPanel == null)
                {
                    continue;
                }
                
                var pair = this.ViewModelViewPairs.SingleOrDefault(x => x.Value == viewPanel);
                if (!pair.Equals(default(KeyValuePair<IPanelViewModel, IPanelView>)))
                {
                    var panelViewModel = pair.Key;
                    var panelView = pair.Value;
                    this.CleanUpPanelsAndSendCloseEvent(panelViewModel, panelView);
                }
            }
        }

        /// <summary>
        /// removes the view and view-model from the <see cref="ViewModelViewPairs"/> and send a panel close event
        /// </summary>
        /// <param name="panelViewModel">
        /// The <see cref="IPanelViewModel"/> that needs to be cleaned up
        /// </param>
        /// <param name="panelView">
        /// The <see cref="IPanelView"/> that needs to be cleaned up
        /// </param>
        private void CleanUpPanelsAndSendCloseEvent(IPanelViewModel panelViewModel, IPanelView panelView)
        {
            this.ViewModelViewPairs.Remove(panelViewModel);
            var closePanelEvent = new NavigationPanelEvent(panelViewModel, panelView, PanelStatus.Closed);
            CDPMessageBus.Current.SendMessage(closePanelEvent);
            panelView.DataContext = null;
            panelViewModel.Dispose();            
        }
    }
}