window.initMap = (depotLat, depotLon, mapId) => {
    const el = document.getElementById("map");
    const map = new google.maps.Map(el, {
        center: { lat: depotLat, lng: depotLon },
        zoom: 11,
        mapId: mapId || ""
    });

    window.mapInstance = map;
    window.activeRenderers = [];
    window.activeMarkers = [];
    window.activeLabels = [];
    window.currentHighlight = null;

    const depotMarker = new google.maps.marker.AdvancedMarkerElement({
        map,
        position: { lat: depotLat, lng: depotLon },
        title: "Depot"
    });
    window.activeMarkers.push(depotMarker);

    console.log("✅ Map initialized (vector mode).");
};

window.clearRoutes = () => {
    for (const list of [window.activeRenderers, window.activeMarkers, window.activeLabels]) {
        if (list) {
            for (const item of list) item.map = null;
            list.length = 0;
        }
    }
    window.currentHighlight = null;
    console.log("🧹 Cleared previous routes and markers.");
};

window.updateRoutes = (routes) => {
    if (!window.mapInstance) return;
    window.clearRoutes();

    const colors = ["#FF0000", "#0000FF", "#008000", "#FF00FF", "#FFA500"];
    let colorIndex = 0;

    routes.forEach(route => {
        const color = colors[colorIndex++ % colors.length];
        const path = route.points.map(p => ({ lat: p.lat, lng: p.lon }));

        const directionsService = new google.maps.DirectionsService();
        const directionsRenderer = new google.maps.DirectionsRenderer({
            map: window.mapInstance,
            suppressMarkers: true,
            polylineOptions: {
                strokeColor: color,
                strokeOpacity: 0.9,
                strokeWeight: 4
            }
        });

        const request = {
            origin: path[0],
            destination: path[path.length - 1],
            waypoints: path.slice(1, -1).map(p => ({ location: p })),
            travelMode: google.maps.TravelMode.DRIVING
        };

        directionsService.route(request, (result, status) => {
            if (status === "OK") {
                directionsRenderer.setDirections(result);
                addRouteLabel(route, color, result.routes[0], directionsRenderer);
            } else {
                console.error("Directions failed:", status);
            }
        });

        window.activeRenderers.push(directionsRenderer);

        for (const p of path) {
            const marker = new google.maps.marker.AdvancedMarkerElement({
                map: window.mapInstance,
                position: p,
                title: route.vehicleId
            });
            window.activeMarkers.push(marker);
        }
    });

    console.log("✅ Routes rendered:", routes.length);
};

function addRouteLabel(route, color, googleRoute, directionsRenderer) {
    try {
        const leg = googleRoute.legs[0];
        const totalMeters = googleRoute.legs.reduce((sum, l) => sum + l.distance.value, 0);
        const totalKm = (totalMeters / 1000).toFixed(1);

        const start = leg.start_location;
        const end = leg.end_location;
        const midLat = (start.lat() + end.lat()) / 2;
        const midLng = (start.lng() + end.lng()) / 2;

        const labelDiv = document.createElement("div");
        labelDiv.textContent = `${route.vehicleId} — ${totalKm} km`;
        labelDiv.style.background = color;
        labelDiv.style.color = "white";
        labelDiv.style.padding = "4px 8px";
        labelDiv.style.borderRadius = "6px";
        labelDiv.style.fontSize = "12px";
        labelDiv.style.fontWeight = "bold";
        labelDiv.style.boxShadow = "0 1px 3px rgba(0,0,0,0.3)";
        labelDiv.style.whiteSpace = "nowrap";
        labelDiv.style.cursor = "pointer";

        // Tooltip showing stop sequence
        const tooltip = document.createElement("div");
        tooltip.style.position = "absolute";
        tooltip.style.background = "rgba(0,0,0,0.75)";
        tooltip.style.color = "white";
        tooltip.style.padding = "4px 8px";
        tooltip.style.borderRadius = "4px";
        tooltip.style.fontSize = "11px";
        tooltip.style.display = "none";
        tooltip.style.whiteSpace = "nowrap";
        tooltip.style.transform = "translate(-50%, -120%)";
        tooltip.style.pointerEvents = "none";
        tooltip.textContent = route.points.map((p, i) => `Stop ${i + 1}`).join(" → ");
        labelDiv.appendChild(tooltip);

        labelDiv.addEventListener("mouseenter", () => {
            tooltip.style.display = "block";
        });
        labelDiv.addEventListener("mouseleave", () => {
            tooltip.style.display = "none";
        });

        // Click = highlight + zoom
        labelDiv.addEventListener("click", () => {
            if (window.currentHighlight === directionsRenderer) {
                resetHighlights();
                window.currentHighlight = null;
                return;
            }
            resetHighlights();
            highlightRoute(directionsRenderer, color);
            zoomToRoute(googleRoute);
            window.currentHighlight = directionsRenderer;
        });

        const labelMarker = new google.maps.marker.AdvancedMarkerElement({
            map: window.mapInstance,
            position: { lat: midLat, lng: midLng },
            content: labelDiv,
            title: `${route.vehicleId} — ${totalKm} km`
        });

        window.activeLabels.push(labelMarker);
    } catch (e) {
        console.warn("⚠️ Could not add label:", e);
    }
}

function highlightRoute(renderer, color) {
    for (const r of window.activeRenderers) {
        const opts = r.getOptions().polylineOptions;
        opts.strokeOpacity = (r === renderer) ? 1.0 : 0.3;
        opts.strokeWeight = (r === renderer) ? 6 : 3;
        opts.strokeColor = (r === renderer) ? color : "#CCCCCC";
        r.setOptions({ polylineOptions: opts });
    }
}

function resetHighlights() {
    for (const r of window.activeRenderers) {
        const opts = r.getOptions().polylineOptions;
        opts.strokeOpacity = 0.9;
        opts.strokeWeight = 4;
        opts.strokeColor = opts.strokeColor || "#000000";
        r.setOptions({ polylineOptions: opts });
    }
}

// 🔍 Auto-zoom to fit selected route
function zoomToRoute(googleRoute) {
    try {
        const bounds = new google.maps.LatLngBounds();
        googleRoute.legs.forEach(leg => {
            leg.steps.forEach(step => {
                step.path.forEach(p => bounds.extend(p));
            });
        });
        window.mapInstance.fitBounds(bounds);
        console.log("🔎 Zoomed to selected route.");
    } catch (e) {
        console.warn("⚠️ Could not zoom to route:", e);
    }
}
