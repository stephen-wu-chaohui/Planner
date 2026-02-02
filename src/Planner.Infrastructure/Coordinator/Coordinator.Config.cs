using System.Xml.Serialization;

namespace Planner.Infrastructure.Coordinator;

[XmlRoot("Coordinator")]
public class CoordinatorConfig {
    [XmlArray("TaskGroups")][XmlArrayItem("TaskGroup")] public List<TaskGroup> TaskGroups { get; set; } = new();
}
public class TaskGroup {
    [XmlElement("Name")] public string Name { get; set; } = string.Empty;
    [XmlArray("Tasks")][XmlArrayItem("Task")] public List<CoordinatorTask> Tasks { get; set; } = new();
}
public class CoordinatorTask {
    [XmlElement("ProcedureName")] public string ProcedureName { get; set; } = string.Empty;
    [XmlElement("RunTime")] public TimeSpan? RunTime { get; set; }
    [XmlElement("IntervalMinutes")] public int? IntervalMinutes { get; set; }
    [XmlIgnore] public DateTime? LastRunUtc { get; set; }
}
