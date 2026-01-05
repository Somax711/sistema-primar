# Verificación de Notificaciones en la Interfaz Web

## Pasos para verificar que las notificaciones lleguen a cada rol:

### 1. Preparación
- Asegúrate de que el proyecto esté ejecutándose
- Tener al menos un usuario de cada rol (empleado, supervisor, gerente, admin)
- Tener la base de datos conectada

### 2. Verificar Usuarios Existentes
Ejecutar en SQL Server:
```sql
SELECT Id, Nombre + ' ' + Apellidos AS NombreCompleto, Email, Rol, Activo
FROM Usuarios 
WHERE Activo = 1
ORDER BY Rol, Nombre;
```

### 3. Flujo de Prueba Completo

#### Paso 1: Crear una Rendición (como Empleado)
1. Iniciar sesión como empleado
2. Ir a "Crear Rendición"
3. Llenar los datos:
   - Título: "Prueba Notificaciones"
   - Descripción: "Rendición para probar el sistema de notificaciones"
   - Monto: 150000
   - Adjuntar algún archivo
4. Hacer clic en "Crear Rendición"

**Verificar:**
- La rendición se crea correctamente
- Aparece en "Mis Rendiciones" del empleado
- Se genera notificación automática

#### Paso 2: Verificar Notificación al Supervisor
1. Iniciar sesión como supervisor
2. Ir a "Notificaciones"
3. Verificar que aparece la notificación:
   - Mensaje: "Nueva rendición [NÚMERO] de [EMPLEADO] requiere tu aprobación."
   - Tipo: supervisor
   - No leída

#### Paso 3: Aprobar como Supervisor
1. Como supervisor, ir a "Rendiciones Pendientes"
2. Buscar la rendición de prueba
3. Hacer clic en "Aprobar"
4. Agregar comentario: "Aprobado para prueba"
5. Hacer clic en "Confirmar Aprobación"

**Verificar:**
- La rendición cambia a estado "Aprobada"
- Se genera notificación al gerente
- Se genera notificación al empleado

#### Paso 4: Verificar Notificación al Gerente
1. Iniciar sesión como gerente
2. Ir a "Notificaciones"
3. Verificar que aparece la notificación:
   - Mensaje: "Rendición [NÚMERO] requiere tu aprobación final."
   - Tipo: gerente
   - No leída

#### Paso 5: Aprobar como Gerente
1. Como gerente, ir a "Rendiciones Pendientes"
2. Buscar la rendición de prueba
3. Hacer clic en "Aprobar"
4. Agregar comentario: "Aprobación final completada"
5. Hacer clic en "Confirmar Aprobación"

**Verificar:**
- La rendición cambia a estado "Aprobada"
- Se genera notificación al supervisor para pago
- Se genera notificación al empleado

#### Paso 6: Marcar como Pagada
1. Como supervisor, ir a "Rendiciones Pendientes"
2. Buscar la rendición aprobada
3. Hacer clic en "Marcar como Pagada"

**Verificar:**
- La rendición cambia a estado "Pagada"
- Se genera notificación al empleado

### 4. Verificación en Base de Datos

Ejecutar después de cada paso:
```sql
-- Verificar notificaciones recientes
SELECT 
    n.Id,
    u.Nombre + ' ' + u.Apellidos AS Usuario,
    u.Rol AS RolUsuario,
    n.Mensaje,
    n.TipoRol,
    n.Leido,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
WHERE n.RendicionId = [ID_DE_LA_RENDICIÓN]
ORDER BY n.FechaCreacion DESC;
```

### 5. Verificación por Rol

#### Para Empleados:
- Deben recibir notificaciones cuando:
  - Su rendición es aprobada por supervisor
  - Su rendición es aprobada por gerente
  - Su rendición es pagada
  - Su rendición es rechazada

#### Para Supervisores:
- Deben recibir notificaciones cuando:
  - Se crea una nueva rendición
  - Una rendición es aprobada por gerente (para proceder con pago)

#### Para Gerentes:
- Deben recibir notificaciones cuando:
  - Una rendición es aprobada por supervisor

### 6. Verificar Interfaz de Notificaciones

En cada rol, verificar:
1. **Contador de notificaciones** en el menú superior
2. **Página de notificaciones** muestra las correctas
3. **Marcar como leída** funciona
4. **Eliminar notificación** funciona
5. **Filtros por tipo** funcionan (si existen)

### 7. Verificar Tiempo Real (SignalR)

1. Abrir múltiples pestañas con diferentes roles
2. Crear una rendición en una pestaña
3. Verificar que las notificaciones aparecen automáticamente en las otras pestañas

### 8. Casos de Error

Probar:
1. **Rechazar rendición** como supervisor
2. **Rechazar rendición** como gerente
3. **Crear rendición sin archivos**
4. **Aprobar rendición ya aprobada**

### 9. Verificación Final

Ejecutar este script para verificar el estado final:
```sql
-- Resumen completo de notificaciones
SELECT 
    n.TipoRol,
    COUNT(*) AS TotalNotificaciones,
    COUNT(CASE WHEN n.Leido = 1 THEN 1 END) AS Leidas,
    COUNT(CASE WHEN n.Leido = 0 THEN 1 END) AS NoLeidas
FROM Notificaciones n
GROUP BY n.TipoRol
ORDER BY n.TipoRol;
```

## Problemas Comunes y Soluciones

### Si no aparecen notificaciones:
1. Verificar que el usuario tenga el rol correcto
2. Verificar que el usuario esté activo
3. Verificar que el método `CambiarEstadoRendicionAsync` se esté llamando
4. Verificar logs de errores en la aplicación

### Si aparecen notificaciones duplicadas:
1. Verificar que no se llame múltiples veces a los métodos de notificación
2. Verificar que el método centralizado funcione correctamente

### Si las notificaciones no se actualizan en tiempo real:
1. Verificar que SignalR esté configurado correctamente
2. Verificar la conexión del hub de notificaciones 