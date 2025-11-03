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
