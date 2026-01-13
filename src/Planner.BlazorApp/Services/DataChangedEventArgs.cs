namespace Planner.BlazorApp.Services;

public enum DataChangeType { Added, Updated, Deleted }

public class DataChangedEventArgs<T> {
    public required T Item { get; set; }
    public DataChangeType ChangeType { get; set; }
}
