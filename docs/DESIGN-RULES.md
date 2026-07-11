Principios de diseño visual del proyecto. Léelos antes de tocar cualquier vista
o CSS. El objetivo es una interfaz sobria, coherente y profesional que NO parezca
generada por IA ("anti-slop").

## Identidad visual

- **Tema:** oscuro (dark), fondo casi negro frío (`--gt-bg`).
- **Acento principal:** lima eléctrico (`--gt-accent`, #c5ff2e). Usar con mesura:
  para acciones primarias, foco y detalles, NO para rellenar áreas grandes.
- **Tipografía display:** Saira Condensed (self-hosted) para títulos, stats y
  números grandes. Da el carácter deportivo. El cuerpo de texto usa la fuente de
  sistema para máxima legibilidad.
- **Sistema de tokens:** usar siempre las variables `--gt-*` (color, radio,
  sombra, espaciado). Nunca hardcodear un color hex suelto en una vista.

## Reglas anti-slop (lo que delata a la IA — EVITAR)

1. **Cero emojis decorativos.** Nada de 💪🔥📈 como íconos de sección o viñetas.
   Si se necesita un ícono, usar un SVG limpio y consistente, no un emoji.
2. **Nada de degradados arcoíris ni glows exagerados.** El acento brilla por
   contraste, no por saturar de sombras de neón todo.
3. **No repetir el mismo patrón "tres botones de texto" (Ver/Editar/Eliminar)**
   en cada fila: es el sello de proyecto escolar genérico. Preferir acciones que
   aparecen en hover o un menú compacto.
4. **No abusar de tarjetas anidadas.** Una tarjeta dentro de otra dentro de otra
   se ve caótico. Preferir jerarquía por espaciado y tipografía.
5. **Copys humanos, no de plantilla.** Evitar "Bienvenido de nuevo a tu
   dashboard". Preferir textos cortos, directos y con voz propia.
6. **Consistencia por encima de variedad.** Mismo radio, misma sombra, mismo
   espaciado en toda la app. La coherencia es lo que se ve "profesional".

## Espaciado y ritmo

- Márgenes y paddings en múltiplos coherentes (usar utilidades de Bootstrap:
  `mb-3`, `mb-4`, `g-3`). No mezclar valores arbitrarios.
- Aire generoso alrededor de títulos. El contenido respira.
- Alinear todo a una grilla; nada "flotando" suelto.

## Componentes clave (contratos visuales)

- **Títulos de página:** `.page-title.section-accent` (barra lateral de acento).
- **Tags de grupo muscular:** chip con acento lateral de color según el grupo
  (borde izquierdo grueso). Sobrio, no pastilla rellena.
- **Tags de resultado (historial):** mini barra de progreso que refleja el
  cumplimiento de la meta (real vs objetivo).
- **Acciones de fila:** aparecen al hacer hover sobre la fila, no siempre visibles.
- **Tarjetas de navegación:** clickeables en toda su superficie, no solo un botón.


## Checklist antes de dar por terminada una vista

- [ ] ¿Hay algún emoji decorativo? → quitarlo.
- [ ] ¿Los colores salen de las variables `--gt-*`? → sí.
- [ ] ¿El espaciado es consistente con el resto de la app? → sí.
- [ ] ¿Se repite el patrón genérico de tres botones? → reemplazar por hover.
- [ ] ¿La tipografía display (Saira) está en títulos y stats? → sí.
- [ ] ¿Algún texto suena a plantilla de IA? → reescribir con voz propia.
- [ ] ¿Compila y funciona igual que antes? → verificado.