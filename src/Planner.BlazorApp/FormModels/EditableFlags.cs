namespace Planner.BlazorApp.FormModels;

public class EditableFlags {
    public bool IsDirty { get; internal set; } = false;
    public bool PendingDeletion { get; internal set; } = false;

    public EditableFlags() { }
    
    public EditableFlags(EditableFlags other) {
        IsDirty = other.IsDirty;
        PendingDeletion = other.PendingDeletion;
    }
}
