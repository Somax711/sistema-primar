-- Script para verificar notificaciones en MySQL (CORREGIDO)
-- Ejecutar en MySQL Workbench o phpMyAdmin

USE sistema_rendiciones;

-- 1. Verificar estructura de la tabla Notificaciones
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'sistema_rendiciones' 
AND TABLE_NAME = 'Notificaciones'
ORDER BY ORDINAL_POSITION;

-- 2. Verificar usuarios por rol
SELECT 
    id,
    nombre,
    apellido,
    email,
    rol,
    activo
FROM usuarios 
WHERE activo = 1
ORDER BY rol, nombre;

-- 3. Verificar notificaciones existentes
SELECT 
    n.id,
    n.usuario_id,
    CONCAT(u.nombre, ' ', u.apellido) AS Usuario,
    u.rol AS RolUsuario,
    n.rendicion_id,
    r.numero_ticket,
    n.mensaje,
    n.tipo_rol,
    n.leido,
    n.fecha_creacion
FROM notificaciones n
INNER JOIN usuarios u ON n.usuario_id = u.id
LEFT JOIN rendiciones r ON n.rendicion_id = r.id
ORDER BY n.fecha_creacion DESC;

-- 4. Contar notificaciones por rol
SELECT 
    n.tipo_rol,
    COUNT(*) AS TotalNotificaciones,
    SUM(CASE WHEN n.leido = 1 THEN 1 ELSE 0 END) AS Leidas,
    SUM(CASE WHEN n.leido = 0 THEN 1 ELSE 0 END) AS NoLeidas
FROM notificaciones n
GROUP BY n.tipo_rol
ORDER BY n.tipo_rol;

-- 5. Verificar rendiciones recientes
SELECT 
    r.id,
    r.numero_ticket,
    r.titulo,
    r.estado,
    r.fecha_creacion,
    CONCAT(u.nombre, ' ', u.apellido) AS Empleado,
    u.rol AS RolEmpleado
FROM rendiciones r
INNER JOIN usuarios u ON r.usuario_id = u.id
ORDER BY r.fecha_creacion DESC;

-- 6. Verificar notificaciones por usuario específico
SELECT 
    n.id,
    n.mensaje,
    n.tipo_rol,
    n.leido,
    n.fecha_creacion,
    r.numero_ticket
FROM notificaciones n
LEFT JOIN rendiciones r ON n.rendicion_id = r.id
WHERE n.usuario_id = 1  -- Cambiar por el ID del usuario que quieras verificar
ORDER BY n.fecha_creacion DESC;

-- 7. Verificar notificaciones no leídas
SELECT 
    n.id,
    CONCAT(u.nombre, ' ', u.apellido) AS Usuario,
    u.rol AS RolUsuario,
    n.mensaje,
    n.tipo_rol,
    n.fecha_creacion
FROM notificaciones n
INNER JOIN usuarios u ON n.usuario_id = u.id
WHERE n.leido = 0
ORDER BY n.fecha_creacion DESC;

-- 8. Verificar usuarios por rol específico
SELECT 
    'Supervisores' AS Tipo,
    id,
    CONCAT(nombre, ' ', apellido) AS NombreCompleto,
    email
FROM usuarios 
WHERE rol = 'aprobador1' AND activo = 1
UNION ALL
SELECT 
    'Gerentes' AS Tipo,
    id,
    CONCAT(nombre, ' ', apellido) AS NombreCompleto,
    email
FROM usuarios 
WHERE rol = 'aprobador2' AND activo = 1
UNION ALL
SELECT 
    'Empleados' AS Tipo,
    id,
    CONCAT(nombre, ' ', apellido) AS NombreCompleto,
    email
FROM usuarios 
WHERE rol = 'empleado' AND activo = 1
ORDER BY Tipo, NombreCompleto;

-- 9. Verificar si hay notificaciones duplicadas
SELECT 
    usuario_id,
    rendicion_id,
    tipo_rol,
    COUNT(*) AS Cantidad
FROM notificaciones
GROUP BY usuario_id, rendicion_id, tipo_rol
HAVING COUNT(*) > 1
ORDER BY Cantidad DESC;

-- 10. Verificar notificaciones de las últimas 24 horas
SELECT 
    n.id,
    CONCAT(u.nombre, ' ', u.apellido) AS Usuario,
    u.rol AS RolUsuario,
    n.mensaje,
    n.tipo_rol,
    n.leido,
    n.fecha_creacion
FROM notificaciones n
INNER JOIN usuarios u ON n.usuario_id = u.id
WHERE n.fecha_creacion >= DATE_SUB(NOW(), INTERVAL 24 HOUR)
ORDER BY n.fecha_creacion DESC; 