﻿namespace DotVVM.Framework.Controls
{
    /// <summary>
    /// Represents settings for row (item) edit feature.
    /// </summary>
    public interface IRowEditOptions
    {
        /// <summary>
        /// Gets or sets the name of a property that uniquely identifies a row. (row ID, primary key, etc.). The value may be left out if the inline editing is not enabled.
        /// </summary>
        string? PrimaryKeyPropertyName { get; set; }

        /// <summary>
        /// Gets or sets the value of a <see cref="PrimaryKeyPropertyName"/> property for the row that is being edited. Null if nothing is edited.
        /// </summary>
        object? EditRowId { get; set; }

    }
}
