﻿// -------------------------------------------------------------------------------------------------
// <copyright file="OrganizationBrowserRibbonViewModel.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace CDP4SiteDirectory.ViewModels
{
    using CDP4Composition;
    using CDP4Composition.Mvvm;
    using CDP4Composition.Navigation;
    using CDP4Composition.Navigation.Interfaces;
    using CDP4Composition.PluginSettingService;
    using CDP4Dal;
    using CDP4SiteDirectory.Views;

    /// <summary>
    /// The view-model for the <see cref="OrganizationBrowserRibbon"/> view
    /// </summary>
    public class OrganizationBrowserRibbonViewModel : RibbonButtonSessionDependentViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationBrowserRibbonViewModel"/> class
        /// </summary>
        public OrganizationBrowserRibbonViewModel()
            : base(InstantiatePanelViewModel)
        {
        }

        /// <summary>
        /// returns a new instance of the <see cref="OrganizationBrowserViewModel"/> class
        /// </summary>
        /// <param name="session">The <see cref="ISession"/></param>
        /// <param name="thingDialogNavigationService">The <see cref="IThingDialogNavigationService"/></param>
        /// <param name="panelNavigationService">The <see cref="IPanelNavigationService"/></param>
        /// <param name="dialogNavigationService">The <see cref="IDialogNavigationService"/></param>
        /// <returns>An instance of the <see cref="OrganizationBrowserViewModel"/> class</returns>
        public static IPanelViewModel InstantiatePanelViewModel(ISession session, IThingDialogNavigationService thingDialogNavigationService, IPanelNavigationService panelNavigationService, IDialogNavigationService dialogNavigationService, IPluginSettingsService pluginSettingsService)
        {
            return new OrganizationBrowserViewModel(session, session.RetrieveSiteDirectory(), thingDialogNavigationService, panelNavigationService, dialogNavigationService, pluginSettingsService);
        }
    }
}