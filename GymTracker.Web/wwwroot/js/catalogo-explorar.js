// catalogo-explorar.js
// Filtrado en cliente de "Explorar ejercicios": búsqueda instantánea + filtros
// combinados (zona, equipamiento, patrón, sub-músculo) + contadores dinámicos.
// Los datos vienen una sola vez de /Catalogo/Datos; todo lo demás es en memoria.

(function () {
    'use strict';

    const PAGINA = 24; // cuántas tarjetas se muestran/añaden por tanda

    // ===== Traducciones y mapeos (los datos del catálogo están en inglés) =====

    const BODY_ES = {
        'chest': 'Pecho', 'back': 'Espalda', 'upper legs': 'Piernas',
        'shoulders': 'Hombros', 'upper arms': 'Brazos', 'lower arms': 'Antebrazos',
        'lower legs': 'Pantorrillas', 'waist': 'Core', 'neck': 'Cuello', 'cardio': 'Cardio'
    };
    const ORDEN_ZONAS = ['chest', 'back', 'upper legs', 'shoulders', 'upper arms',
        'lower arms', 'lower legs', 'waist', 'neck', 'cardio'];

    const MUSCULO_ES = {
        'abs': 'Abdomen', 'pectorals': 'Pectorales', 'biceps': 'Bíceps',
        'glutes': 'Glúteos', 'delts': 'Deltoides', 'triceps': 'Tríceps',
        'upper back': 'Espalda alta', 'lats': 'Dorsales', 'calves': 'Pantorrillas',
        'quads': 'Cuádriceps', 'forearms': 'Antebrazos', 'hamstrings': 'Isquiotibiales',
        'cardiovascular system': 'Cardiovascular', 'spine': 'Zona lumbar', 'traps': 'Trapecios',
        'adductors': 'Aductores', 'abductors': 'Abductores',
        'serratus anterior': 'Serrato anterior', 'levator scapulae': 'Elevador de la escápula'
    };

    // Buckets de equipamiento (valores crudos -> grupo en español).
    const EQUIPOS = [
        { key: 'mancuernas', label: 'Mancuernas', vals: ['dumbbell'] },
        { key: 'barra', label: 'Barra', vals: ['barbell', 'ez barbell', 'olympic barbell', 'trap bar'] },
        { key: 'polea', label: 'Polea', vals: ['cable'] },
        {
            key: 'maquina', label: 'Máquina', vals: ['leverage machine', 'smith machine',
                'sled machine', 'assisted', 'upper body ergometer', 'skierg machine',
                'elliptical machine', 'stepmill machine', 'stationary bike', 'hammer']
        },
        { key: 'pesocorporal', label: 'Peso corporal', vals: ['body weight', 'weighted'] },
        { key: 'bandas', label: 'Bandas', vals: ['band', 'resistance band'] },
        { key: 'kettlebell', label: 'Kettlebell', vals: ['kettlebell'] }
        // Todo lo que no cae en un bucket conocido va a 'otros' (ver derivarEquipos).
    ];
    const EQUIPO_OTROS = { key: 'otros', label: 'Otros' };
    // Índice valor-crudo -> key de bucket.
    const VALOR_A_BUCKET = {};
    EQUIPOS.forEach(b => b.vals.forEach(v => { VALOR_A_BUCKET[v] = b.key; }));

    // Patrones de movimiento derivados de los músculos objetivo.
    const PATRONES = [
        { key: 'empuje', label: 'Empuje', musc: ['pectorals', 'delts', 'triceps', 'serratus anterior'] },
        { key: 'traccion', label: 'Tracción', musc: ['lats', 'upper back', 'biceps', 'forearms', 'traps', 'levator scapulae'] },
        { key: 'pierna', label: 'Pierna', musc: ['glutes', 'quads', 'hamstrings', 'calves', 'adductors', 'abductors'] },
        { key: 'core', label: 'Core', musc: ['abs', 'spine'] }
    ];
    const MUSC_A_PATRON = {};
    PATRONES.forEach(p => p.musc.forEach(m => { (MUSC_A_PATRON[m] = MUSC_A_PATRON[m] || []).push(p.key); }));

    // ===== Utilidades =====
    const esc = s => String(s == null ? '' : s).replace(/[&<>"']/g, c =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const cap = s => s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
    const musculoEs = m => MUSCULO_ES[m] || cap(m);
    const bodyEs = b => BODY_ES[b] || cap(b);
    const interseca = (setA, iterB) => { for (const x of iterB) { if (setA.has(x)) return true; } return false; };

    function derivarEquipos(equipArr) {
        const grupos = new Set();
        (equipArr || []).forEach(v => grupos.add(VALOR_A_BUCKET[v] || 'otros'));
        return grupos;
    }
    function derivarPatrones(targetArr) {
        const pats = new Set();
        (targetArr || []).forEach(m => (MUSC_A_PATRON[m] || []).forEach(p => pats.add(p)));
        return pats;
    }

    // ===== Estado =====
    let TODOS = [];                 // catálogo completo (enriquecido)
    let mostrados = PAGINA;         // cuántos renderizar
    let filtradosCache = [];        // último resultado filtrado (para "Ver más")
    let misEjercicios = null;       // para el modal Vincular (se carga una vez)

    const estado = {
        q: '',
        zona: null,                 // single-select
        equipos: new Set(),         // multi
        patrones: new Set(),        // multi
        musculos: new Set()         // multi (dependen de la zona)
    };

    // ===== Filtro combinado =====
    // `omit` permite excluir una categoría para calcular su contador dinámico.
    function pasa(e, omit) {
        if (estado.q && !e.nameLower.includes(estado.q)) return false;
        if (omit !== 'zona' && estado.zona && !e.body.includes(estado.zona)) return false;
        if (omit !== 'equipos' && estado.equipos.size && !interseca(e.equipGroups, estado.equipos)) return false;
        if (omit !== 'patrones' && estado.patrones.size && !interseca(e.patterns, estado.patrones)) return false;
        if (omit !== 'musculos' && estado.musculos.size && !e.target.some(m => estado.musculos.has(m))) return false;
        return true;
    }

    // ===== Referencias al DOM =====
    let el = {};

    function init() {
        el = {
            buscar: document.getElementById('fx-buscar'),
            zonas: document.getElementById('fx-zonas'),
            subWrap: document.getElementById('fx-submusculo-wrap'),
            sub: document.getElementById('fx-submusculo'),
            toggleMas: document.getElementById('fx-toggle-mas'),
            panelMas: document.getElementById('fx-panel-mas'),
            equipos: document.getElementById('fx-equipos'),
            patrones: document.getElementById('fx-patrones'),
            activos: document.getElementById('fx-activos'),
            info: document.getElementById('fx-resultados-info'),
            grid: document.getElementById('fx-grid'),
            verMasWrap: document.getElementById('fx-vermas-wrap'),
            verMas: document.getElementById('fx-vermas'),
            vacio: document.getElementById('fx-vacio'),
            cargando: document.getElementById('fx-cargando')
        };

        el.buscar.addEventListener('input', () => {
            estado.q = el.buscar.value.trim().toLowerCase();
            mostrados = PAGINA;
            aplicar();
        });

        el.toggleMas.addEventListener('click', () => {
            const abierto = el.panelMas.classList.toggle('abierto');
            el.toggleMas.setAttribute('aria-expanded', abierto ? 'true' : 'false');
            el.toggleMas.classList.toggle('activo', abierto);
        });

        el.verMas.addEventListener('click', () => {
            mostrados += PAGINA;
            renderGrid();
        });

        // Vincular por delegación (las tarjetas se crean dinámicamente).
        el.grid.addEventListener('click', (ev) => {
            const btn = ev.target.closest('.gt-cat-vincular');
            if (btn) { ev.preventDefault(); abrirVincular(btn.dataset.exid, btn.dataset.nombre); }
        });

        cargarDatos();
        initVincular();
    }

    async function cargarDatos() {
        try {
            const r = await fetch('/Catalogo/Datos');
            const crudos = await r.json();
            TODOS = crudos.map(e => ({
                id: e.id, name: e.name, gif: e.gif,
                body: e.body || [], target: e.target || [],
                nameLower: (e.name || '').toLowerCase(),
                equipGroups: derivarEquipos(e.equip),
                patterns: derivarPatrones(e.target)
            }));
            el.cargando.classList.add('d-none');
            renderZonas();
            renderEquipos();
            renderPatrones();
            aplicar();
        } catch (err) {
            el.cargando.textContent = 'No se pudo cargar el catálogo. Recarga la página.';
        }
    }

    // ===== Contadores dinámicos =====
    function contarZona(z) { return TODOS.reduce((n, e) => n + (pasa(e, 'zona') && e.body.includes(z) ? 1 : 0), 0); }
    function contarEquipo(k) { return TODOS.reduce((n, e) => n + (pasa(e, 'equipos') && e.equipGroups.has(k) ? 1 : 0), 0); }
    function contarPatron(k) { return TODOS.reduce((n, e) => n + (pasa(e, 'patrones') && e.patterns.has(k) ? 1 : 0), 0); }
    function contarMusculo(m) { return TODOS.reduce((n, e) => n + (pasa(e, 'musculos') && e.target.includes(m) ? 1 : 0), 0); }

    // ===== Render de filtros =====
    function pill(texto, count, activo, extraClass) {
        const c = count != null ? ` <span class="gt-pill-count">${count}</span>` : '';
        return `<button type="button" class="gt-pill ${activo ? 'activo' : ''} ${extraClass || ''}">${esc(texto)}${c}</button>`;
    }

    function renderZonas() {
        const zonas = ORDEN_ZONAS.filter(z => TODOS.some(e => e.body.includes(z)));
        let html = `<button type="button" class="gt-pill ${estado.zona === null ? 'activo' : ''}" data-zona="">Todas</button>`;
        html += zonas.map(z =>
            `<button type="button" class="gt-pill ${estado.zona === z ? 'activo' : ''}" data-zona="${z}">${esc(bodyEs(z))} <span class="gt-pill-count">${contarZona(z)}</span></button>`
        ).join('');
        el.zonas.innerHTML = html;
        el.zonas.querySelectorAll('[data-zona]').forEach(b => {
            b.addEventListener('click', () => {
                estado.zona = b.dataset.zona || null;
                estado.musculos.clear();       // el sub-músculo depende de la zona
                mostrados = PAGINA;
                aplicar();
            });
        });
    }

    function renderSubMusculo() {
        if (!estado.zona) { el.subWrap.classList.add('d-none'); el.sub.innerHTML = ''; return; }
        // Músculos presentes en la zona seleccionada.
        const musc = [];
        TODOS.forEach(e => { if (e.body.includes(estado.zona)) e.target.forEach(m => { if (!musc.includes(m)) musc.push(m); }); });
        if (!musc.length) { el.subWrap.classList.add('d-none'); return; }
        el.subWrap.classList.remove('d-none');
        el.sub.innerHTML = musc.map(m =>
            `<button type="button" class="gt-pill ${estado.musculos.has(m) ? 'activo' : ''}" data-musc="${esc(m)}">${esc(musculoEs(m))} <span class="gt-pill-count">${contarMusculo(m)}</span></button>`
        ).join('');
        el.sub.querySelectorAll('[data-musc]').forEach(b => {
            b.addEventListener('click', () => {
                const m = b.dataset.musc;
                estado.musculos.has(m) ? estado.musculos.delete(m) : estado.musculos.add(m);
                mostrados = PAGINA;
                aplicar();
            });
        });
    }

    function renderEquipos() {
        el.equipos.innerHTML = EQUIPOS.concat(EQUIPO_OTROS).map(b =>
            `<button type="button" class="gt-pill ${estado.equipos.has(b.key) ? 'activo' : ''}" data-equipo="${b.key}">${esc(b.label)} <span class="gt-pill-count">${contarEquipo(b.key)}</span></button>`
        ).join('');
        el.equipos.querySelectorAll('[data-equipo]').forEach(b => {
            b.addEventListener('click', () => {
                const k = b.dataset.equipo;
                estado.equipos.has(k) ? estado.equipos.delete(k) : estado.equipos.add(k);
                mostrados = PAGINA;
                aplicar();
            });
        });
    }

    function renderPatrones() {
        el.patrones.innerHTML = PATRONES.map(p =>
            `<button type="button" class="gt-pill ${estado.patrones.has(p.key) ? 'activo' : ''}" data-patron="${p.key}">${esc(p.label)} <span class="gt-pill-count">${contarPatron(p.key)}</span></button>`
        ).join('');
        el.patrones.querySelectorAll('[data-patron]').forEach(b => {
            b.addEventListener('click', () => {
                const k = b.dataset.patron;
                estado.patrones.has(k) ? estado.patrones.delete(k) : estado.patrones.add(k);
                mostrados = PAGINA;
                aplicar();
            });
        });
    }

    function renderActivos() {
        const chips = [];
        if (estado.zona) chips.push({ tipo: 'zona', val: estado.zona, txt: bodyEs(estado.zona) });
        estado.equipos.forEach(k => { const b = EQUIPOS.concat(EQUIPO_OTROS).find(x => x.key === k); chips.push({ tipo: 'equipo', val: k, txt: b ? b.label : k }); });
        estado.patrones.forEach(k => { const p = PATRONES.find(x => x.key === k); chips.push({ tipo: 'patron', val: k, txt: p ? p.label : k }); });
        estado.musculos.forEach(m => chips.push({ tipo: 'musc', val: m, txt: musculoEs(m) }));

        if (!chips.length) { el.activos.innerHTML = ''; return; }
        el.activos.innerHTML = chips.map(c =>
            `<button type="button" class="gt-chip-activo" data-tipo="${c.tipo}" data-val="${esc(c.val)}">${esc(c.txt)}<span class="gt-chip-x" aria-hidden="true">&times;</span></button>`
        ).join('') + `<button type="button" class="gt-chip-limpiar" id="fx-limpiar">Limpiar todo</button>`;

        el.activos.querySelectorAll('.gt-chip-activo').forEach(b => {
            b.addEventListener('click', () => {
                const { tipo, val } = b.dataset;
                if (tipo === 'zona') { estado.zona = null; estado.musculos.clear(); }
                else if (tipo === 'equipo') estado.equipos.delete(val);
                else if (tipo === 'patron') estado.patrones.delete(val);
                else if (tipo === 'musc') estado.musculos.delete(val);
                mostrados = PAGINA;
                aplicar();
            });
        });
        document.getElementById('fx-limpiar').addEventListener('click', () => {
            estado.zona = null; estado.equipos.clear(); estado.patrones.clear(); estado.musculos.clear();
            estado.q = ''; el.buscar.value = '';
            mostrados = PAGINA;
            aplicar();
        });
    }

    // ===== Render del grid =====
    const PLACEHOLDER = 'data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22100%22 height=%22100%22%3E%3Crect fill=%22%23eee%22 width=%22100%22 height=%22100%22/%3E%3Ctext x=%2250%22 y=%2250%22 fill=%22%23999%22 font-size=%228%22 text-anchor=%22middle%22 dy=%22.3em%22%3ESin animaci%C3%B3n%3C/text%3E%3C/svg%3E';

    function tarjeta(e) {
        const chips = e.target.slice(0, 2).map(m => `<span class="gt-cat-chip">${esc(musculoEs(m))}</span>`).join('');
        return `<div class="gt-cat-card-wrap">
            <a href="/Catalogo/Detalle/${encodeURIComponent(e.id)}" class="gt-cat-card">
                <img src="${esc(e.gif)}" alt="${esc(e.name)}" class="gt-cat-gif" loading="lazy"
                     onerror="this.onerror=null;this.src='${PLACEHOLDER}';" />
                <div class="gt-cat-body">
                    <div class="gt-cat-nombre">${esc(e.name)}</div>
                    <div class="gt-cat-meta">${chips}</div>
                </div>
            </a>
            <button type="button" class="gt-cat-vincular" data-exid="${esc(e.id)}" data-nombre="${esc(e.name)}" title="Vincular a mis ejercicios">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
                    <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" />
                </svg>
                <span>Vincular</span>
            </button>
        </div>`;
    }

    function renderGrid() {
        const slice = filtradosCache.slice(0, mostrados);
        el.grid.innerHTML = slice.map(tarjeta).join('');
        const hayMas = mostrados < filtradosCache.length;
        el.verMasWrap.classList.toggle('d-none', !hayMas);
    }

    // ===== Aplicar (re-filtra, re-cuenta, re-renderiza) =====
    function aplicar() {
        filtradosCache = TODOS.filter(e => pasa(e, null));

        // refrescar contadores/estados de todos los filtros
        renderZonas();
        renderSubMusculo();
        renderEquipos();
        renderPatrones();
        renderActivos();

        const n = filtradosCache.length;
        el.info.textContent = n === 1 ? '1 ejercicio' : `${n} ejercicios`;
        el.vacio.classList.toggle('d-none', n !== 0);

        renderGrid();
    }

    // ===== Vinculación (idéntica a la de la Fase 2, por delegación) =====
    let mv = {};
    function initVincular() {
        mv = {
            modalEl: document.getElementById('modalVincular'),
            select: document.getElementById('vinculoSelect'),
            catNombre: document.getElementById('vinculoCatNombre'),
            err: document.getElementById('vinculoError'),
            picker: document.getElementById('vinculoPicker'),
            sinEj: document.getElementById('vinculoSinEjercicios'),
            confirmar: document.getElementById('vinculoConfirmar'),
            toast: document.getElementById('vinculoToast')
        };
        if (!mv.modalEl) return;
        mv.modal = new bootstrap.Modal(mv.modalEl);
        const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        mv.token = tokenEl ? tokenEl.value : '';
        mv.exid = null;
        mv.confirmar.addEventListener('click', confirmarVincular);
    }

    async function cargarMios() {
        if (misEjercicios) return misEjercicios;
        const r = await fetch('/Ejercicios/Mios');
        misEjercicios = await r.json();
        return misEjercicios;
    }
    function mostrarToast(msg) {
        mv.toast.textContent = msg;
        mv.toast.classList.remove('d-none');
        clearTimeout(mv.toast._t);
        mv.toast._t = setTimeout(() => mv.toast.classList.add('d-none'), 2600);
    }
    async function abrirVincular(exid, nombre) {
        if (!mv.modal) return;
        mv.exid = exid;
        mv.catNombre.textContent = nombre;
        mv.err.classList.add('d-none');
        const mios = await cargarMios();
        if (!mios.length) {
            mv.picker.classList.add('d-none'); mv.sinEj.classList.remove('d-none'); mv.confirmar.classList.add('d-none');
        } else {
            mv.picker.classList.remove('d-none'); mv.sinEj.classList.add('d-none'); mv.confirmar.classList.remove('d-none');
            mv.select.innerHTML = mios.map(e =>
                `<option value="${e.id}">${esc(e.nombre)}${e.exerciseDbId ? ' · ya vinculado' : ''}</option>`).join('');
        }
        mv.modal.show();
    }
    async function confirmarVincular() {
        const ejercicioId = mv.select.value;
        if (!ejercicioId) return;
        mv.confirmar.disabled = true;
        mv.err.classList.add('d-none');
        try {
            const body = new URLSearchParams({ ejercicioId, exerciseDbId: mv.exid, __RequestVerificationToken: mv.token });
            const r = await fetch('/Ejercicios/Vincular', { method: 'POST', body });
            const data = await r.json();
            if (r.ok && data.ok) {
                const e = misEjercicios.find(x => x.id == ejercicioId);
                if (e) e.exerciseDbId = mv.exid;
                mv.modal.hide();
                mostrarToast('Vinculado a "' + data.nombre + '"');
            } else {
                mv.err.textContent = data.error || 'No se pudo vincular.';
                mv.err.classList.remove('d-none');
            }
        } catch {
            mv.err.textContent = 'Error de red al vincular.';
            mv.err.classList.remove('d-none');
        } finally {
            mv.confirmar.disabled = false;
        }
    }

    document.addEventListener('DOMContentLoaded', init);
})();
