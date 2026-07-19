// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ===========================================================================
// Pantalla de carga (Lottie)
// Reproduce la animación del logo en un overlay a pantalla completa y lo retira
// cuando la página terminó de cargar, garantizando un tiempo mínimo visible
// (2B) para que la animación se aprecie aunque la carga sea instantánea.
// Respeta prefers-reduced-motion: en ese caso se oculta de inmediato, sin
// animar. Puramente visual; no toca lógica de negocio.
// ===========================================================================
(function () {
    var loader = document.getElementById("gt-loader");
    if (!loader) return;

    var contenedor = document.getElementById("gt-loader-anim");
    // La animación dura 90 frames a 30 fps = 3 s. Mantenemos el loader visible
    // ese tiempo para que se aprecie una pasada completa antes del fade-out.
    var DURACION_MINIMA_MS = 3000; // tiempo mínimo que el loader permanece visible

    // Accesibilidad: si el usuario prefiere menos movimiento, retiramos el
    // overlay sin reproducir la animación ni esperar el mínimo.
    var reduceMotion = window.matchMedia &&
        window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    var oculto = false;
    function ocultar() {
        if (oculto) return; // evita ocultarlo dos veces (load + respaldo)
        oculto = true;
        loader.classList.add("gt-loader-hidden");
        // Al terminar la transición, lo sacamos del árbol para no dejarlo activo.
        loader.addEventListener("transitionend", function () {
            loader.style.display = "none";
        }, { once: true });
        // Respaldo por si transitionend no dispara.
        setTimeout(function () { loader.style.display = "none"; }, 700);
    }

    // Carga la animación Lottie si la librería está disponible. Con reduced-motion
    // NO reproducimos el bucle (accesibilidad), pero sí mostramos el logo estático
    // en su último frame: el usuario ve la pantalla de carga, sin movimiento.
    if (contenedor && window.lottie) {
        var anim = window.lottie.loadAnimation({
            container: contenedor,
            renderer: "svg",
            loop: !reduceMotion,
            autoplay: !reduceMotion,
            path: "/animations/logo-loader.json"
        });
        if (reduceMotion) {
            anim.addEventListener("DOMLoaded", function () {
                anim.goToAndStop(anim.totalFrames - 1, true); // logo completo, quieto
            });
        }
    }

    // Oculta el loader respetando un tiempo MÍNIMO visible, medido desde el
    // inicio REAL de la navegación (performance.now() cuenta desde ahí, no desde
    // que corre este script, que va al final del body y podría ejecutarse tarde).
    function programarOcultado() {
        var transcurrido = (window.performance && performance.now) ? performance.now() : DURACION_MINIMA_MS;
        var restante = Math.max(0, DURACION_MINIMA_MS - transcurrido);
        setTimeout(ocultar, restante);
    }

    // La página puede estar aún cargando o ya haber terminado cuando este script
    // corre; cubrimos ambos casos (si "load" ya pasó, el listener no dispararía).
    if (document.readyState === "complete") {
        programarOcultado();
    } else {
        window.addEventListener("load", programarOcultado);
    }

    // Red de seguridad ABSOLUTA: pase lo que pase (load que nunca dispara,
    // performance ausente, error de Lottie), el overlay se retira como máximo a
    // los 6 s. Nunca deja la pantalla bloqueada.
    setTimeout(ocultar, 6000);
})();
