# Tanques - Juego para 2 jugadores (Unity)

Juego de tanques para 2 jugadores en el mismo teclado, en 3D ortográfico. Es la base del
tutorial "Tanks!" de Unity, siguiendo el PDF del profesor, ampliada para la Actividad Semana 4.

## Autor

- **Estudiante:** Edwin Jesús Flete Cruz
- **Matrícula:** 1-22-1805
- **Universidad:** UTESA, Sede Santiago
- **Asignatura:** Programación de Video Juegos (INF-910-001)
- **Profesor:** Iván Mendoza
- **Motor:** Unity 6 (6000.6.4f1), URP, C#

## Enlaces

- **Jugar en el navegador:** PENDIENTE (se llena al publicar)
- **Repositorio:** PENDIENTE (se llena al subir)

## Controles

| Acción | Jugador 1 (azul) | Jugador 2 (rojo) |
|---|---|---|
| Avanzar / retroceder | W / S | Flecha arriba / abajo |
| Girar | A / D | Flecha izquierda / derecha |
| Disparar (mantener para cargar) | Barra espaciadora | Enter |
| Reiniciar (al terminar la partida) | R o botón en pantalla | R o botón en pantalla |

Algunos teclados no registran 3 o más teclas a la vez (ghosting). Si un jugador deja de
responder mientras el otro dispara, es el teclado, no el juego.

## Cómo se juega

- Gana la ronda quien destruye al otro tanque. Si ambos se destruyen, es empate de ronda.
- Cada ronda perdida cuenta como un **intento perdido**. Al llegar a 5 intentos perdidos
  pierdes la partida y gana el otro jugador.
- La partida tiene un reloj de **4 minutos** (240 s) que corre solo mientras se juega. Si llega
  a 0:00, ambos pierden.
- Puntaje: 100 por ronda ganada y 25 por cada enemigo destruido.
- Los **enemigos** patrullan el mapa, estorban a los tanques y los dañan al tocarlos. Las
  bombas los destruyen.

## Requisitos de la actividad

| ID | Requisito | Estado |
|---|---|---|
| RF-01 | Nombre y matrícula en la esquina superior derecha | Hecho en código (por probar) |
| RF-02 | Código en GitHub y juego publicado | Pendiente |
| RF-03 | Música de fondo: marcha fúnebre | Script listo, falta la grabación |
| RF-04 | El juego termina cuando un jugador pierde 5 veces | Hecho en código (por probar) |
| RF-05 | Pantalla del ganador con puntaje y tiempo | Hecho en código (por probar) |
| RF-06 | Temporizador máximo de 4 minutos; si llega a 0 ambos pierden | Hecho en código (por probar) |
| RF-07 | Enemigos que obstaculizan a los tanques | Hecho en código (por probar) |

## Estructura de scripts (`Assets/Scripts`)

- `Tank/` TankMovement, TankShooting, TankHealth (del PDF del profesor)
- `Camera/` CameraControl (del PDF)
- `Shell/` ShellExplosion (del PDF; ahora también daña a los enemigos)
- `Managers/` GameManager y TankManager (del PDF; con intentos perdidos, reloj, puntaje y pantalla final)
- `UI/` UIDirectionControl (del PDF) y HUDJuego (nuevo)
- `Enemigos/` EnemigoPatrulla y PuntoEnemigo (nuevos)
- `Audio/` MusicaFondo (nuevo)

## Créditos

- Modelos, sonidos y arte: paquete gratuito "Tanks!" de Unity Technologies (Unity Companion License).
- Música: PENDIENTE (título, autor, licencia y enlace de la marcha fúnebre).
- Guía del juego base: PDF "Videojuego para 2 jugadores en 3D ortográfico", prof. Iván Mendoza.
