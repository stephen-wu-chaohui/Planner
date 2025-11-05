window.mapInteropInit = () => {
    // Called by Google script once loaded
    console.log("Google Maps API loaded.");
};


window.loadGoogleMaps = (apiKey) => {
    return new Promise((resolve, reject) => {
        if (window.google && google.maps && google.maps.marker) { resolve(); return; }
        const s = document.createElement("script");
        s.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=marker&callback=mapInteropInit`;
        s.async = true;
        s.onload = resolve;
        s.onerror = reject;
        document.head.appendChild(s);
    });
};

/* global google */
window.plannerMap = {
    map: null,
    markers: [],
    directionsRenderers: [],
    routeLabels: [],
    directionsService: null,
    activeRoute: null,

    // --- Initialization ---
    initMap: function (centerLat, centerLng, apiMapId) {
        this.map = new google.maps.Map(document.getElementById('map'), {
            center: { lat: centerLat, lng: centerLng },
            zoom: 12,
            mapId: apiMapId
        });
        this.directionsService = new google.maps.DirectionsService();
    },

    registerClickHandler: function (dotnetRef, clickHandlerName) {

        this.map.addListener("click", function (e) {
            const domEvent = e.domEvent;
            if (domEvent.ctrlKey) {
                const lat = e.latLng.lat();
                const lng = e.latLng.lng();

                dotnetRef.invokeMethodAsync(clickHandlerName, lat, lng);
            }
        });
    },

    updateCustomers: function (customers) {
        this.clearMarkers();
        customers.forEach(c => {
            const marker = new google.maps.marker.AdvancedMarkerElement({
                position: { lat: c.lat, lng: c.lng },
                map: this.map,
                title: c.label,
                content: this.createCustomerInfoWindow(c)
            });

            this.markers.push(marker);
        });
    },

    createCustomerInfoWindow: function (customer) {
        const content = document.createElement("div");
        content.innerHTML = `<div style='font-size:30px;backgroundColor ="red"'>📍</div>`;
        return content;
    },
    clearMarkers: function () {
        this.markers.forEach(m => m.map = null);
        this.markers = [];
    },

    updateRoutes: function (routes) {
        this.clearRoutes();
        routes.forEach(r => {
            this.drawRoute(r.routeName, r.color, r.points);
        });
	},

    // --- Cleanup ---
    clearRoutes: function () {
        // return;
        this.markers.forEach(m => m.map = null);
        this.markers = [];
        this.directionsRenderers.forEach(r => r.setMap(null));
        this.directionsRenderers = [];
        this.routeLabels.forEach(l => l.setMap(null));
        this.routeLabels = [];
        this.activeRoute = null;
    },

    // --- Add marker with rich info popup ---
    addMarker: function (m) {
        if (!this.map) return;

        const content = document.createElement("div");
        content.innerHTML = `
            <div style="
                background:'orange';
                border-radius:50%;
                width:12px;
                height:12px;
                margin:4px;">
            </div>`;

        const advMarker = new google.maps.marker.AdvancedMarkerElement({
            position: { lat: m.lat, lng: m.lng },
            map: this.map,
            title: `${m.label} (${m.jobType})`,
            content: content
        });

        // build InfoWindow HTML
        const infoHtml = `
            <div style='min-width:180px;font-size:13px;'>
                <strong>${m.label}</strong><br/>
                Type: <span style="color:${m.color}">${m.jobType}</span><br/>
                Vehicle: ${m.routeName}<br/>
                Arrival: ${m.arrival?.toFixed?.(1) ?? m.arrival}<br/>
                Departure: ${m.departure?.toFixed?.(1) ?? m.departure}<br/>
                Pallets: ${m.palletLoad ?? 0}<br/>
                Weight: ${m.weightLoad ?? 0}<br/>
                Refrig: ${m.refrigeratedLoad ?? 0}
            </div>`;

        const info = new google.maps.InfoWindow({ content: infoHtml });

        advMarker.addListener("click", () => {
            info.open(this.map, advMarker);
        });

        this.markers.push(advMarker);
    },

    // --- Draw route along roads ---
    drawRoute: function (routeName, color, points) {
        if (!this.map || points.length < 2) return;

        const waypoints = points.slice(1, points.length - 1).map(p => ({
            location: { lat: p.lat, lng: p.lng },
            stopover: true
        }));

        const request = {
            origin: { lat: points[0].lat, lng: points[0].lng },
            destination: { lat: points[points.length - 1].lat, lng: points[points.length - 1].lng },
            waypoints: waypoints,
            travelMode: google.maps.TravelMode.DRIVING,
            optimizeWaypoints: false
        };

        this.directionsService.route(request, (result, status) => {
            if (status === "OK" && result) {
                const renderer = new google.maps.DirectionsRenderer({
                    map: this.map,
                    suppressMarkers: true,
                    preserveViewport: true,
                    polylineOptions: {
                        strokeColor: color,
                        strokeOpacity: 0.9,
                        strokeWeight: 4
                    }
                });
                renderer.setDirections(result);
                renderer.routeName = routeName;
                renderer.routeColor = color;
                this.directionsRenderers.push(renderer);

                // add small floating label
                const midLeg = result.routes[0].legs[Math.floor(result.routes[0].legs.length / 2)];
                const midPoint = midLeg.steps[Math.floor(midLeg.steps.length / 2)].end_location;
                const labelDiv = document.createElement("div");
                labelDiv.innerHTML = `
                    <div style="
                        background:${color};
                        color:white;
                        padding:2px 6px;
                        border-radius:4px;
                        font-size:10px;
                        box-shadow:0 0 2px rgba(0,0,0,0.5);
                        white-space:nowrap;">
                        ${routeName}
                    </div>`;
                const label = new google.maps.marker.AdvancedMarkerElement({
                    position: midPoint,
                    map: this.map,
                    content: labelDiv
                });
                this.routeLabels.push(label);
            } else {
                console.warn("DirectionsService failed:", status);
            }
        });
    },

    // --- Highlight / Reset ---
    highlightRoute: function (routeName) {
        if (!this.map) return;
        this.directionsRenderers.forEach(r => {
            const isTarget = (r.routeName === routeName);
            r.setOptions({
                polylineOptions: {
                    strokeColor: isTarget ? "#FF4081" : r.routeColor,
                    strokeOpacity: isTarget ? 1.0 : 0.3,
                    strokeWeight: isTarget ? 6 : 3
                }
            });
        });
        this.activeRoute = routeName;
    },

    resetHighlights: function () {
        if (!this.map) return;
        this.directionsRenderers.forEach(r => {
            r.setOptions({
                polylineOptions: {
                    strokeColor: r.routeColor,
                    strokeOpacity: 0.9,
                    strokeWeight: 4
                }
            });
        });
        this.activeRoute = null;
    },

    // --- Fit to all markers ---
    fitToBounds: function () {
        if (!this.map || this.markers.length === 0) return;
        const bounds = new google.maps.LatLngBounds();
        this.markers.forEach(m => bounds.extend(m.position));
        this.map.fitBounds(bounds);
    }
};
