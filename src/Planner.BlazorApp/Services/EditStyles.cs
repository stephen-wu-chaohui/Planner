using Planner.BlazorApp.FormModels;

static public class EditStyles {

    /// <summary>
    /// Show CSS style for pending deleted and dirty items
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    static public string GetRowClass(this EditableFlags row) {
        if (row.PendingDeletion)
            return "item-pending-deletion";
        if (row.IsDirty)
            return "item-dirty";
        return string.Empty;
    }
}
