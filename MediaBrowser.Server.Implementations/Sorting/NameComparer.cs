﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;
using System;

namespace MediaBrowser.Server.Implementations.Sorting
{
    /// <summary>
    /// Class NameComparer
    /// </summary>
    public class NameComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            if (!x.EnableAlphaNumericSorting || !y.EnableAlphaNumericSorting)
            {
                return string.Compare(x.SortName, y.SortName, StringComparison.CurrentCultureIgnoreCase);
            }

            return AlphanumComparator.CompareValues(x.Name, y.Name);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.Name; }
        }
    }
}
