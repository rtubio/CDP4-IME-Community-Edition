﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CellSelectionBehavior.cs" company="RHEA System S.A.">
//   Copyright (c) 2015-2019 RHEA System S.A.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4RelationshipMatrix.Behaviour
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Interactivity;
    using DevExpress.Xpf.Grid;

    /// <summary>
    /// An attached behaviour to add a selected-cell capability to the <see cref="GridControl"/>
    /// </summary>
    public class CellSelectionBehavior : Behavior<GridControl> {

        /// <summary>
        /// The dependency property to bind te selected-cells
        /// </summary>
        public static readonly DependencyProperty SelectedCellsProperty = DependencyProperty.Register("SelectedCells", typeof(List<object>), typeof(CellSelectionBehavior), null);

        /// <summary>
        /// The dependency property to bind the selected-cell
        /// </summary>
        public static readonly DependencyProperty SelectedCellProperty = DependencyProperty.Register("SelectedCell", typeof(object), typeof(CellSelectionBehavior), null);

        /// <summary>
        /// Gets or sets the selected-cells
        /// </summary>
        public List<object> SelectedCells {
            get { return (List<object>)this.GetValue(SelectedCellsProperty); }
            set { this.SetValue(SelectedCellsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the selected-cell
        /// </summary>
        public object SelectedCell
        {
            get { return this.GetValue(SelectedCellProperty); }
            set { this.SetValue(SelectedCellProperty, value); }
        }

        /// <summary>
        /// Override to add an event-handler to the selectionchanged event
        /// </summary>
        protected override void OnAttached() {
            base.OnAttached();
            this.AssociatedObject.SelectionChanged += this.AssociatedObject_SelectionChanged;
        }

        /// <summary>
        /// OVerride to remove the added event-handler
        /// </summary>
        protected override void OnDetaching() {
            base.OnDetaching();
            this.AssociatedObject.SelectionChanged -= this.AssociatedObject_SelectionChanged;
        }

        /// <summary>
        /// The selection-changed event-handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event</param>
        private void AssociatedObject_SelectionChanged(object sender, GridSelectionChangedEventArgs e) {
            this.SelectedCells = ((TableView)this.AssociatedObject.View).GetSelectedCells().Cast<object>().ToList();

            var firstcell = ((TableView)this.AssociatedObject.View).GetSelectedCells().FirstOrDefault();
            var selectedRow = this.AssociatedObject.SelectedItem as ExpandoObject;
            var dic = (IDictionary<string, object>)selectedRow;

            object output;
            this.SelectedCell = firstcell?.Column.FieldName != null && dic != null && dic.TryGetValue(firstcell.Column.FieldName, out output) ? output : null;
        }
    }
}