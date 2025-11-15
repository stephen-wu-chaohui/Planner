window.wizardStorage = {
    setSeen: function () {
        localStorage.setItem("planner-wizard-seen", "1");
    },
    hasSeen: function () {
        return localStorage.getItem("planner-wizard-seen") === "1";
    }
};
