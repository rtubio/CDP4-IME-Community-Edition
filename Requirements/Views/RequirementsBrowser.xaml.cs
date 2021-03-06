﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequirementsBrowser.xaml.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4Requirements.Views
{
    using CDP4Composition;
    using CDP4Composition.Attributes;
    using System.Windows.Controls;
    using DevExpress.Xpf.Grid;

    /// <summary>
    /// Interaction logic for RequirementsBrowser.xaml
    /// </summary>
    [PanelViewExport(RegionNames.LeftPanel)]
    public partial class RequirementsBrowser : UserControl, IPanelView
    {
        public RequirementsBrowser()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsBrowser"/> class.
        /// </summary>
        /// <param name="initializeComponent">
        /// a value indicating whether the contained Components shall be loaded
        /// </param>
        /// <remarks>
        /// This constructor is called by the navigation service
        /// </remarks>
        public RequirementsBrowser(bool initializeComponent)
        {
            if (initializeComponent)
            {
                this.InitializeComponent();
                var control = (TreeListControl)this.FindName("RequirementBrowserTreeListControl");
                if (control != null)
                {
                    FilterStringService.FilterString.AddTreeListControl(control);
                }
            }
        }
    }
}