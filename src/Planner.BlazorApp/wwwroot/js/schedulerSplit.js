// wwwroot/js/schedulerSplit.js

// Make sure Split.js is loaded first (via import or CDN)
window.initSchedulerSplit = () => {
    if (window.schedulerSplit) return; // prevent multiple init

    window.schedulerSplit = Split(['#leftPane', '#rightPane'], {
        sizes: [80, 20],      // initial split ratio (percent)
        minSize: [200, 150],  // minimum pixel widths
        gutterSize: 12,        // width of draggable gutter
        cursor: 'col-resize',
        onDragEnd: (sizes) => {
            // optional: persist to localStorage
            localStorage.setItem('schedulerSplitSizes', JSON.stringify(sizes));
        }
    });

    // restore saved size if available
    const saved = localStorage.getItem('schedulerSplitSizes');
    if (saved) {
        const sizes = JSON.parse(saved);
        window.schedulerSplit.setSizes(sizes);
    }
};
