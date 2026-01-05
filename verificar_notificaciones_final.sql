-- Script final para verificar notificaciones en MySQL
-- Usando los nombres exactos de las columnas de la base de datos

USE sistema_rendiciones;

-- 1. Verificar usuarios activos por rol
SELECT '=== USUARIOS ACTIVOS POR ROL ===' AS Info;
SELECT 
    id,
    CONCAT(nombre, ' ', apellido) AS NombreCompleto,
    email,
    rol,
    activo
FROM usuarios 
WHERE activo = 1
ORDER BY rol, nombre;

-- 2. Verificar rendiciones recientes
SELECT '=== RENDICIONES RECIENTES ===' AS Info;
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
ORDER BY r.fecha_creacion DESC
LIMIT 5;

-- 3. Verificar notificaciones existentes
SELECT '=== NOTIFICACIONES EXISTENTES ===' AS Info;
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
ORDER BY n.fecha_creacion DESC;

-- 4. Contar notificaciones por tipo de rol
SELECT '=== RESUMEN DE NOTIFICACIONES POR TIPO ===' AS Info;
SELECT 
    n.tipo_rol,
    COUNT(*) AS TotalNotificaciones,
    SUM(CASE WHEN n.leido = 1 THEN 1 ELSE 0 END) AS Leidas,
    SUM(CASE WHEN n.leido = 0 THEN 1 ELSE 0 END) AS NoLeidas
FROM notificaciones n
GROUP BY n.tipo_rol
ORDER BY n.tipo_rol;

-- 5. Verificar notificaciones no leídas
SELECT '=== NOTIFICACIONES NO LEÍDAS ===' AS Info;
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

-- 6. Verificar usuarios que deberían recibir notificaciones
SELECT '=== USUARIOS QUE DEBERÍAN RECIBIR NOTIFICACIONES ===' AS Info;
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

-- 7. Verificar notificaciones de las últimas 24 horas
SELECT '=== NOTIFICACIONES DE LAS ÚLTIMAS 24 HORAS ===' AS Info;
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

-- 8. Verificar mapeo de estados de rendiciones
SELECT '=== MAPEO DE ESTADOS DE RENDICIONES ===' AS Info;
SELECT 
    estado,
    COUNT(*) AS Cantidad,
    CASE 
        WHEN estado = 1 THEN 'Pendiente'
        WHEN estado = 2 THEN 'Aprobado Supervisor'
        WHEN estado = 3 THEN 'Aprobado Gerente'
        WHEN estado = 4 THEN 'Pagado'
        WHEN estado = 5 THEN 'Rechazado'
        ELSE CONCAT('Estado ', estado)
    END AS EstadoDescripcion
FROM rendiciones
GROUP BY estado
ORDER BY estado; 