-- Script para verificar notificaciones en el sistema
-- Ejecutar en SQL Server Management Studio o Azure Data Studio

-- 1. Verificar estructura de la tabla Notificaciones
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Notificaciones'
ORDER BY ORDINAL_POSITION;

-- 2. Verificar usuarios por rol
SELECT 
    Id,
    Nombre,
    Apellidos,
    Email,
    Rol,
    Activo
FROM Usuarios 
WHERE Activo = 1
ORDER BY Rol, Nombre;

-- 3. Verificar notificaciones existentes
SELECT 
    n.Id,
    n.UsuarioId,
    u.Nombre + ' ' + u.Apellidos AS Usuario,
    u.Rol AS RolUsuario,
    n.RendicionId,
    r.NumeroTicket,
    n.Mensaje,
    n.TipoRol,
    n.Leido,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
LEFT JOIN Rendiciones r ON n.RendicionId = r.Id
ORDER BY n.FechaCreacion DESC;

-- 4. Contar notificaciones por rol
SELECT 
    n.TipoRol,
    COUNT(*) AS TotalNotificaciones,
    COUNT(CASE WHEN n.Leido = 1 THEN 1 END) AS Leidas,
    COUNT(CASE WHEN n.Leido = 0 THEN 1 END) AS NoLeidas
FROM Notificaciones n
GROUP BY n.TipoRol
ORDER BY n.TipoRol;

-- 5. Verificar rendiciones recientes
SELECT 
    r.Id,
    r.NumeroTicket,
    r.Titulo,
    r.Estado,
    r.FechaCreacion,
    u.Nombre + ' ' + u.Apellidos AS Empleado,
    u.Rol AS RolEmpleado
FROM Rendiciones r
INNER JOIN Usuarios u ON r.UsuarioId = u.Id
ORDER BY r.FechaCreacion DESC;

-- 6. Verificar notificaciones por usuario específico
SELECT 
    n.Id,
    n.Mensaje,
    n.TipoRol,
    n.Leido,
    n.FechaCreacion,
    r.NumeroTicket
FROM Notificaciones n
LEFT JOIN Rendiciones r ON n.RendicionId = r.Id
WHERE n.UsuarioId = 1  -- Cambiar por el ID del usuario que quieras verificar
ORDER BY n.FechaCreacion DESC;

-- 7. Verificar notificaciones no leídas
SELECT 
    n.Id,
    u.Nombre + ' ' + u.Apellidos AS Usuario,
    u.Rol AS RolUsuario,
    n.Mensaje,
    n.TipoRol,
    n.FechaCreacion
FROM Notificaciones n
INNER JOIN Usuarios u ON n.UsuarioId = u.Id
WHERE n.Leido = 0
ORDER BY n.FechaCreacion DESC; 