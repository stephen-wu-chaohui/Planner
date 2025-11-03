window.PlannerMap = (() => {
    let mapInstance = null;
    let activeRenderers = [];
    let activeMarkers = [];
    let activeLabels = [];
    let currentHighlight = null;
    let dotNetRef = null;
	let clickHandler = null;
    let mapsReady = false;

    // --- Private helpers ---
    function highlightRoute(renderer, color) {
        for (const r of activeRenderers) {
            const opts = r.getOptions().polylineOptions;
            opts.strokeOpacity = (r === renderer) ? 1.0 : 0.3;
            opts.strokeWeight = (r === renderer) ? 6 : 3;
            opts.strokeColor = (r === renderer) ? color : "#CCCCCC";
            r.setOptions({ polylineOptions: opts });
        }
    }

    function resetHighlights() {
        for (const r of activeRenderers) {
            const opts = r.getOptions().polylineOptions;
            opts.strokeOpacity = 0.9;
            opts.strokeWeight = 4;
            r.setOptions({ polylineOptions: opts });
        }
    }

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
            Object.assign(labelDiv.style, {
                background: color,
                color: "white",
                padding: "4px 8px",
                borderRadius: "6px",
                fontSize: "12px",
                fontWeight: "bold",
                boxShadow: "0 1px 3px rgba(0,0,0,0.3)",
                whiteSpace: "nowrap",
                cursor: "pointer"
            });

            const tooltip = document.createElement("div");
            Object.assign(tooltip.style, {
                position: "absolute",
                background: "rgba(0,0,0,0.75)",
                color: "white",
                padding: "4px 8px",
                borderRadius: "4px",
                fontSize: "11px",
                display: "none",
                whiteSpace: "nowrap",
                transform: "translate(-50%, -120%)",
                pointerEvents: "none"
            });
            tooltip.textContent = route.points.map((p, i) => `Stop ${i + 1}`).join(" → ");
            labelDiv.appendChild(tooltip);

            labelDiv.addEventListener("mouseenter", () => tooltip.style.display = "block");
            labelDiv.addEventListener("mouseleave", () => tooltip.style.display = "none");

            labelDiv.addEventListener("click", () => {
                if (currentHighlight === directionsRenderer) {
                    resetHighlights();
                    currentHighlight = null;
                    return;
                }
                resetHighlights();
                highlightRoute(directionsRenderer, color);
                currentHighlight = directionsRenderer;
            });

            const labelMarker = new google.maps.marker.AdvancedMarkerElement({
                map: mapInstance,
                position: { lat: midLat, lng: midLng },
                content: labelDiv,
                title: `${route.vehicleId} — ${totalKm} km`
            });

            activeLabels.push(labelMarker);
        } catch (e) {
            console.warn("⚠️ Could not add label:", e);
        }
    }

    // --- Public API ---
    function initMap(depotLat, depotLon, mapId) {
        const el = document.getElementById("map");
        const map = new google.maps.Map(el, {
            center: { lat: depotLat, lng: depotLon },
            zoom: 8,
            mapId: mapId || ""
        });

        mapInstance = map;
        activeRenderers = [];
        activeMarkers = [];
        activeLabels = [];
        currentHighlight = null;

        // Listen for click and report to Blazor
        map.addListener("click", (e) => {
            if (dotNetRef && clickHandler) {
                dotNetRef.invokeMethodAsync(clickHandler, {
                    lat: e.latLng.lat(),
                    lng: e.latLng.lng()
                });
            }
        });

        const depotMarker = new google.maps.marker.AdvancedMarkerElement({
            map,
            position: { lat: depotLat, lng: depotLon },
            title: "Depot"
        });
        activeMarkers.push(depotMarker);

        console.log("✅ PlannerMap initialized.");
    }

    function clearRoutes() {
        for (const list of [activeRenderers, activeMarkers, activeLabels]) {
            if (list) {
                for (const item of list) item.map = null;
                list.length = 0;
            }
        }
        currentHighlight = null;
        console.log("🧹 Cleared all routes and markers.");
    }

    function updateRoutes(routes) {
        if (!mapInstance) return;
        this.clearRoutes();

        const colors = ["#FF0000", "#0000FF", "#008000", "#FF00FF", "#FFA500"];
        let colorIndex = 0;

        routes.forEach(route => {
            const color = colors[colorIndex++ % colors.length];
            const path = route.points.map(p => ({ lat: p.lat, lng: p.lon }));

            const directionsService = new google.maps.DirectionsService();
            const directionsRenderer = new google.maps.DirectionsRenderer({
                map: mapInstance,
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

            activeRenderers.push(directionsRenderer);

            for (const p of path) {
                const marker = new google.maps.marker.AdvancedMarkerElement({
                    map: mapInstance,
                    position: p,
                    title: route.vehicleId
                });
                activeMarkers.push(marker);
            }
        });

        console.log(`✅ Rendered ${routes.length} routes.`);
    }

    // 🔗 Register DotNet reference for callback
    function registerClickHandler(dotNetObjectRef, clickHandlerName) {
        dotNetRef = dotNetObjectRef;
        clickHandler = clickHandlerName;
        console.log("🔗 Registered Blazor click handler.");
    }

    function onGoogleMapsReady() {
        mapsReady = true;
        console.log("✅ PlannerMap: Maps API is ready.");
    }

    async function getDistanceMatrix() {
        if (!mapInstance) return;

        const origins = activeMarkers.map(marker => marker.getPosition());
        const destinations = activeMarkers.map(marker => marker.getPosition());

        const response = await fetch("/api/distanceMatrix", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ origins, destinations })
        });

        if (!response.ok) {
            console.error("Failed to fetch distance matrix:", response.statusText);
            return null;
        }

        return await response.json();
    }

    return {
        initMap,
        clearRoutes,
        updateRoutes,
        registerClickHandler,
        onGoogleMapsReady,
        getDistanceMatrix
    };
})();


window.mapInteropInit = () => {
	window.PlannerMap.onGoogleMapsReady();
};

