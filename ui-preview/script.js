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
    var btnSimHold = document.getElementById("btn-sim-hold");
    var btnSimAuto = document.getElementById("btn-sim-auto");
    var btnFfbBump = document.getElementById("btn-ffb-bump");
    var btnFfbCurb = document.getElementById("btn-ffb-curb");
    var sldTestAngle = document.getElementById("sld-test-angle");
    var wheel = document.getElementById("wheel");
    var ffbMeterFill = document.getElementById("ffb-meter-fill");
    var simReadout = document.getElementById("sim-readout");

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
        "chk-inv-motor",
        "sld-test-angle",
        "btn-sim-hold",
        "btn-sim-auto",
        "btn-ffb-bump",
        "btn-ffb-curb"
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
    var simulatedAngle = 0;
    var simulatedVelocity = 0;
    var targetAngle = 0;
    var mode = "auto";
    var kickTorque = 0;
    var ffbLoad = 0;
    var dragState = null;
    var simLastTs = 0;

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

    function syncTestAngleBounds() {
        var halfRange = Math.round(getHalfRange());
        sldTestAngle.min = String(-halfRange);
        sldTestAngle.max = String(halfRange);
        targetAngle = clampAngle(targetAngle);
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

    document.getElementById("sld-range").addEventListener("input", syncTestAngleBounds);
    document.getElementById("nud-range").addEventListener("input", syncTestAngleBounds);

    document.getElementById("chk-inv-encoder").addEventListener("change", markDirty);
    document.getElementById("chk-inv-motor").addEventListener("change", markDirty);

    function getHalfRange() {
        return Number(document.getElementById("sld-range").value) / 2;
    }

    function clampAngle(degrees) {
        var halfRange = getHalfRange();
        return Math.max(Math.min(degrees, halfRange), -halfRange);
    }

    function getPointerAngle(evt) {
        var rect = wheel.getBoundingClientRect();
        var cx = rect.left + rect.width / 2;
        var cy = rect.top + rect.height / 2;
        var dx = evt.clientX - cx;
        var dy = evt.clientY - cy;
        return Math.atan2(dy, dx) * 180 / Math.PI + 90;
    }

    function setMode(nextMode) {
        mode = nextMode;
        btnSimHold.classList.toggle("primary", nextMode === "hold");
        btnSimAuto.classList.toggle("primary", nextMode === "auto");
    }

    function refreshSimulatorUi() {
        wheel.style.transform = "rotate(" + simulatedAngle + "deg)";
        var shown = Math.round(simulatedAngle);
        var raw = Math.round(shown * 2400 / 360);
        var sliderValue = mode === "hold" ? Math.round(targetAngle) : shown;

        angleValue.textContent = shown + "°";
        rawCounts.textContent = "Raw Counts: " + raw;
        sldTestAngle.value = String(clampAngle(sliderValue));

        var loadPercent = Math.min(Math.round(ffbLoad), 100);
        ffbMeterFill.style.width = loadPercent + "%";
        simReadout.textContent = "FFB Load: " + loadPercent + "%";
    }

    function injectRoadEffect(level) {
        kickTorque += level;
        setStatus(level > 0 ? "Road bump pulse injected" : "Curb strike pulse injected");
    }

    function runSimulator(ts) {
        if (!simLastTs) {
            simLastTs = ts;
        }

        var dt = Math.min((ts - simLastTs) / 1000, 0.05);
        simLastTs = ts;

        if (connected) {
            var halfRange = getHalfRange();
            var now = ts / 1000;
            var overall = Number(document.getElementById("sld-force").value) / 100;
            var minForce = Number(document.getElementById("sld-min-force").value) / 100;
            var damping = Number(document.getElementById("sld-damping").value) / 100;
            var friction = Number(document.getElementById("sld-friction").value) / 100;
            var spring = Number(document.getElementById("sld-spring").value) / 100;

            if (mode === "auto") {
                targetAngle = Math.sin(now * 0.9) * halfRange * 0.92;
            }

            targetAngle = clampAngle(targetAngle);

            var centerTorque = -simulatedAngle * (0.12 + spring * 0.45);
            var followTorque = (targetAngle - simulatedAngle) * (0.15 + spring * 0.65);
            var driveTorque = (centerTorque + followTorque) * (0.35 + overall * 1.2);
            var dampingTorque = simulatedVelocity * (0.8 + damping * 4.5 + friction * 2.4);
            var accel = driveTorque - dampingTorque + kickTorque;

            if (Math.abs(accel) > 0.01) {
                accel += (accel > 0 ? 1 : -1) * minForce * 0.4;
            }

            simulatedVelocity += accel * dt * 30;
            simulatedAngle += simulatedVelocity * dt * 30;
            simulatedAngle = clampAngle(simulatedAngle);

            if (Math.abs(simulatedAngle) >= halfRange && Math.abs(simulatedVelocity) > 0.01) {
                simulatedVelocity *= -0.2;
            }

            kickTorque *= Math.max(0, 1 - dt * 7);
            ffbLoad = Math.min(Math.abs(accel) * 10, 100);
        }

        refreshSimulatorUi();
        window.requestAnimationFrame(runSimulator);
    }

    wheel.addEventListener("pointerdown", function (evt) {
        if (!connected) {
            return;
        }

        dragState = {
            pointerStart: getPointerAngle(evt),
            wheelStart: simulatedAngle
        };

        setMode("manual");
        wheel.setPointerCapture(evt.pointerId);
    });

    wheel.addEventListener("pointermove", function (evt) {
        if (!connected || !dragState) {
            return;
        }

        var pointerNow = getPointerAngle(evt);
        var delta = pointerNow - dragState.pointerStart;
        simulatedAngle = clampAngle(dragState.wheelStart + delta);
        simulatedVelocity = 0;
        targetAngle = simulatedAngle;
    });

    function endWheelDrag() {
        dragState = null;
    }

    wheel.addEventListener("pointerup", endWheelDrag);
    wheel.addEventListener("pointercancel", endWheelDrag);

    sldTestAngle.addEventListener("input", function () {
        if (!connected) {
            return;
        }

        targetAngle = clampAngle(Number(sldTestAngle.value));
        setMode("hold");
        markDirty();
    });

    btnSimHold.addEventListener("click", function () {
        if (!connected) {
            return;
        }

        targetAngle = clampAngle(Number(sldTestAngle.value));
        setMode("hold");
        setStatus("Wheel holding test angle");
    });

    btnSimAuto.addEventListener("click", function () {
        if (!connected) {
            return;
        }

        setMode("auto");
        setStatus("Wheel running auto sweep");
    });

    btnFfbBump.addEventListener("click", function () {
        if (!connected) {
            return;
        }
        injectRoadEffect(9);
    });

    btnFfbCurb.addEventListener("click", function () {
        if (!connected) {
            return;
        }
        injectRoadEffect(-12);
    });

    function setConnected(state) {
        connected = state;
        setEnabled(state);
        connectionStatus.textContent = state ? "Status: Connected" : "Status: Disconnected";
        firmware.textContent = state ? "Firmware: 1.2.0-emc" : "Firmware: —";

        if (state) {
            setStatus("Connected to " + document.getElementById("port-select").value);
            simulatedAngle = 0;
            simulatedVelocity = 0;
            targetAngle = 0;
            kickTorque = 0;
            ffbLoad = 0;
            setMode("auto");
        } else {
            setMode("manual");
            simulatedAngle = 0;
            simulatedVelocity = 0;
            targetAngle = 0;
            kickTorque = 0;
            ffbLoad = 0;
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
        syncTestAngleBounds();

        dirty = true;
        setTitle();
        setStatus("Defaults restored");
    });

    btnCenter.addEventListener("click", function () {
        if (!connected) {
            return;
        }

        simulatedAngle = 0;
        simulatedVelocity = 0;
        targetAngle = 0;
        setStatus("Center set");
    });

    window.requestAnimationFrame(runSimulator);
    syncTestAngleBounds();
    setConnected(false);
})();
