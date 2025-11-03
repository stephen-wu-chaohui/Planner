//window.mapInteropInit = () => {
//    // Called by Google script once loaded
//    console.log("Google Maps API loaded.");
//};

//window.mapInterop = {
//    init: function (elementId, dotnetHelper, centerLat, centerLng, zoom) {
//        const map = new google.maps.Map(document.getElementById(elementId), {
//            center: { lat: centerLat, lng: centerLng },
//            zoom: zoom ?? 13
//        });

//        // click event -> send lat/lng back to Blazor
//        map.addListener("click", (e) => {
//            dotnetHelper.invokeMethodAsync("OnMapClick",
//                e.latLng.lat(), e.latLng.lng());
//        });

//        // store reference if you need later (optional)
//        window._mapInstance = map;
//    }
//};
