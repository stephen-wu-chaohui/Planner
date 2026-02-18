
let mapsResolver;
const googleMapsPromise = new Promise((resolve) => { mapsResolver = resolve; });

// Configuration constants
const INFOWINDOW_AUTO_CLOSE_DELAY = 2000; // milliseconds

window.mapInteropInit = () => {
    console.log("Google Maps API loaded.");
    if (mapsResolver) {
        mapsResolver();
    }
};

window.loadGoogleMaps = (apiKey) => {
    // 1. If Google Maps is already fully loaded, return immediately.
    if (window.google && google.maps && google.maps.marker) { 
        return Promise.resolve(); 
    }

    // 2. Otherwise, wait for the 'mapInteropInit' callback to resolve this promise.
    //    We assume the script tag in App.razor has already started the process.
    return googleMapsPromise;
};

/* global google */
window.plannerMap = window.plannerMap || {
    map: null,
    markers: [],
    directionsRenderers: [],
    routeLabels: [],
    directionsService: null,
    geocoder: null,
    activeRoute: null,

    // --- Initialization ---
    initMap: function (centerLat, centerLng, apiMapId) {
        this.map = new google.maps.Map(document.getElementById('map'), {
            center: { lat: centerLat, lng: centerLng },
            mapId: apiMapId
        });
        this.map.setZoom(4);

        this.directionsService = new google.maps.DirectionsService();
        this.geocoder = new google.maps.Geocoder();
    },

    // Always present - safe even if initMap hasn't finished yet
    recenter: function (centerLat, centerLng, zoom) {
        if (!this.map) return;

        this.map.setCenter({ lat: centerLat, lng: centerLng });
        if (zoom !== undefined && zoom !== null) {
            this.map.setZoom(zoom);
        }
    },

    registerClickHandler: function (dotnetRef, clickHandlerName) {
        const me = this;
        if (!me.map) return;

        me.map.addListener("click", function (e) {
            const domEvent = e.domEvent;
            if (!domEvent.ctrlKey) {
                return;
            }

            const latLng = e.latLng;

            me.geocoder.geocode({ location: latLng }).then((response) => {
                const infowindow = new google.maps.InfoWindow();
                let address = '', region = '';

                if (response.results && response.results.length > 0) {
                    address = response.results[0].formatted_address;
                    infowindow.setPosition(latLng);
                    infowindow.setContent(address);

                    // Fix: use the actual map instance (previously used undefined variable `map`)
                    infowindow.open(me.map);

                    // Close the infowindow after a short delay to prevent UI obstruction
                    setTimeout(() => {
                        infowindow.close();
                    }, INFOWINDOW_AUTO_CLOSE_DELAY);

                    const components = response.results[0].address_components;
                    region = components.find(c =>
                        c.types.includes("locality") || c.types.includes("sublocality")
                    )?.long_name;

                    const lat = e.latLng.lat();
                    const lng = e.latLng.lng();

                    dotnetRef.invokeMethodAsync(clickHandlerName, { lat, lng, address, label: region });
                } else {
                    console.log("No address found");
                }
            }).catch((error) => {
                console.error("Geocoder error:", error);
            });
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

    createCustomerInfoWindow: function () {
        const content = document.createElement("div");
        content.innerHTML = `<div style='font-size:24px;backgroundColor ="red"'>📍</div>`;
        return content;
    },

    removeMarker: function (title) {
        if (!this.markers || this.markers.length === 0) return;
        const toRemove = this.markers.filter(m => m.title === title);
        toRemove.forEach(m => m.map = null);

        this.markers = this.markers.filter(m => m.title !== title);
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
        this.directionsRenderers.forEach(r => r.setMap(null));
        this.directionsRenderers = [];
        this.routeLabels.forEach(l => l.setMap(null));
        this.routeLabels = [];
        this.activeRoute = null;
    },

    // --- Add marker with rich info popup ---
    addMarker: function (c) {
        if (!this.map) return;

        const marker = new google.maps.marker.AdvancedMarkerElement({
            position: { lat: c.lat, lng: c.lng },
            map: this.map,
            title: c.label,
            content: this.createCustomerInfoWindow(c)
        });

        this.markers.push(marker);
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
