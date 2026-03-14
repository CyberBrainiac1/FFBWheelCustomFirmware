(function () {
    "use strict";

    var angleValue = document.getElementById("angle-value");
    var rawCounts = document.getElementById("raw-counts");
    var firmware = document.getElementById("lbl-firmware");
    var connectionStatus = document.getElementById("lbl-connection-status");
    var statusLine = document.getElementById("status-line");
    var windowFrame = document.getElementById("window");

    var btnConnect = document.getElementById("btn-connect");
    var btnDisconnect = document.getElementById("btn-disconnect");
    var btnRead = document.getElementById("btn-read");
    var btnApply = document.getElementById("btn-apply");
    var btnSave = document.getElementById("btn-save");
    var btnReset = document.getElementById("btn-reset");
    var btnCenter = document.getElementById("btn-center");

    var settingControls = [
        "btn-read",
        "btn-apply",
        "btn-save",
        "btn-reset",
        "btn-center",
        "sld-range",
        "nud-range",
        "sld-force",
        "nud-force",
        "sld-min-force",
        "nud-min-force",
        "sld-damping",
        "nud-damping",
        "sld-friction",
        "nud-friction",
        "sld-spring",
        "nud-spring",
        "chk-inv-encoder",
        "chk-inv-motor"
    ];

    var linkedPairs = [
        ["sld-range", "nud-range"],
        ["sld-force", "nud-force"],
        ["sld-min-force", "nud-min-force"],
        ["sld-damping", "nud-damping"],
        ["sld-friction", "nud-friction"],
        ["sld-spring", "nud-spring"]
    ];

    var connected = false;
    var dirty = false;
    var angleTimer = null;
    var simulatedAngle = 0;
    var direction = 1;

    function setTitle() {
        document.title = dirty ? "DIY Wheel Config *" : "DIY Wheel Config Preview";
        windowFrame.dataset.dirty = dirty ? "1" : "0";
    }

    function setStatus(message) {
        statusLine.textContent = message;
    }

    function markDirty() {
        if (!connected) {
            return;
        }

        dirty = true;
        setTitle();
    }

    function setEnabled(enabled) {
        settingControls.forEach(function (id) {
            document.getElementById(id).disabled = !enabled;
        });
        btnConnect.disabled = enabled;
        btnDisconnect.disabled = !enabled;
    }

    function syncRangeFill(input) {
        var min = Number(input.min);
        var max = Number(input.max);
        var value = Number(input.value);
        var percent = ((value - min) / (max - min)) * 100;
        input.style.setProperty("--fill", percent + "%");
    }

    linkedPairs.forEach(function (pair) {
        var slider = document.getElementById(pair[0]);
        var numeric = document.getElementById(pair[1]);

        syncRangeFill(slider);

        slider.addEventListener("input", function () {
            numeric.value = slider.value;
            syncRangeFill(slider);
            markDirty();
        });

        numeric.addEventListener("input", function () {
            var value = Number(numeric.value);
            var min = Number(numeric.min);
            var max = Number(numeric.max);
            if (Number.isNaN(value)) {
                return;
            }

            value = Math.min(Math.max(value, min), max);
            numeric.value = value;
            slider.value = value;
            syncRangeFill(slider);
            markDirty();
        });
    });

    document.getElementById("chk-inv-encoder").addEventListener("change", markDirty);
    document.getElementById("chk-inv-motor").addEventListener("change", markDirty);

    function startLiveAngle() {
        simulatedAngle = 0;
        direction = 1;
        angleTimer = setInterval(function () {
            var range = Number(document.getElementById("sld-range").value);
            var halfRange = range / 2;
            simulatedAngle += direction * (Math.random() * 10 + 3);

            if (simulatedAngle >= halfRange) {
                simulatedAngle = halfRange;
                direction = -1;
            }

            if (simulatedAngle <= -halfRange) {
                simulatedAngle = -halfRange;
                direction = 1;
            }

            var shown = Math.round(simulatedAngle);
            var raw = Math.round(shown * 2400 / 360);

            angleValue.textContent = shown + "°";
            rawCounts.textContent = "Raw Counts: " + raw;
        }, 50);
    }

    function stopLiveAngle() {
        if (angleTimer) {
            clearInterval(angleTimer);
            angleTimer = null;
        }
    }

    function setConnected(state) {
        connected = state;
        setEnabled(state);
        connectionStatus.textContent = state ? "Status: Connected" : "Status: Disconnected";
        firmware.textContent = state ? "Firmware: 1.2.0-emc" : "Firmware: —";

        if (state) {
            setStatus("Connected to " + document.getElementById("port-select").value);
            startLiveAngle();
        } else {
            stopLiveAngle();
            angleValue.textContent = "0°";
            rawCounts.textContent = "Raw Counts: 0";
            dirty = false;
            setTitle();
            setStatus("Ready");
        }
    }

    btnConnect.addEventListener("click", function () {
        setConnected(true);
    });

    btnDisconnect.addEventListener("click", function () {
        setConnected(false);
    });

    btnRead.addEventListener("click", function () {
        setStatus(connected ? "Connected to " + document.getElementById("port-select").value : "Ready");
        dirty = false;
        setTitle();
    });

    btnApply.addEventListener("click", function () {
        if (!connected) {
            return;
        }

        setStatus("Settings applied");
        markDirty();
    });

    btnSave.addEventListener("click", function () {
        if (!connected) {
            return;
        }

        dirty = false;
        setTitle();
        setStatus("EEPROM saved");
    });

    btnReset.addEventListener("click", function () {
        if (!connected) {
            return;
        }

        if (!window.confirm("Reset all settings to defaults?")) {
            return;
        }

        document.getElementById("sld-range").value = 900;
        document.getElementById("nud-range").value = 900;
        document.getElementById("sld-force").value = 60;
        document.getElementById("nud-force").value = 60;
        document.getElementById("sld-min-force").value = 5;
        document.getElementById("nud-min-force").value = 5;
        document.getElementById("sld-damping").value = 10;
        document.getElementById("nud-damping").value = 10;
        document.getElementById("sld-friction").value = 4;
        document.getElementById("nud-friction").value = 4;
        document.getElementById("sld-spring").value = 15;
        document.getElementById("nud-spring").value = 15;
        document.getElementById("chk-inv-encoder").checked = false;
        document.getElementById("chk-inv-motor").checked = false;

        linkedPairs.forEach(function (pair) {
            syncRangeFill(document.getElementById(pair[0]));
        });

        dirty = true;
        setTitle();
        setStatus("Defaults restored");
    });

    btnCenter.addEventListener("click", function () {
        if (!connected) {
            return;
        }

        setStatus("Center set");
    });

    setConnected(false);
})();
