-- Script simplificado para probar notificaciones
-- Ejecutar en MySQL Workbench o phpMyAdmin
-- Asegúrate de seleccionar la base de datos: sistema_rendiciones

USE sistema_rendiciones;

-- PASO 1: Verificar usuarios existentes
SELECT '=== USUARIOS EXISTENTES ===' AS Info;
SELECT 
    Id,
    CONCAT(Nombre, ' ', Apellidos) AS NombreCompleto,
    Email,
    Rol,
    Activo
FROM Usuarios 
WHERE Activo = 1
ORDER BY Rol, Nombre;

-- PASO 2: Verificar notificaciones existentes
SELECT '=== NOTIFICACIONES EXISTENTES ===' AS Info;
SELECT 
    n.Id,
    u.Nombre,
    u.Rol AS RolUsuario,
    n.Mensaje,
    n.TipoRol,
    n.Leido,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
ORDER BY n.FechaCreacion DESC
LIMIT 10;

-- PASO 3: Crear una rendición de prueba
SELECT '=== CREANDO RENDICIÓN DE PRUEBA ===' AS Info;

-- Obtener un empleado para la prueba
SET @UsuarioId = (SELECT Id FROM Usuarios WHERE Rol = 'empleado' AND Activo = 1 LIMIT 1);
SET @NumeroTicket = CONCAT('RND-TEST-', YEAR(NOW()), '-', MONTH(NOW()), '-', DAY(NOW()), '-', HOUR(NOW()), MINUTE(NOW()));

-- Insertar rendición
INSERT INTO Rendiciones (
    NumeroTicket, 
    UsuarioId, 
    Titulo, 
    Descripcion, 
    MontoTotal, 
    Estado, 
    FechaCreacion,
    Nombre,
    Apellidos,
    Rut,
    Telefono,
    Cargo,
    Departamento
)
SELECT 
    @NumeroTicket,
    @UsuarioId,
    'Prueba Notificaciones',
    'Rendición para probar el sistema de notificaciones',
    150000,
    'pendiente',
    NOW(),
    u.Nombre,
    u.Apellidos,
    u.Rut,
    u.Telefono,
    u.Cargo,
    u.Departamento
FROM Usuarios u 
WHERE u.Id = @UsuarioId;

SET @RendicionId = LAST_INSERT_ID();
SELECT CONCAT('Rendición creada con ID: ', @RendicionId) AS Info;

-- PASO 4: Simular notificación al supervisor
SELECT '=== NOTIFICANDO A SUPERVISORES ===' AS Info;
INSERT INTO Notificaciones (UsuarioId, RendicionId, Mensaje, Leido, FechaCreacion, TipoRol)
SELECT 
    u.Id,
    @RendicionId,
    CONCAT('Nueva rendición ', @NumeroTicket, ' de ', (SELECT CONCAT(Nombre, ' ', Apellidos) FROM Usuarios WHERE Id = @UsuarioId), ' requiere tu aprobación.'),
    0,
    NOW(),
    'supervisor'
FROM Usuarios u 
WHERE u.Rol = 'aprobador1' AND u.Activo = 1;

SELECT CONCAT('Notificaciones enviadas a supervisores: ', ROW_COUNT()) AS Info;

-- PASO 5: Verificar notificaciones creadas
SELECT '=== NOTIFICACIONES CREADAS ===' AS Info;
SELECT 
    n.Id,
    u.Nombre,
    u.Rol AS RolUsuario,
    n.Mensaje,
    n.TipoRol,
    n.Leido,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
WHERE n.RendicionId = @RendicionId
ORDER BY n.FechaCreacion DESC;

-- PASO 6: Simular aprobación del supervisor
SELECT '=== SIMULANDO APROBACIÓN DEL SUPERVISOR ===' AS Info;
UPDATE Rendiciones 
SET 
    Estado = 'aprobado_1',
    Aprobador1Id = (SELECT Id FROM Usuarios WHERE Rol = 'aprobador1' AND Activo = 1 LIMIT 1),
    FechaAprobacion1 = NOW(),
    ComentariosAprobador = 'Aprobado por supervisor - Prueba'
WHERE Id = @RendicionId;

-- PASO 7: Notificar al gerente
SELECT '=== NOTIFICANDO A GERENTES ===' AS Info;
INSERT INTO Notificaciones (UsuarioId, RendicionId, Mensaje, Leido, FechaCreacion, TipoRol)
SELECT 
    u.Id,
    @RendicionId,
    CONCAT('Rendición ', @NumeroTicket, ' requiere tu aprobación final.'),
    0,
    NOW(),
    'gerente'
FROM Usuarios u 
WHERE u.Rol = 'aprobador2' AND u.Activo = 1;

-- PASO 8: Notificar al empleado
INSERT INTO Notificaciones (UsuarioId, RendicionId, Mensaje, Leido, FechaCreacion, TipoRol)
VALUES (
    @UsuarioId,
    @RendicionId,
    CONCAT('Tu rendición ', @NumeroTicket, ' fue aprobada por el supervisor y está pendiente de aprobación del gerente.'),
    0,
    NOW(),
    'empleado'
);

SELECT 'Notificaciones enviadas a gerentes y empleado' AS Info;

-- PASO 9: Verificar todas las notificaciones del flujo
SELECT '=== RESUMEN DE NOTIFICACIONES ===' AS Info;
SELECT 
    n.TipoRol,
    u.Nombre,
    u.Rol AS RolUsuario,
    n.Mensaje,
    n.Leido,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
WHERE n.RendicionId = @RendicionId
ORDER BY n.FechaCreacion DESC;

-- PASO 10: Contar notificaciones por tipo
SELECT '=== CONTEO DE NOTIFICACIONES ===' AS Info;
SELECT 
    n.TipoRol,
    COUNT(*) AS TotalNotificaciones
FROM Notificaciones n
WHERE n.RendicionId = @RendicionId
GROUP BY n.TipoRol
ORDER BY n.TipoRol;

SELECT CONCAT('=== PRUEBA COMPLETADA - Rendición ID: ', @RendicionId, ' ===') AS Info; 