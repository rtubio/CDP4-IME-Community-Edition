﻿// -------------------------------------------------------------------------------------------------
// <copyright file="BudgetViewer.xaml.cs" company="RHEA System S.A.">
//   Copyright (c) 2015-2018 RHEA System S.A.
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace CDP4Budget.Views
{
    using CDP4Composition;
    using CDP4Composition.Attributes;

    /// <summary>
    /// Interaction logic for BudgetViewer view
    /// </summary>
    [PanelViewExport(RegionNames.RightPanel)]
    public partial class BudgetViewer : IPanelView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BudgetViewer"/> class
        /// </summary>
        public BudgetViewer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BudgetViewer"/> class.
        /// </summary>
        /// <param name="initializeComponent">
        /// a value indicating whether the contained Components shall be loaded
        /// </param>
        /// <remarks>
        /// This constructor is called by the navigation service
        /// </remarks>
        public BudgetViewer(bool initializeComponent)
        {
            if (initializeComponent)
            {
                this.InitializeComponent();
            }
        }
    }
}