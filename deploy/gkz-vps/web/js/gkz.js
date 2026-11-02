// MU GKZ: la web pública.
// Regla de seguridad: todo lo que viene del server (nombres de personajes, guilds, noticias) se escribe
// como texto con textContent; nunca con innerHTML. Los escudos de guild se dibujan en un canvas.
"use strict";

(function () {
  const API = "/public/web";

  // ---------- utilidades ----------

  /** Crea un elemento con clase y texto, sin interpretar HTML. */
  function el(tag, clase, texto) {
    const e = document.createElement(tag);
    if (clase) e.className = clase;
    if (texto !== undefined && texto !== null) e.textContent = String(texto);
    return e;
  }

  function vaciar(nodo) {
    while (nodo.firstChild) nodo.removeChild(nodo.firstChild);
  }

  function numero(n) {
    const v = Number(n);
    return Number.isFinite(v) ? v.toLocaleString("es-AR") : "-";
  }

  async function pedir(ruta) {
    const r = await fetch(ruta, { credentials: "omit", cache: "no-cache", headers: { "Accept": "application/json" } });
    if (!r.ok) throw new Error("HTTP " + r.status);
    return r.json();
  }

  function mostrarVacio(nodo, texto) {
    vaciar(nodo);
    nodo.appendChild(el("div", "vacio", texto));
  }

  // Paleta de los escudos de guild de MU (16 colores, el 0 es transparente).
  const PALETA = [
    null, [0, 0, 0], [128, 128, 128], [255, 255, 255], [254, 0, 0], [255, 127, 0], [255, 255, 0], [128, 255, 0],
    [0, 255, 0], [0, 255, 128], [0, 255, 255], [0, 128, 255], [0, 0, 255], [127, 0, 255], [255, 0, 255], [255, 0, 128],
  ];

  /** Dibuja el escudo de una guild (64 dígitos hexadecimales: 8x8 píxeles de 4 bits). */
  function escudo(hex, clase) {
    if (typeof hex !== "string" || !/^[0-9a-fA-F]{64}$/.test(hex)) return document.createTextNode("");
    const c = document.createElement("canvas");
    c.width = 8;
    c.height = 8;
    c.className = clase || "escudo";
    const ctx = c.getContext("2d");
    const img = ctx.createImageData(8, 8);
    for (let i = 0; i < 64; i++) {
      const color = PALETA[parseInt(hex[i], 16)];
      if (!color) continue;
      img.data.set([color[0], color[1], color[2], 255], i * 4);
    }
    ctx.putImageData(img, 0, 0);
    return c;
  }

  // ---------- estado del server ----------

  async function cargarEstado() {
    const caja = document.getElementById("estado");
    if (!caja) return;
    const valor = caja.querySelector(".valor");
    try {
      const d = await pedir(API + "/status");
      const online = d && d.online === true;
      caja.classList.toggle("online", online);
      valor.textContent = online ? numero(d.players) + " online" : "Offline";
    } catch (e) {
      caja.classList.remove("online");
      valor.textContent = "Sin datos";
    }
  }

  // ---------- información del server (sale de la configuración del admin) ----------

  function poner(clave, texto) {
    document.querySelectorAll('[data-info="' + clave + '"]').forEach(function (n) { n.textContent = texto; });
  }

  async function cargarInfo() {
    if (!document.querySelector("[data-info]")) return;
    let d;
    try {
      d = await pedir(API + "/info");
    } catch (e) {
      return; // quedan los valores escritos en la página
    }
    const esNumero = function (v) { return typeof v === "number" && Number.isFinite(v); };
    if (esNumero(d.experience)) {
      poner("experiencia", "x" + numero(d.experience) + (esNumero(d.vipExperience) ? " · VIP x" + numero(d.vipExperience) : ""));
    }
    if (esNumero(d.maxStats)) poner("stats", numero(d.maxStats));
    const r = d.reset;
    if (r) {
      if (esNumero(r.level)) {
        poner("reset-nivel", numero(r.level) + (esNumero(r.levelVip) ? " · VIP " + numero(r.levelVip) : ""));
        poner("reset-nivel-corto", "Nivel " + numero(r.level));
      }
      if (esNumero(r.zen)) poner("reset-costo", numero(r.zen) + " de zen" + (r.zenMultipliedByResets ? " × resets" : ""));
      if (esNumero(r.points)) poner("reset-puntos", numero(r.points) + (esNumero(r.pointsDarkLord) ? " · DL " + numero(r.pointsDarkLord) : ""));
      poner("reset-stats", r.statsBackToZero ? "Vuelven a 0" : "Se mantienen");
      poner("reset-limite", esNumero(r.limit) ? numero(r.limit) : "Sin límite");
    }
    const v = d.vip;
    const lista = document.getElementById("vip-lista");
    if (v && lista) {
      const puntos = [];
      if (esNumero(d.vipExperience) && esNumero(d.experience)) puntos.push("Experiencia x" + numero(d.vipExperience) + " en lugar de x" + numero(d.experience) + ".");
      if (esNumero(v.dropBonusPercent) && v.dropBonusPercent > 0) puntos.push("+" + numero(v.dropBonusPercent) + " % de chance de drop.");
      if (esNumero(v.zenBonusPercent) && v.zenBonusPercent > 0) puntos.push("+" + numero(v.zenBonusPercent) + " % de zen.");
      if (r && esNumero(r.levelVip)) puntos.push("Reset desde el nivel " + numero(r.levelVip) + ".");
      if (esNumero(v.vipVaults) && esNumero(v.vaults)) puntos.push(numero(v.vipVaults) + " baúles en lugar de " + numero(v.vaults) + ".");
      if (v.combination) puntos.push("Combinación de ítems excellent.");
      vaciar(lista);
      puntos.forEach(function (t) { lista.appendChild(el("li", null, t)); });
      const precio = document.getElementById("vip-precio");
      if (precio && esNumero(v.price) && esNumero(v.days)) {
        precio.textContent = "Cuesta " + numero(v.price) + " " + String(v.coinName || "GKZ Coins") + " por " + numero(v.days)
          + " días. Se compra en el juego, desde el botón VIP de la ventana del personaje.";
      }
    }
  }

  // ---------- noticias (las mismas del launcher) ----------

  const IMAGEN_NOTICIA = { evento: "img/noticia-evento.webp", servidor: "img/noticia-servidor.webp", mantenimiento: "img/noticia-servidor.webp" };

  async function cargarNoticias() {
    const lista = document.getElementById("noticias");
    if (!lista) return;
    try {
      const noticias = await pedir("/launcher/news.json");
      vaciar(lista);
      (Array.isArray(noticias) ? noticias : []).slice(0, 3).forEach(function (n) {
        const tarjeta = el("article", "noticia");
        const imagen = el("div", "imagen");
        const etiqueta = String(n.tag || "Novedad");
        imagen.style.backgroundImage = 'url("' + (IMAGEN_NOTICIA[etiqueta.toLowerCase()] || "img/noticia-novedad.webp") + '")';
        tarjeta.appendChild(imagen);
        const datos = el("div", "datos");
        const meta = el("div", "meta");
        meta.appendChild(el("span", "etiqueta", etiqueta));
        meta.appendChild(el("time", null, n.date || ""));
        datos.appendChild(meta);
        datos.appendChild(el("h4", null, n.title || ""));
        datos.appendChild(el("p", null, n.text || ""));
        tarjeta.appendChild(datos);
        lista.appendChild(tarjeta);
      });
      if (!lista.firstChild) mostrarVacio(lista, "Todavía no hay noticias.");
    } catch (e) {
      mostrarVacio(lista, "No se pudieron cargar las noticias.");
    }
  }

  // ---------- eventos con cuenta regresiva ----------

  let eventos = [];
  let desfase = 0; // diferencia entre el reloj del server y el del navegador

  function falta(ms) {
    if (ms <= 0) return "¡Ahora!";
    const s = Math.floor(ms / 1000);
    const d = Math.floor(s / 86400), h = Math.floor((s % 86400) / 3600), m = Math.floor((s % 3600) / 60), seg = s % 60;
    if (d > 0) return d + "d " + h + "h " + m + "m";
    if (h > 0) return h + "h " + m + "m " + seg + "s";
    return m + "m " + seg + "s";
  }

  function pintarEventos() {
    const lista = document.getElementById("lista-eventos");
    if (!lista) return;
    const ahora = Date.now() + desfase;
    eventos.forEach(function (ev) {
      const ms = ev.inicio - ahora;
      ev.li.classList.toggle("activo", ev.activo || ms <= 0);
      ev.cuando.textContent = ev.activo ? "En curso" : falta(ms);
    });
  }

  async function cargarEventos() {
    const lista = document.getElementById("lista-eventos");
    if (!lista) return;
    try {
      const d = await pedir(API + "/events");
      const ahoraServer = Date.parse(d.now);
      if (Number.isFinite(ahoraServer)) desfase = ahoraServer - Date.now();
      vaciar(lista);
      eventos = (Array.isArray(d.events) ? d.events : [])
        .map(function (ev) { return { nombre: String(ev.name || ""), inicio: Date.parse(ev.next), activo: ev.active === true }; })
        .filter(function (ev) { return ev.activo || Number.isFinite(ev.inicio); })
        .sort(function (a, b) { return (a.activo ? 0 : a.inicio) - (b.activo ? 0 : b.inicio); });
      eventos.forEach(function (ev) {
        ev.li = el("li");
        ev.li.appendChild(el("span", "nombre", ev.nombre));
        ev.cuando = el("span", "cuando");
        ev.li.appendChild(ev.cuando);
        lista.appendChild(ev.li);
      });
      if (!eventos.length) mostrarVacio(lista, "Sin eventos programados.");
      pintarEventos();
    } catch (e) {
      mostrarVacio(lista, "No se pudieron cargar los eventos.");
    }
  }

  // ---------- apertura: cuenta regresiva y botón para jugar en el navegador ----------

  // Miércoles 4/11/2026 a las 19 h de Argentina (UTC-3).
  const APERTURA = Date.parse("2026-11-04T22:00:00Z");
  const JUEGO_WEB = "https://jugar.mugkz.com/";

  function dosCifras(n) {
    return (n < 10 ? "0" : "") + n;
  }

  function pintarApertura() {
    const reloj = document.getElementById("apertura-reloj");
    if (!reloj) return;
    const ms = APERTURA - (Date.now() + desfase);
    if (ms > 0) {
      const s = Math.floor(ms / 1000);
      const partes = { d: Math.floor(s / 86400), h: Math.floor((s % 86400) / 3600), m: Math.floor((s % 3600) / 60), s: s % 60 };
      Object.keys(partes).forEach(function (k) {
        reloj.querySelector('[data-reloj="' + k + '"]').textContent = k === "d" ? String(partes[k]) : dosCifras(partes[k]);
      });
      return;
    }
    // Ya abrimos: el botón lleva al juego (la dirección no aparece en la página antes).
    const boton = document.getElementById("boton-jugar");
    if (boton.getAttribute("aria-disabled") !== "true") return;
    reloj.hidden = true;
    document.getElementById("apertura-rotulo").textContent = "¡Ya abrimos!";
    document.getElementById("apertura-titulo").textContent = "Season 3 · Resets · Castle Siege los domingos";
    document.getElementById("boton-jugar-nota").textContent = "Desde Chrome o Edge en tu PC";
    boton.href = JUEGO_WEB;
    boton.removeAttribute("aria-disabled");
    boton.removeAttribute("role");
  }

  // ---------- Castle Siege y destacados ----------

  function llenarGuild(nodo, guild, sinDatos) {
    vaciar(nodo);
    if (!guild) {
      nodo.appendChild(el("span", null, sinDatos));
      return;
    }
    nodo.appendChild(escudo(guild.mark));
    const texto = el("div");
    texto.appendChild(el("div", "rotulo", "Guild dominante"));
    texto.appendChild(el("b", null, guild.name));
    if (guild.master) texto.appendChild(el("span", null, "Master: " + guild.master));
    nodo.appendChild(texto);
  }

  function fecha(iso) {
    const t = Date.parse(iso);
    if (!Number.isFinite(t)) return "A definir";
    return new Date(t).toLocaleString("es-AR", { weekday: "long", day: "numeric", month: "numeric", hour: "2-digit", minute: "2-digit" });
  }

  async function cargarCastillo() {
    const nodo = document.getElementById("castillo-guild");
    if (!nodo) return;
    try {
      const d = await pedir(API + "/castle");
      llenarGuild(nodo, d.owner, "El castillo no tiene dueño todavía.");
      const inicio = Date.parse(d.nextSiege);
      document.getElementById("castillo-proxima").textContent =
        Number.isFinite(inicio) && inicio <= Date.now() + desfase ? "¡En curso!" : fecha(d.nextSiege);
    } catch (e) {
      llenarGuild(nodo, null, "Sin datos.");
    }
  }

  async function cargarDestacados() {
    const guild = document.getElementById("top-guild");
    const killer = document.getElementById("top-killer");
    if (!guild && !killer) return;
    try {
      const g = await pedir(API + "/rankings/guilds");
      const primero = g.entries && g.entries[0];
      llenarGuild(guild, primero, "Todavía no hay guilds.");
      document.getElementById("top-guild-puntos").textContent = primero ? numero(primero.score) : "-";
    } catch (e) {
      llenarGuild(guild, null, "Sin datos.");
    }
    try {
      const k = await pedir(API + "/rankings/killers");
      const primero = k.entries && k.entries[0];
      vaciar(killer);
      if (primero) {
        const texto = el("div");
        texto.appendChild(el("div", "rotulo", "El más letal"));
        texto.appendChild(el("b", null, primero.name));
        texto.appendChild(el("span", null, primero.class || ""));
        killer.appendChild(texto);
      } else {
        killer.appendChild(el("span", null, "Todavía nadie."));
      }
      document.getElementById("top-killer-kills").textContent = primero ? numero(primero.kills) : "0";
    } catch (e) {
      vaciar(killer);
      killer.appendChild(el("span", null, "Sin datos."));
    }
  }

  // ---------- rankings ----------

  const RANKINGS = {
    resets: { titulo: "Resets", valor: function (e) { return numero(e.resets) + " resets"; }, columnas: ["Personaje", "Clase", "Guild", "Nivel", "Resets"],
      fila: function (e) { return [e.name, e.class, e.guild || "-", numero(e.level), numero(e.resets)]; } },
    level: { titulo: "Nivel", valor: function (e) { return "Nivel " + numero(e.level); }, columnas: ["Personaje", "Clase", "Guild", "Resets", "Nivel"],
      fila: function (e) { return [e.name, e.class, e.guild || "-", numero(e.resets), numero(e.level)]; } },
    killers: { titulo: "Killers", valor: function (e) { return numero(e.kills) + " kills"; }, columnas: ["Personaje", "Clase", "Guild", "Resets", "Kills"],
      fila: function (e) { return [e.name, e.class, e.guild || "-", numero(e.resets), numero(e.kills)]; } },
    guilds: { titulo: "Guilds", valor: function (e) { return numero(e.score) + " puntos"; }, columnas: ["Guild", "Master", "Miembros", "Puntos"], guild: true,
      fila: function (e) { return [e.name, e.master || "-", numero(e.members), numero(e.score)]; } },
  };

  async function mostrarRanking(tipo, contenedor, cantidad) {
    const def = RANKINGS[tipo];
    if (!def) return;
    mostrarVacio(contenedor, "Cargando…");
    let entradas;
    try {
      const d = await pedir(API + "/rankings/" + tipo);
      entradas = Array.isArray(d.entries) ? d.entries.slice(0, cantidad) : [];
    } catch (e) {
      mostrarVacio(contenedor, "No se pudo cargar el ranking.");
      return;
    }
    vaciar(contenedor);
    if (!entradas.length) {
      mostrarVacio(contenedor, "Todavía no hay nadie en este ranking.");
      return;
    }

    // Podio: 2º, 1º, 3º
    const podio = el("div", "podio");
    [1, 0, 2].forEach(function (i) {
      const e = entradas[i];
      if (!e) { podio.appendChild(el("div")); return; }
      const p = el("div", "puesto p" + (i + 1));
      p.appendChild(el("span", "lugar", (i + 1) + "º"));
      if (def.guild) p.appendChild(escudo(e.mark));
      p.appendChild(el("div", "clase", def.guild ? "Master: " + (e.master || "-") : (e.class || "")));
      p.appendChild(el("b", null, e.name));
      p.appendChild(el("div", "valor", def.valor(e)));
      podio.appendChild(p);
    });
    contenedor.appendChild(podio);

    // Resto en tabla
    if (entradas.length > 3) {
      const tabla = el("table", "tabla");
      const cabeza = el("tr");
      cabeza.appendChild(el("th", "pos", "#"));
      def.columnas.forEach(function (c, i) {
        cabeza.appendChild(el("th", (i === def.columnas.length - 1 ? "num" : "") + (i === 2 ? " ocultar-celular" : ""), c));
      });
      const thead = el("thead");
      thead.appendChild(cabeza);
      tabla.appendChild(thead);
      const cuerpo = el("tbody");
      entradas.slice(3).forEach(function (e, j) {
        const tr = el("tr");
        tr.appendChild(el("td", "pos", j + 4));
        def.fila(e).forEach(function (v, i) {
          const td = el("td", (i === def.columnas.length - 1 ? "num" : "") + (i === 2 ? " ocultar-celular" : ""));
          if (def.guild && i === 0) td.appendChild(escudo(e.mark));
          td.appendChild(document.createTextNode(String(v)));
          tr.appendChild(td);
        });
        cuerpo.appendChild(tr);
      });
      tabla.appendChild(cuerpo);
      contenedor.appendChild(tabla);
    }
  }

  function prepararPestanas() {
    const pestanas = document.getElementById("pestanas");
    const contenedor = document.getElementById("ranking");
    if (!pestanas || !contenedor) return;
    const cantidad = Number(contenedor.dataset.cantidad) || 50;
    const botones = Array.prototype.slice.call(pestanas.querySelectorAll("button[data-tipo]"));
    function elegir(tipo) {
      if (!RANKINGS[tipo]) tipo = "resets";
      botones.forEach(function (b) { b.classList.toggle("activo", b.dataset.tipo === tipo); });
      mostrarRanking(tipo, contenedor, cantidad);
    }
    botones.forEach(function (b) {
      b.addEventListener("click", function () {
        elegir(b.dataset.tipo);
        if (pestanas.dataset.historial === "si") history.replaceState(null, "", "#" + b.dataset.tipo);
      });
    });
    elegir(pestanas.dataset.historial === "si" ? location.hash.slice(1) : "resets");
  }

  // ---------- registro ----------

  function prepararRegistro() {
    const form = document.getElementById("registro");
    if (!form) return;
    const mensaje = document.getElementById("registro-mensaje");
    const boton = form.querySelector("button[type=submit]");
    const captcha = document.getElementById("registro-captcha");
    let captchaId = null;
    let captchaToken = "";

    // Captcha de Cloudflare Turnstile. La clave del sitio es pública; la secreta solo la tiene el servidor,
    // que verifica cada token con Cloudflare antes de crear la cuenta.
    if (captcha && window.turnstile) {
      captchaId = window.turnstile.render(captcha, {
        sitekey: captcha.dataset.sitekey,
        action: "registro",
        language: "es",
        theme: "dark",
        callback: function (token) { captchaToken = token; },
        "expired-callback": function () { captchaToken = ""; },
        "error-callback": function () { captchaToken = ""; },
      });
    }

    function reiniciarCaptcha() {
      captchaToken = "";
      if (captchaId !== null && window.turnstile) window.turnstile.reset(captchaId);
    }

    function avisar(tipo, texto) {
      mensaje.className = "mensaje " + tipo;
      mensaje.textContent = texto;
    }

    form.addEventListener("submit", async function (ev) {
      ev.preventDefault();
      const usuario = form.usuario.value.trim();
      const clave = form.clave.value;
      const clave2 = form.clave2.value;
      const email = form.email.value.trim();
      if (!/^[A-Za-z0-9]{4,10}$/.test(usuario)) return avisar("error", "El usuario debe tener de 4 a 10 letras o números.");
      if (!/^[\x21-\x7E]{6,20}$/.test(clave)) return avisar("error", "La contraseña debe tener de 6 a 20 caracteres, sin espacios ni acentos.");
      if (clave !== clave2) return avisar("error", "Las contraseñas no coinciden.");
      if (email && !/^[^\s@<>"']{1,64}@[^\s@<>"']{1,190}\.[A-Za-z]{2,}$/.test(email)) return avisar("error", "El e-mail no es válido.");
      if (!captchaToken) return avisar("error", captchaId === null
        ? "No se pudo cargar la verificación anti-bots. Recargá la página."
        : "Esperá a que termine la verificación anti-bots.");

      boton.disabled = true;
      try {
        const r = await fetch("/public/register", {
          method: "POST",
          credentials: "omit",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ loginName: usuario, password: clave, eMail: email || null, captchaToken: captchaToken }),
        });
        if (r.status === 201) {
          form.reset();
          avisar("ok", "¡Cuenta creada! Ya podés entrar al juego con tu usuario y contraseña.");
        } else if (r.status === 409) {
          avisar("error", "Ese usuario ya existe. Probá con otro.");
        } else if (r.status === 429 || r.status === 503) {
          avisar("error", "Demasiados intentos. Esperá un minuto y probá de nuevo.");
        } else if (r.status === 403) {
          avisar("error", "No pasaste la verificación anti-bots. Probá de nuevo.");
        } else if (r.status === 400) {
          avisar("error", "Revisá los datos: alguno no es válido.");
        } else {
          avisar("error", "No se pudo crear la cuenta. Probá más tarde.");
        }
      } catch (e) {
        avisar("error", "No hay conexión con el servidor.");
      } finally {
        // Cada token sirve una sola vez.
        reiniciarCaptcha();
        boton.disabled = false;
      }
    });
  }

  // ---------- panel del jugador ----------

  // Pide algo al panel. La sesión es una cookie que el navegador manda solo a /public/panel;
  // la cabecera X-GKZ-Panel no la puede mandar otro sitio, así que nadie actúa en nombre del jugador.
  async function panelApi(ruta, datos) {
    const opciones = {
      method: datos ? "POST" : "GET",
      credentials: "same-origin",
      cache: "no-store",
      headers: { "Accept": "application/json", "X-GKZ-Panel": "1" },
    };
    if (datos) {
      opciones.headers["Content-Type"] = "application/json";
      opciones.body = JSON.stringify(datos);
    }
    const r = await fetch("/public/panel/" + ruta, opciones);
    let cuerpo = null;
    try { cuerpo = await r.json(); } catch (e) { cuerpo = null; }
    return { estado: r.status, datos: cuerpo };
  }

  // El texto de error que manda el servidor, o uno general según el código.
  function textoError(res) {
    if (res.datos && typeof res.datos.error === "string") return res.datos.error;
    if (res.estado === 429) return "Demasiados intentos. Esperá un minuto y probá de nuevo.";
    return "No se pudo hacer. Probá más tarde.";
  }

  function fechaCorta(iso) {
    const d = new Date(iso);
    return isNaN(d) ? "-" : d.toLocaleDateString("es-AR", { day: "numeric", month: "short", year: "numeric" });
  }

  function prepararPanel() {
    const raiz = document.getElementById("panel");
    if (!raiz) return;
    const vistas = ["ingreso", "olvido", "nueva", "cuenta"];
    const captchas = {};
    let tokenRecuperar = "";

    function avisar(nodo, tipo, texto) {
      nodo.className = "mensaje " + tipo;
      nodo.textContent = texto;
    }

    function limpiar(form) {
      const m = form.querySelector(".mensaje");
      m.className = "mensaje";
      m.textContent = "";
    }

    // Un captcha por formulario, creado cuando el formulario se ve.
    function captcha(nombre) {
      const nodo = document.getElementById("captcha-" + nombre);
      if (!nodo) return null;
      if (!captchas[nombre]) {
        captchas[nombre] = { id: null, token: "" };
        if (window.turnstile) {
          captchas[nombre].id = window.turnstile.render(nodo, {
            sitekey: raiz.dataset.sitekey,
            action: "panel",
            language: "es",
            theme: "dark",
            callback: function (t) { captchas[nombre].token = t; },
            "expired-callback": function () { captchas[nombre].token = ""; },
            "error-callback": function () { captchas[nombre].token = ""; },
          });
        }
      }
      return captchas[nombre];
    }

    function reiniciarCaptcha(nombre) {
      const c = captchas[nombre];
      if (!c) return;
      c.token = "";
      if (c.id !== null && window.turnstile) window.turnstile.reset(c.id);
    }

    function faltaCaptcha(c) {
      if (c && c.token) return null;
      return c && c.id !== null
        ? "Esperá a que termine la verificación anti-bots."
        : "No se pudo cargar la verificación anti-bots. Recargá la página.";
    }

    function mostrar(vista) {
      vistas.forEach(function (v) { document.getElementById("vista-" + v).hidden = v !== vista; });
      document.getElementById("panel-subtitulo").textContent = vista === "cuenta"
        ? "Tu cuenta, tus personajes, tu VIP y tus GKZ Coins."
        : "Entrá con el usuario y la contraseña del juego.";
      if (vista === "ingreso" || vista === "olvido") captcha(vista);
    }

    function pintarCuenta(d) {
      document.getElementById("cuenta-nombre").textContent = d.loginName;
      document.getElementById("cuenta-registro").textContent = fechaCorta(d.registered);
      const estado = document.getElementById("cuenta-estado");
      estado.classList.toggle("online", d.online === true);
      estado.lastElementChild.textContent = d.online ? "Conectada al juego" : "Fuera del juego";
      document.getElementById("cuenta-coins").textContent = numero(d.coins);
      document.getElementById("cuenta-coins-nota").textContent = d.pendingCoins > 0
        ? numero(d.pendingCoins) + " se acreditan al abrir la tienda X"
        : "Para el VIP y la tienda X";
      const vip = d.vipUntil ? new Date(d.vipUntil) : null;
      document.getElementById("cuenta-vip").textContent = vip ? "Activo" : "Sin VIP";
      document.getElementById("cuenta-vip-detalle").textContent = vip
        ? "Hasta el " + fechaCorta(d.vipUntil) + " (" + Math.max(0, Math.ceil((vip - Date.now()) / 86400000)) + " días)"
        : "Se compra en el juego con 1.000 GKZ Coins";
      document.getElementById("cuenta-email").textContent = d.email || "sin correo";

      const aviso = document.getElementById("aviso-conectado");
      if (d.online) avisar(aviso, "error", "Tu cuenta está conectada al juego. Para destrabar un personaje o cambiar datos, salí del juego.");
      else { aviso.className = "mensaje"; aviso.textContent = ""; }

      pintarRecarga(d);

      const tbody = document.getElementById("personajes");
      vaciar(tbody);
      (Array.isArray(d.characters) ? d.characters : []).forEach(function (c) {
        const tr = el("tr");
        const nombre = el("td");
        nombre.appendChild(el("b", null, c.name));
        if (c.online) nombre.appendChild(el("span", "en-juego", " · en el juego"));
        tr.appendChild(nombre);
        tr.appendChild(el("td", null, c.class || "-"));
        tr.appendChild(el("td", "num", numero(c.level)));
        tr.appendChild(el("td", "num", numero(c.resets)));
        tr.appendChild(el("td", "ocultar-celular", c.map || "-"));
        const acciones = el("td", "num");
        const b = el("button", "boton-borde", "Destrabar");
        b.type = "button";
        b.disabled = d.online === true;
        b.addEventListener("click", function () { destrabar(c.name, b); });
        acciones.appendChild(b);
        tr.appendChild(acciones);
        tbody.appendChild(tr);
      });
      if (!tbody.children.length) {
        const tr = el("tr");
        const td = el("td", "vacio", "Todavía no tenés personajes. Crealos en el juego.");
        td.colSpan = 6;
        tr.appendChild(td);
        tbody.appendChild(tr);
      }
    }

    // Los packs a la venta y el historial de compras.
    function pintarRecarga(d) {
      const packs = document.getElementById("packs");
      vaciar(packs);
      const lista = Array.isArray(d.packs) ? d.packs : [];
      lista.forEach(function (p, i) {
        const caja = el("article", "pack" + (i === lista.length - 2 ? " destacado" : ""));
        caja.appendChild(el("small", null, p.name));
        caja.appendChild(el("span", "coins", numero(p.coins + p.bonus)));
        caja.appendChild(el("span", "regalo", p.bonus > 0 ? "incluye " + numero(p.bonus) + " de regalo" : "GKZ Coins"));
        caja.appendChild(el("span", "precio", "USD " + p.price));
        const b = el("button", "boton", "Comprar");
        b.type = "button";
        b.addEventListener("click", function () { comprar(p, b); });
        caja.appendChild(b);
        packs.appendChild(caja);
      });

      const tbody = document.getElementById("compras");
      vaciar(tbody);
      (Array.isArray(d.purchases) ? d.purchases : []).forEach(function (c) {
        const tr = el("tr");
        tr.appendChild(el("td", null, fechaCorta(c.date)));
        tr.appendChild(el("td", null, c.description || "-"));
        tr.appendChild(el("td", "num" + (c.coins < 0 ? " negativo" : ""), (c.coins > 0 ? "+" : "") + numero(c.coins)));
        tr.appendChild(el("td", "ocultar-celular" + (c.applied ? "" : " pendiente"), c.applied ? "En tu cuenta" : "Al abrir la tienda X"));
        tbody.appendChild(tr);
      });
      if (!tbody.children.length) {
        const tr = el("tr");
        const td = el("td", "vacio", "Todavía no compraste coins.");
        td.colSpan = 4;
        tr.appendChild(td);
        tbody.appendChild(tr);
      }
    }

    // Pide el pago al servidor y va a la página de Mercado Pago.
    async function comprar(pack, boton) {
      const msg = document.getElementById("recarga-mensaje");
      msg.className = "mensaje";
      boton.disabled = true;
      try {
        const res = await panelApi("buy", { pack: pack.id });
        const url = res.datos && res.datos.url;
        if (res.estado === 200 && typeof url === "string" && /^https:\/\/([a-z0-9-]+\.)*mercadopago\.com(\.[a-z]{2})?\//.test(url)) {
          location.href = url;
          return;
        }
        if (res.estado === 401) mostrar("ingreso");
        else avisar(msg, "error", textoError(res));
      } catch (e) {
        avisar(msg, "error", "No hay conexión con el servidor.");
      }
      boton.disabled = false;
    }

    // Al volver de Mercado Pago, la dirección trae el pago: se consulta enseguida para no esperar el aviso.
    async function volverDelPago() {
      const q = new URLSearchParams(location.search);
      const resultado = q.get("pago");
      if (!resultado) return;
      const pago = q.get("payment_id") || q.get("collection_id") || "";
      history.replaceState(null, "", location.pathname + "#recargar");
      const msg = document.getElementById("recarga-mensaje");
      if (!/^\d{1,20}$/.test(pago)) {
        if (resultado === "error") avisar(msg, "error", "El pago no se completó. No se te cobró nada; podés probar de nuevo.");
        return;
      }
      try {
        const res = await panelApi("confirm", { paymentId: pago });
        const d = res.datos || {};
        if (res.estado !== 200) avisar(msg, "error", textoError(res));
        else if (d.status === "acreditado") avisar(msg, "ok", "¡Gracias! Se acreditaron " + numero(d.coins) + " GKZ Coins. En el juego las ves al abrir la tienda (tecla X).");
        else if (d.status === "pendiente") avisar(msg, "ok", "Tu pago está pendiente. Cuando Mercado Pago lo apruebe, las coins se acreditan solas.");
        else avisar(msg, "error", "El pago no se aprobó. No se te cobró nada; podés probar de nuevo.");
      } catch (e) {
        avisar(msg, "error", "No pudimos consultar el pago. Si lo pagaste, las coins llegan solas en unos minutos.");
      }
      await cargarCuenta();
      document.getElementById("recargar").scrollIntoView();
    }

    async function cargarCuenta() {
      const res = await panelApi("me");
      if (res.estado === 200 && res.datos) {
        pintarCuenta(res.datos);
        mostrar("cuenta");
        return true;
      }
      mostrar("ingreso");
      return false;
    }

    async function destrabar(nombre, boton) {
      const msg = document.getElementById("personajes-mensaje");
      if (!confirm("¿Llevar a " + nombre + " a Lorencia?")) return;
      boton.disabled = true;
      try {
        const res = await panelApi("unstick", { character: nombre });
        if (res.estado === 204) {
          avisar(msg, "ok", nombre + " ya está en Lorencia.");
          cargarCuenta();
        } else if (res.estado === 401) {
          mostrar("ingreso");
        } else {
          avisar(msg, "error", textoError(res));
        }
      } catch (e) {
        avisar(msg, "error", "No hay conexión con el servidor.");
      } finally {
        boton.disabled = false;
      }
    }

    // Manda un formulario: valida, deshabilita el botón y muestra lo que pasó.
    function alEnviar(id, accion) {
      const form = document.getElementById(id);
      form.addEventListener("submit", async function (ev) {
        ev.preventDefault();
        const msg = form.querySelector(".mensaje");
        const boton = form.querySelector("button[type=submit]");
        limpiar(form);
        boton.disabled = true;
        try {
          await accion(form, function (tipo, texto) { avisar(msg, tipo, texto); });
        } catch (e) {
          avisar(msg, "error", "No hay conexión con el servidor.");
        } finally {
          boton.disabled = false;
        }
      });
    }

    const claveValida = /^[\x21-\x7E]{6,20}$/;

    alEnviar("form-ingreso", async function (form, avisarForm) {
      const usuario = form.usuario.value.trim();
      const clave = form.clave.value;
      if (!usuario || !clave) return avisarForm("error", "Completá el usuario y la contraseña.");
      const c = captcha("ingreso");
      const falta = faltaCaptcha(c);
      if (falta) return avisarForm("error", falta);
      const res = await panelApi("login", { loginName: usuario, password: clave, captchaToken: c.token });
      reiniciarCaptcha("ingreso");
      if (res.estado === 200) {
        form.reset();
        await cargarCuenta();
      } else {
        avisarForm("error", textoError(res));
      }
    });

    alEnviar("form-olvido", async function (form, avisarForm) {
      const usuario = form.usuario.value.trim();
      if (!/^[A-Za-z0-9]{3,10}$/.test(usuario)) return avisarForm("error", "Escribí tu usuario.");
      const c = captcha("olvido");
      const falta = faltaCaptcha(c);
      if (falta) return avisarForm("error", falta);
      const res = await panelApi("recover", { loginName: usuario, captchaToken: c.token });
      reiniciarCaptcha("olvido");
      if (res.estado === 204) {
        form.reset();
        avisarForm("ok", "Si la cuenta tiene un correo, te mandamos un enlace. Revisá también la carpeta de spam. El enlace vale 30 minutos.");
      } else {
        avisarForm("error", textoError(res));
      }
    });

    alEnviar("form-nueva", async function (form, avisarForm) {
      if (!claveValida.test(form.clave.value)) return avisarForm("error", "La contraseña debe tener de 6 a 20 caracteres, sin espacios ni acentos.");
      if (form.clave.value !== form.clave2.value) return avisarForm("error", "Las contraseñas no coinciden.");
      const res = await panelApi("reset", { token: tokenRecuperar, password: form.clave.value });
      if (res.estado === 204) {
        form.reset();
        tokenRecuperar = "";
        avisarForm("ok", "¡Listo! Ya podés entrar con tu contraseña nueva, en el juego y acá.");
      } else {
        avisarForm("error", textoError(res));
      }
    });

    alEnviar("form-clave", async function (form, avisarForm) {
      if (!claveValida.test(form.clave.value)) return avisarForm("error", "La contraseña nueva debe tener de 6 a 20 caracteres, sin espacios ni acentos.");
      if (form.clave.value !== form.clave2.value) return avisarForm("error", "Las contraseñas nuevas no coinciden.");
      const res = await panelApi("password", { currentPassword: form.actual.value, newPassword: form.clave.value });
      if (res.estado === 204) {
        form.reset();
        avisarForm("ok", "Contraseña cambiada. Usala desde ahora también en el juego.");
      } else if (res.estado === 401 && !(res.datos && /actual/.test(res.datos.error || ""))) {
        mostrar("ingreso");
      } else {
        avisarForm("error", textoError(res));
      }
    });

    alEnviar("form-email", async function (form, avisarForm) {
      const email = form.email.value.trim();
      if (!/^[^\s@<>"']{1,64}@[^\s@<>"']{1,190}\.[A-Za-z]{2,}$/.test(email)) return avisarForm("error", "El correo no es válido.");
      const res = await panelApi("email", { password: form.clave.value, eMail: email });
      if (res.estado === 204) {
        form.reset();
        document.getElementById("cuenta-email").textContent = email;
        avisarForm("ok", "Correo cambiado.");
      } else {
        avisarForm("error", textoError(res));
      }
    });

    document.getElementById("salir").addEventListener("click", async function () {
      try { await panelApi("logout", {}); } catch (e) { /* igual se muestra la entrada */ }
      mostrar("ingreso");
    });

    raiz.querySelectorAll("[data-ir]").forEach(function (a) {
      a.addEventListener("click", function (ev) {
        ev.preventDefault();
        mostrar(a.dataset.ir);
      });
    });

    // El enlace del correo trae el token después de # (no llega a ningún servidor ni queda en los registros);
    // se saca de la dirección enseguida para que no quede en el historial.
    const m = /^#recuperar=([A-Za-z0-9_-]{20,100})$/.exec(location.hash);
    if (m) {
      tokenRecuperar = m[1];
      history.replaceState(null, "", location.pathname);
      mostrar("nueva");
      return;
    }
    cargarCuenta()
      .then(function (dentro) { if (dentro) return volverDelPago(); })
      .catch(function () { mostrar("ingreso"); });
  }

  // ---------- arranque ----------

  document.addEventListener("DOMContentLoaded", function () {
    const pagina = document.body.dataset.pagina;
    document.querySelectorAll(".menu a[data-pagina]").forEach(function (a) {
      a.classList.toggle("activo", a.dataset.pagina === pagina);
    });
    const anio = document.getElementById("anio");
    if (anio) anio.textContent = String(new Date().getFullYear());

    cargarEstado();
    cargarNoticias();
    cargarInfo();
    cargarEventos();
    cargarCastillo();
    cargarDestacados();
    prepararPestanas();
    prepararRegistro();
    prepararPanel();

    pintarApertura();
    setInterval(pintarEventos, 1000);
    setInterval(pintarApertura, 1000);
    setInterval(cargarEventos, 5 * 60 * 1000);
    setInterval(cargarEstado, 60 * 1000);
  });
})();
