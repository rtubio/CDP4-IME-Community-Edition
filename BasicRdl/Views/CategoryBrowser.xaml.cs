﻿// -------------------------------------------------------------------------------------------------
// <copyright file="CategoryBrowser.xaml.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace BasicRdl.Views
{
    using CDP4Composition;
    using CDP4Composition.Attributes;
    using DevExpress.Xpf.Grid;
    using NLog;

    /// <summary>
    /// Interaction logic for <see cref="CategoryBrowser"/>
    /// </summary>
    [PanelViewExport(RegionNames.LeftPanel)]
    public partial class CategoryBrowser : IPanelView
    {
        /// <summary>
        /// The NLog logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryBrowser"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is called by MEF
        /// </remarks>
        public CategoryBrowser()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasurementUnitsBrowser"/> class.
        /// </summary>
        /// <param name="initializeComponent">
        /// a value indicating whether the contained Components shall be loaded
        /// </param>
        /// <remarks>
        /// This constructor is called by the navigation service
        /// </remarks>
        public CategoryBrowser(bool initializeComponent)
        {
            if (initializeComponent)
            {
                this.InitializeComponent();
                var control = (GridControl)this.FindName("CategoriesGridControl");
                if (control != null)
                {
                    FilterStringService.FilterString.AddGridControl(control);
                    logger.Debug("{0} Added to the FilterStringService", control.Name);
                }
            }            
        }
    }
}
