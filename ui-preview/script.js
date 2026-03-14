/* ============================================================
   FFB Wheel Config — UI Preview Script
   Simulates live angle updates and links sliders ↔ number inputs.
   No real serial connection; purely for visual testing.
   ============================================================ */

(function () {
    "use strict";

    // ── Element references ─────────────────────────────────
    const angleValue  = document.getElementById("angle-value");
    const rawCounts   = document.getElementById("raw-counts");
    const statusDot   = document.getElementById("status-dot");
    const statusText  = document.getElementById("status-text");
    const lblStatus   = document.getElementById("lbl-status");

    const btnConnect    = document.getElementById("btn-connect");
    const btnDisconnect = document.getElementById("btn-disconnect");
    const btnRead       = document.getElementById("btn-read");
    const btnApply      = document.getElementById("btn-apply");
    const btnSave       = document.getElementById("btn-save");
    const btnReset      = document.getElementById("btn-reset");
    const btnCenter     = document.getElementById("btn-center");

    // ── Link each slider ↔ number input pair ───────────────
    const pairs = [
        ["sld-range",     "nud-range"],
        ["sld-force",     "nud-force"],
        ["sld-min-force", "nud-min-force"],
        ["sld-damping",   "nud-damping"],
        ["sld-friction",  "nud-friction"],
        ["sld-spring",    "nud-spring"]
    ];

    pairs.forEach(function (pair) {
        var slider = document.getElementById(pair[0]);
        var nud    = document.getElementById(pair[1]);

        slider.addEventListener("input", function () { nud.value = slider.value; });
        nud.addEventListener("input", function () { slider.value = nud.value; });
    });

    // ── Simulated connection state ─────────────────────────
    var connected = false;
    var angleInterval = null;
    var simulatedAngle = 0;
    var angleDirection = 1;

    function setConnected(state) {
        connected = state;
        btnConnect.disabled    = state;
        btnDisconnect.disabled = !state;
        statusDot.classList.toggle("connected", state);
        statusText.textContent = state ? "Connected" : "Disconnected";

        if (state) {
            showStatus("Connected to " + document.getElementById("port-select").value);
            startAngleSimulation();
        } else {
            showStatus("Disconnected");
            stopAngleSimulation();
            angleValue.textContent = "0";
            rawCounts.textContent  = "Raw: 0";
        }
    }

    btnConnect.addEventListener("click", function () { setConnected(true); });
    btnDisconnect.addEventListener("click", function () { setConnected(false); });

    // ── Simulated live angle ───────────────────────────────
    function startAngleSimulation() {
        simulatedAngle = 0;
        angleDirection  = 1;
        angleInterval = setInterval(function () {
            var range = parseInt(document.getElementById("sld-range").value, 10);
            var half  = range / 2;

            simulatedAngle += angleDirection * (Math.random() * 4 + 1);

            if (simulatedAngle >=  half) { simulatedAngle =  half; angleDirection = -1; }
            if (simulatedAngle <= -half) { simulatedAngle = -half; angleDirection =  1; }

            var display = Math.round(simulatedAngle);
            var raw     = Math.round(simulatedAngle * 2400 / 360);

            angleValue.textContent = display;
            rawCounts.textContent  = "Raw: " + raw;
        }, 100);
    }

    function stopAngleSimulation() {
        if (angleInterval) {
            clearInterval(angleInterval);
            angleInterval = null;
        }
    }

    // ── Button actions (simulated) ─────────────────────────
    function showStatus(msg) {
        lblStatus.textContent = msg;
    }

    btnRead.addEventListener("click", function () {
        showStatus(connected ? "Settings read from wheel" : "Not connected");
    });

    btnApply.addEventListener("click", function () {
        showStatus(connected ? "Settings applied" : "Not connected");
    });

    btnSave.addEventListener("click", function () {
        showStatus(connected ? "Saved to EEPROM" : "Not connected");
    });

    btnReset.addEventListener("click", function () {
        if (!connected) { showStatus("Not connected"); return; }

        document.getElementById("sld-force").value     = 60;  document.getElementById("nud-force").value     = 60;
        document.getElementById("sld-min-force").value  = 5;   document.getElementById("nud-min-force").value  = 5;
        document.getElementById("sld-damping").value    = 10;  document.getElementById("nud-damping").value    = 10;
        document.getElementById("sld-friction").value   = 4;   document.getElementById("nud-friction").value   = 4;
        document.getElementById("sld-spring").value     = 15;  document.getElementById("nud-spring").value     = 15;
        document.getElementById("sld-range").value      = 900; document.getElementById("nud-range").value      = 900;
        document.getElementById("chk-inv-encoder").checked = false;
        document.getElementById("chk-inv-motor").checked   = false;

        showStatus("Defaults restored");
    });

    btnCenter.addEventListener("click", function () {
        showStatus(connected ? "Centre set" : "Not connected");
    });
})();
