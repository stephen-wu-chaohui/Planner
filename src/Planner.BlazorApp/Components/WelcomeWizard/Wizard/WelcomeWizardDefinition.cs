public static class WelcomeWizardDefinition {
    public static IReadOnlyList<WizardStep> Steps { get; } =
        new List<WizardStep>
        {
            new(
                Title: "Welcome to Planner Dispatch Center",
                ImageUrl: "images/wizard/welcome_to_planner_dispatch_center.png",
                Description:
                    "Planner Dispatch Center gives you a real-time operational view of vehicles, jobs, and routes. " +
                    "This short guide will help you get productive quickly."
            ),

            new(
                Title: "Create Jobs and Vehicles",
                ImageUrl: "images/wizard/create_jobs_and_vehicles.png",
                Description:
                    "Define delivery jobs, time windows, and constraints. " +
                    "Configure your fleet with capacities, costs, and shift limits so optimization reflects reality."
            ),

            new(
                Title: "Run Route Optimization",
                ImageUrl: "images/wizard/run_route_optimization.png",
                Description:
                    "With a single click, Planner calculates optimized routes using advanced vehicle routing algorithms. " +
                    "You can review each route on the map and inspect timing, distance, and cost."
            ),

            new(
                Title: "Dispatch with Confidence",
                ImageUrl: "images/wizard/dispatch_with_confidence.png",
                Description:
                    "Once routes are ready, dispatch them to drivers and monitor execution. " +
                    "Planner keeps everything visible so you stay in control."
            )
        };
}
