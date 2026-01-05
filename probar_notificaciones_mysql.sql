-- Script de prueba para notificaciones en MySQL
-- Ejecutar paso a paso para verificar el flujo

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

-- PASO 2: Verificar rendiciones existentes
SELECT '=== RENDICIONES EXISTENTES ===' AS Info;
SELECT 
    Id,
    NumeroTicket,
    Titulo,
    Estado,
    FechaCreacion,
    UsuarioId
FROM Rendiciones 
ORDER BY FechaCreacion DESC
LIMIT 5;

-- PASO 3: Verificar notificaciones existentes
SELECT '=== NOTIFICACIONES EXISTENTES ===' AS Info;
SELECT 
    n.Id,
    CONCAT(u.Nombre, ' ', u.Apellidos) AS Usuario,
    u.Rol AS RolUsuario,
    n.Mensaje,
    n.TipoRol,
    n.Leido,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
ORDER BY n.FechaCreacion DESC
LIMIT 10;

-- PASO 4: Crear una rendición de prueba manualmente
-- (Ejecutar solo si quieres crear una rendición de prueba)
/*
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
    CONCAT('RND-TEST-', DATE_FORMAT(NOW(), '%Y%m%d-%H%i%s')),
    Id,
    'Rendición de Prueba - Notificaciones',
    'Esta es una rendición de prueba para verificar el sistema de notificaciones',
    150000,
    'pendiente',
    NOW(),
    Nombre,
    Apellidos,
    Rut,
    Telefono,
    Cargo,
    Departamento
FROM Usuarios 
WHERE Rol = 'empleado' AND Activo = 1
LIMIT 1;
*/

-- PASO 5: Simular notificación al supervisor
-- (Ejecutar solo si quieres crear notificaciones de prueba)
/*
INSERT INTO Notificaciones (UsuarioId, RendicionId, Mensaje, Leido, FechaCreacion, TipoRol)
SELECT 
    u.Id,
    (SELECT MAX(Id) FROM Rendiciones),
    CONCAT('Nueva rendición de prueba requiere tu aprobación.'),
    0,
    NOW(),
    'supervisor'
FROM Usuarios u 
WHERE u.Rol = 'aprobador1' AND u.Activo = 1;
*/

-- PASO 6: Verificar notificaciones después de la prueba
SELECT '=== NOTIFICACIONES DESPUÉS DE LA PRUEBA ===' AS Info;
SELECT 
    n.Id,
    CONCAT(u.Nombre, ' ', u.Apellidos) AS Usuario,
    u.Rol AS RolUsuario,
    n.Mensaje,
    n.TipoRol,
    n.Leido,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
WHERE n.FechaCreacion >= DATE_SUB(NOW(), INTERVAL 1 HOUR)
ORDER BY n.FechaCreacion DESC;

-- PASO 7: Contar notificaciones por tipo
SELECT '=== RESUMEN DE NOTIFICACIONES ===' AS Info;
SELECT 
    n.TipoRol,
    COUNT(*) AS TotalNotificaciones,
    SUM(CASE WHEN n.Leido = 1 THEN 1 ELSE 0 END) AS Leidas,
    SUM(CASE WHEN n.Leido = 0 THEN 1 ELSE 0 END) AS NoLeidas
FROM Notificaciones n
GROUP BY n.TipoRol
ORDER BY n.TipoRol;

-- PASO 8: Verificar usuarios que deberían recibir notificaciones
SELECT '=== USUARIOS QUE DEBERÍAN RECIBIR NOTIFICACIONES ===' AS Info;
SELECT 
    'Supervisores' AS Tipo,
    CONCAT(Nombre, ' ', Apellidos) AS NombreCompleto,
    Email
FROM Usuarios 
WHERE Rol = 'aprobador1' AND Activo = 1
UNION ALL
SELECT 
    'Gerentes' AS Tipo,
    CONCAT(Nombre, ' ', Apellidos) AS NombreCompleto,
    Email
FROM Usuarios 
WHERE Rol = 'aprobador2' AND Activo = 1
UNION ALL
SELECT 
    'Empleados' AS Tipo,
    CONCAT(Nombre, ' ', Apellidos) AS NombreCompleto,
    Email
FROM Usuarios 
WHERE Rol = 'empleado' AND Activo = 1
ORDER BY Tipo, NombreCompleto; 