window.initMap = (depotLat, depotLon, mapId) => {
    const el = document.getElementById("map");
    const map = new google.maps.Map(el, {
        center: { lat: depotLat, lng: depotLon },
        zoom: 11,
        mapId: mapId || ""
    });
    window.mapInstance = map;

    // depot marker
    new google.maps.marker.AdvancedMarkerElement({
        map,
        position: { lat: depotLat, lng: depotLon },
        title: "Depot"
    });

    console.log("✅ Map ready (vector mode).");
};

window.updateRoutes = (routes) => {
    if (!window.mapInstance) return;

    const colors = ["#FF0000", "#0000FF", "#008000", "#FF00FF", "#FFA500"];
    let colorIndex = 0;

    routes.forEach(route => {
        const color = colors[colorIndex++ % colors.length];
        const path = route.points.map(p => ({ lat: p.lat, lng: p.lon }));

        const directionsService = new google.maps.DirectionsService();
        const directionsRenderer = new google.maps.DirectionsRenderer({
            map: window.mapInstance,
            suppressMarkers: true,
            polylineOptions: { strokeColor: color, strokeOpacity: 0.8, strokeWeight: 4 }
        });

        const request = {
            origin: path[0],
            destination: path[path.length - 1],
            waypoints: path.slice(1, -1).map(p => ({ location: p })),
            travelMode: google.maps.TravelMode.DRIVING
        };

        directionsService.route(request, (result, status) => {
            if (status === "OK") directionsRenderer.setDirections(result);
            else console.error("Directions failed:", status);
        });

        // advanced markers at stops
        for (const p of path) {
            new google.maps.marker.AdvancedMarkerElement({
                map: window.mapInstance,
                position: p,
                title: route.vehicleId
            });
        }
    });

    console.log("✅ Routes rendered:", routes);
};
