﻿// ------------------------------------------------------------------------------------------------
// <copyright file="ProductTree.xaml.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace CDP4ProductTree.Views
{
    using System.Windows.Controls;
    using CDP4Composition;
    using CDP4Composition.Attributes;

    /// <summary>
    /// Interaction logic for CDP4ProductTree.xaml
    /// </summary>
    [PanelViewExport(RegionNames.EditorPanel)]
    public partial class ProductTree : UserControl, IPanelView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductTree"/> class
        /// </summary>
        public ProductTree()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductTree"/> class.
        /// </summary>
        /// <param name="initializeComponent">
        /// a value indicating whether the contained Components shall be loaded
        /// </param>
        /// <remarks>
        /// This constructor is called by the navigation service
        /// </remarks>
        public ProductTree(bool initializeComponent)
        {
            if (initializeComponent)
            {
                this.InitializeComponent();
            }
        }
    }
}