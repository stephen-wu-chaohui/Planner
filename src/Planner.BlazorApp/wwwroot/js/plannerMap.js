
let mapsResolver;
let mapsScriptLoading = false;
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

    // 2. Dynamically inject the Google Maps script tag if not already loading.
    //    This is required for Blazor WebAssembly where there is no server-side
    //    template to inject the script tag with the API key.
    if (!mapsScriptLoading && apiKey) {
        mapsScriptLoading = true;
        const script = document.createElement("script");
        script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=marker,places,routes,geometry&loading=async&callback=mapInteropInit`;
        script.async = true;
        script.defer = true;
        document.head.appendChild(script);
    }

    // 3. Wait for the 'mapInteropInit' callback to resolve this promise.
    return googleMapsPromise;
};

function getMidpointFromPath(path) {
    if (!Array.isArray(path) || path.length === 0) {
        return null;
    }

    const midpoint = path[Math.floor(path.length / 2)];
    if (!midpoint) {
        return null;
    }

    if (typeof midpoint.lat === "function" && typeof midpoint.lng === "function") {
        return { lat: midpoint.lat(), lng: midpoint.lng() };
    }

    return { lat: midpoint.lat, lng: midpoint.lng };
}

function createRouteLabel(routeName, color) {
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
    return labelDiv;
}

async function computeRoutePath(points) {
    if (google.maps.routes && google.maps.routes.Route && typeof google.maps.routes.Route.computeRoutes === "function") {
        const response = await google.maps.routes.Route.computeRoutes({
            origin: { lat: points[0].lat, lng: points[0].lng },
            destination: { lat: points[points.length - 1].lat, lng: points[points.length - 1].lng },
            intermediates: points.slice(1, points.length - 1).map(p => ({ lat: p.lat, lng: p.lng })),
            travelMode: google.maps.TravelMode.DRIVING,
            optimizeWaypointOrder: false
        });

        const route = response && response.routes && response.routes[0];
        if (route && Array.isArray(route.path) && route.path.length > 0) {
            return route.path;
        }

        if (route && route.polyline && route.polyline.encodedPolyline && google.maps.geometry && google.maps.geometry.encoding) {
            return google.maps.geometry.encoding.decodePath(route.polyline.encodedPolyline);
        }

        return null;
    }

    return null;
}

/* global google */
window.plannerMap = window.plannerMap || {
    map: null,
    markers: [],
    directionsRenderers: [],
    routeLabels: [],
    geocoder: null,
    activeRoute: null,

    // --- Initialization ---
    initMap: function (centerLat, centerLng, apiMapId) {
        this.map = new google.maps.Map(document.getElementById('map'), {
            center: { lat: centerLat, lng: centerLng },
            mapId: apiMapId
        });
        this.map.setZoom(4);
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
    drawRoute: async function (routeName, color, points) {
        if (!this.map || points.length < 2) return;

        try {
            const path = await computeRoutePath(points);

            if (!path || path.length === 0) {
                console.warn("Unable to compute route path.");
                return;
            }

            const routeLine = new google.maps.Polyline({
                map: this.map,
                path: path,
                strokeColor: color,
                strokeOpacity: 0.9,
                strokeWeight: 4
            });
            routeLine.routeName = routeName;
            routeLine.routeColor = color;
            this.directionsRenderers.push(routeLine);

            const midPoint = getMidpointFromPath(path);
            if (midPoint) {
                const label = new google.maps.marker.AdvancedMarkerElement({
                    position: midPoint,
                    map: this.map,
                    content: createRouteLabel(routeName, color)
                });
                this.routeLabels.push(label);
            }
        } catch (error) {
            console.warn("Route computation failed:", error);
        }
    },

    // --- Highlight / Reset ---
    highlightRoute: function (routeName) {
        if (!this.map) return;
        this.directionsRenderers.forEach(r => {
            const isTarget = (r.routeName === routeName);
            r.setOptions({
                strokeColor: isTarget ? "#FF4081" : r.routeColor,
                strokeOpacity: isTarget ? 1.0 : 0.3,
                strokeWeight: isTarget ? 6 : 3
            });
        });
        this.activeRoute = routeName;
    },

    resetHighlights: function () {
        if (!this.map) return;
        this.directionsRenderers.forEach(r => {
            r.setOptions({
                strokeColor: r.routeColor,
                strokeOpacity: 0.9,
                strokeWeight: 4
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
