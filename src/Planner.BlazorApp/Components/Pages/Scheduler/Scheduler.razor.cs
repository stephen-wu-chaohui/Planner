using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Planner.Contracts.Messages.VehicleRoutingProblem;
using System.Runtime.Intrinsics.Arm;

namespace Planner.BlazorApp.Components.Pages.Scheduler;

public partial class Scheduler
{
    private bool IsConnected = false;
    private double MouseX;
    private double MouseY;

    private void ToggleConnection()
    {
        IsConnected = !IsConnected;
    }

    private void UpdateMousePosition(MouseEventArgs e)
    {
        MouseX = Math.Round(e.ClientX, 0);
        MouseY = Math.Round(e.ClientY, 0);
    }

    private static async Task HandleSolve(List<ControlPanel.Vehicle> vehicles)
    {
        var request = new VrpRequestMessage {
            RequestId = $"Planner-{DateTime.Now:HHmmss}",
            Request = new VrpRequest {
                Depot = new DepotDto { Id = "Depot", Latitude = -31.9505, Longitude = 115.8605 },
                Vehicles = [.. vehicles.Select(v => new VehicleDto { Id = v.Id })],
                Jobs =
                [
                    new() { Id = "J1", Latitude = -31.9783, Longitude = 115.8180 },
                    new() { Id = "J2", Latitude = -32.0671, Longitude = 115.8957 },
                    new() { Id = "J3", Latitude = -31.9766, Longitude = 115.9213 }
                ],
                DistanceMatrix = [[0, 3, 5, 7, 9],[3, 0, 2, 6, 8],[5, 2, 0, 4, 7],[7, 6, 4, 0, 5],[9, 8, 7, 5, 0]]
            }
        };

        using var client = new HttpClient { BaseAddress = new Uri("https://localhost:7085/") };
        var response = await client.PostAsJsonAsync("api/vrp/solve", request);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Received VRP solution from API.");
        }
    }
}
