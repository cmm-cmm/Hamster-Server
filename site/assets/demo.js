// Hamster Server website: the live control-panel demo in the hero.
// It replays what the real app shows when Start All is pressed — statuses go grey → amber →
// green and the log fills in — then the tray dot, the peeking hamster and a localhost card light
// up. No network, no storage. Without JavaScript, or with reduced motion, the page shows the real
// screenshot instead (see .no-js / .demo-static in site.css).
(function () {
  "use strict";

  var root = document.documentElement;
  root.classList.remove("no-js");
  root.classList.add("js");

  var demo = document.querySelector("[data-demo]");
  if (!demo) return;
  if (window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
    root.classList.add("demo-static");
    return;
  }

  var win = demo.querySelector(".win");
  var log = demo.querySelector(".log");
  var startBtn = demo.querySelector("[data-action='start']");
  var stopBtn = demo.querySelector("[data-action='stop']");
  var status = demo.querySelector(".gs-text");
  var rows = {
    apache: demo.querySelector("[data-svc='apache']"),
    php: demo.querySelector("[data-svc='php']")
  };
  var timers = [];
  var busy = false;

  function now() {
    var d = new Date();
    return [d.getHours(), d.getMinutes(), d.getSeconds()]
      .map(function (n) { return (n < 10 ? "0" : "") + n; }).join(":");
  }

  // parts: [text, className?] pairs so nothing is parsed as HTML.
  function line(parts) {
    var el = document.createElement("span");
    el.className = "line";
    var t = document.createElement("span");
    t.className = "t";
    t.textContent = "[" + now() + "] ";
    el.appendChild(t);
    parts.forEach(function (p) {
      var s = document.createElement("span");
      if (p[1]) s.className = p[1];
      s.textContent = p[0];
      el.appendChild(s);
    });
    log.appendChild(el);
    while (log.children.length > 9) log.removeChild(log.firstChild);
  }

  function setRow(name, state, label, pid) {
    var row = rows[name];
    row.setAttribute("data-state", state);
    row.querySelector(".st").textContent = label;
    row.querySelector(".pid").textContent = pid || "—";
  }

  function setWin(state, label) {
    win.setAttribute("data-state", state);
    status.textContent = label;
  }

  function at(ms, fn) { timers.push(setTimeout(fn, ms)); }
  function pid() { return String(2000 + Math.floor(Math.random() * 30000)); }

  function start() {
    if (busy) return;
    busy = true;
    startBtn.disabled = true;
    stopBtn.disabled = true;
    setWin("starting", "Starting…");
    setRow("php", "starting", "Starting…");
    setRow("apache", "starting", "Starting…");
    line([["Starting PHP 8.5.12-1 FastCGI on "], ["127.0.0.1:9000", "lnk"], ["…"]]);
    at(350, function () { line([["Generating Apache configuration…"]]); });
    at(700, function () { line([["Testing Apache config…"]]); });
    at(1000, function () { line([["Config test: Syntax OK"]]); });
    at(1300, function () { line([["Starting Apache…"]]); });
    at(1750, function () {
      setRow("php", "running", "Running", pid());
      line([["[OK]", "ok"], [" PHP 8.5.12-1 FastCGI started (Workers: 8, MaxReq: 0)"]]);
    });
    at(2150, function () {
      setRow("apache", "running", "Running", pid());
      line([["[OK]", "ok"], [" Apache running"]]);
    });
    at(2450, function () {
      line([["[OK]", "ok"], [" Port 80 ready. (Primary HTTP)  → "], ["http://localhost/", "lnk"]]);
      setWin("running", "Running");
      demo.classList.add("on");
      stopBtn.disabled = false;
      busy = false;
    });
  }

  function stop() {
    if (busy) return;
    busy = true;
    startBtn.disabled = true;
    stopBtn.disabled = true;
    demo.classList.remove("on");
    setWin("starting", "Stopping…");
    setRow("apache", "starting", "Stopping…", rows.apache.querySelector(".pid").textContent);
    line([["Stopping Apache…"]]);
    at(450, function () {
      setRow("apache", "stopped", "Stopped");
      setRow("php", "starting", "Stopping…", rows.php.querySelector(".pid").textContent);
      line([["Apache stopped."]]);
    });
    at(850, function () {
      setRow("php", "stopped", "Stopped");
      line([["PHP-CGI stopped."]]);
      setWin("stopped", "Stopped");
      startBtn.disabled = false;
      busy = false;
    });
  }

  startBtn.addEventListener("click", start);
  stopBtn.addEventListener("click", stop);

  // Play once by itself the first time the demo is on screen.
  var played = false;
  function autoplay() {
    if (played) return;
    played = true;
    at(700, start);
  }
  if ("IntersectionObserver" in window) {
    var io = new IntersectionObserver(function (entries) {
      if (entries.some(function (e) { return e.isIntersecting; })) {
        autoplay();
        io.disconnect();
      }
    }, { threshold: 0.4 });
    io.observe(demo);
  } else {
    autoplay();
  }
})();
