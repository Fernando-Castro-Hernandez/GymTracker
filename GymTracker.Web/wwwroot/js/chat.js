// ============================================================================
// Chatbot con contexto (Integración 4) — lógica del widget flotante.
// Vanilla JS (sin jQuery). Habla con /api/chat/* vía fetch (cookie de sesión).
// El markup lo pone _ChatWidget.cshtml; el estilo, site.css.
// ============================================================================
(function () {
    "use strict";

    const raiz = document.getElementById("gt-chat");
    if (!raiz) return; // no autenticado / widget no presente

    const toggle = document.getElementById("gt-chat-toggle");
    const panel = document.getElementById("gt-chat-panel");
    const mensajes = document.getElementById("gt-chat-messages");
    const form = document.getElementById("gt-chat-form");
    const input = document.getElementById("gt-chat-input");
    const btnEnviar = document.getElementById("gt-chat-send");
    const btnLimpiar = document.getElementById("gt-chat-clear");

    let historialCargado = false;
    let enviando = false;

    // ---- Utilidades ----------------------------------------------------------

    // Escapa HTML para evitar inyección al renderizar el contenido del mensaje.
    function escapar(texto) {
        const div = document.createElement("div");
        div.textContent = texto;
        return div.innerHTML;
    }

    // Añade una burbuja al hilo. tipo: "usuario" | "asistente" | "error".
    function agregarMensaje(texto, tipo) {
        const burbuja = document.createElement("div");
        burbuja.className = "gt-msg gt-msg-" + tipo;
        burbuja.innerHTML = escapar(texto).replace(/\n/g, "<br>");
        mensajes.appendChild(burbuja);
        mensajes.scrollTop = mensajes.scrollHeight;
        return burbuja;
    }

    // Indicador de "escribiendo…" mientras esperamos al modelo.
    function mostrarSpinner() {
        const s = document.createElement("div");
        s.className = "gt-msg gt-msg-asistente gt-typing";
        s.innerHTML = "<span></span><span></span><span></span>";
        mensajes.appendChild(s);
        mensajes.scrollTop = mensajes.scrollHeight;
        return s;
    }

    function mensajeBienvenida() {
        if (mensajes.children.length === 0) {
            agregarMensaje(
                "¡Hola! Soy tu asistente de GymTracker. Puedo responder sobre tus " +
                "rutinas, sesiones, volumen y progreso. ¿En qué te ayudo?",
                "asistente");
        }
    }

    // ---- Carga del historial -------------------------------------------------

    async function cargarHistorial() {
        if (historialCargado) return;
        historialCargado = true;
        try {
            const resp = await fetch("/api/chat/historial", { credentials: "same-origin" });
            if (resp.ok) {
                const data = await resp.json();
                data.forEach(m => agregarMensaje(m.contenido, m.esDelUsuario ? "usuario" : "asistente"));
            }
        } catch (e) {
            // Silencioso: si falla, solo mostramos la bienvenida.
        }
        mensajeBienvenida();
    }

    // ---- Envío de un mensaje -------------------------------------------------

    async function enviarMensaje(texto) {
        if (enviando) return;
        enviando = true;
        btnEnviar.disabled = true;

        agregarMensaje(texto, "usuario");
        const spinner = mostrarSpinner();

        try {
            const resp = await fetch("/api/chat/mensaje", {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ mensaje: texto })
            });

            spinner.remove();

            if (resp.status === 429) {
                const err = await resp.json().catch(() => ({}));
                agregarMensaje(err.error || "Vas muy rápido. Espera un momento.", "error");
            } else if (!resp.ok) {
                const err = await resp.json().catch(() => ({}));
                agregarMensaje(err.error || "El asistente no está disponible ahora mismo.", "error");
            } else {
                const data = await resp.json();
                // Rechazado por el guardarriel o respuesta normal: mismo render,
                // el estilo de error solo si el servidor lo marcó como rechazo.
                agregarMensaje(data.contenido, data.rechazado ? "error" : "asistente");
            }
        } catch (e) {
            spinner.remove();
            agregarMensaje("No pude contactar al asistente. Revisa tu conexión.", "error");
        } finally {
            enviando = false;
            btnEnviar.disabled = false;
            input.focus();
        }
    }

    // ---- Limpiar conversación ------------------------------------------------

    async function limpiar() {
        if (!confirm("¿Borrar toda la conversación?")) return;
        try {
            const resp = await fetch("/api/chat/historial", {
                method: "DELETE",
                credentials: "same-origin"
            });
            if (resp.ok) {
                mensajes.innerHTML = "";
                mensajeBienvenida();
            }
        } catch (e) {
            agregarMensaje("No se pudo limpiar la conversación.", "error");
        }
    }

    // ---- Eventos -------------------------------------------------------------

    toggle.addEventListener("click", function () {
        const abierto = raiz.classList.toggle("gt-chat-abierto");
        panel.hidden = !abierto;
        if (abierto) {
            cargarHistorial();
            input.focus();
        }
    });

    form.addEventListener("submit", function (e) {
        e.preventDefault();
        const texto = input.value.trim();
        if (!texto) return;
        input.value = "";
        ajustarAltura();
        enviarMensaje(texto);
    });

    // Enter envía; Shift+Enter hace salto de línea.
    input.addEventListener("keydown", function (e) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            form.requestSubmit();
        }
    });

    // Auto-crecer del textarea hasta un máximo.
    function ajustarAltura() {
        input.style.height = "auto";
        input.style.height = Math.min(input.scrollHeight, 120) + "px";
    }
    input.addEventListener("input", ajustarAltura);

    btnLimpiar.addEventListener("click", limpiar);
})();
