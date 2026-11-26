# Videojuego 3D para Móviles - Rolling Spheres 3D

Proyecto desarrollado para la materia **Optativa: Diseño de juegos II (Ciclo 2026B)** en la **Universidad de Guadalajara (UDGVirtual / CUCEI)**.

- **Estudiante:** Mercedes de Jesús Ballesteros Chávez
- **Código:** 226300438
- **Asesor:** Oscar Didier Sanchez Sanchez
- **Actividad:** 4.1. Una propuesta en 3D
- **Motor:** Unity 3D (C#)
- **Plataforma:** Dispositivos Móviles (Android / iOS) y PC

---

## 🎮 Descripción del Proyecto
*Rolling Spheres 3D* es un prototipo de videojuego arcade y plataformas en tres dimensiones diseñado con un enfoque prioritario en dispositivos móviles con pantalla táctil. El jugador controla un orbe tridimensional a través de un circuito con rampas con pendiente pronunciada, plataformas suspendidas y obstáculos geométricos.

---

## 📋 Cumplimiento de Criterios Oficiales de Moodle

| Criterio Evaluado | Estado | Implementación Técnica |
| :--- | :---: | :--- |
| **Criterio 1: UI Móvil** | ✅ Cumplido | `MobileCanvas` adaptativo con `CanvasScaler` (`Scale With Screen Size` 1080x1920), D-Pad táctil en pantalla, botón de salto, botón de pausa y menú interactivo con `SceneManager`. |
| **Criterio 2: Movimiento 3D (X, Y, Z)** | ✅ Cumplido | `PlayerController3D.cs` gobernado por `Rigidbody` tridimensional: desplazamiento horizontal en X, avance y retroceso en profundidad Z, e impulso vertical con gravedad adaptativa en Y. |
| **Criterio 3: Cámara en X y Z** | ✅ Cumplido | `CameraFollow3D.cs` con cinemática suave (`Vector3.SmoothDamp`) sincronizada únicamente con los ejes X y Z del jugador, desacoplando la altura Y para evitar saltos o sacudidas de cámara. |
| **Criterio 4: Restricción Z Fija** | ✅ Cumplido | La cámara mantiene un offset constante e inmutable en el eje de profundidad Z (`smoothedPosition.z = target.position.z + offset.z`), preservando el encuadre óptimo en móviles. |

---

## 🛠️ Arquitectura de Scripts C#

- `PlayerController3D.cs`: Movimiento físico del jugador en los 3 ejes espaciales (X, Y, Z) con soporte híbrido (teclado + controles táctiles) y detección de suelo esférica (`Physics.CheckSphere`).
- `CameraFollow3D.cs`: Cinemática de seguimiento suave en tercera persona con sincronización estricta en X y Z y distancia constante en Z.
- `MobileUIManager3D.cs`: Gestor de pantallas, pausa del juego (`Time.timeScale`), reinicio y navegación entre escenas.
- `VirtualTouchButton.cs`: Controlador táctil para botones en pantalla basado en `IPointerDownHandler` y `IPointerUpHandler`.
- `SetupScene3DHelper.cs`: Asistente de editor accesible desde `Herramientas 3D > Montar Escenario y Controles 3D (Actividad 4.1)`.

---

## 🚀 Instrucciones de Ejecución

1. Abrir el proyecto en **Unity 6** o superior (plantilla 3D).
2. Si la escena está vacía, ejecutar:
   `Herramientas 3D > Montar Escenario y Controles 3D (Actividad 4.1)`.
3. En la ventana **Game**, seleccionar resolución vertical **1080x1920** (Portrait) o usar el simulador de dispositivos móviles.
4. Presionar **Play** y controlar el orbe con los botones táctiles en pantalla o las teclas `W`, `A`, `S`, `D` y `Espacio`.
