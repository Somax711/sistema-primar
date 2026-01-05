-- Script para agregar el campo TipoRol a la tabla notificaciones
-- Ejecutar este script para actualizar la estructura de la base de datos

USE rendiciones_primar;

-- Agregar el campo TipoRol a la tabla notificaciones
ALTER TABLE notificaciones 
ADD COLUMN tipo_rol VARCHAR(20) NOT NULL DEFAULT 'empleado' 
AFTER fecha_creacion;

-- Actualizar notificaciones existentes según el rol del usuario
-- Notificaciones para empleados (usuarios con rol 'empleado')
UPDATE notificaciones n 
JOIN usuarios u ON n.usuario_id = u.id 
SET n.tipo_rol = 'empleado' 
WHERE u.rol = 'empleado';

-- Notificaciones para supervisores (usuarios con rol 'aprobador1')
UPDATE notificaciones n 
JOIN usuarios u ON n.usuario_id = u.id 
SET n.tipo_rol = 'supervisor' 
WHERE u.rol = 'aprobador1';

-- Notificaciones para gerentes (usuarios con rol 'aprobador2')
UPDATE notificaciones n 
JOIN usuarios u ON n.usuario_id = u.id 
SET n.tipo_rol = 'gerente' 
WHERE u.rol = 'aprobador2';

-- Notificaciones para administradores (usuarios con rol 'admin')
UPDATE notificaciones n 
JOIN usuarios u ON n.usuario_id = u.id 
SET n.tipo_rol = 'admin' 
WHERE u.rol = 'admin';

-- Verificar la estructura actualizada
DESCRIBE notificaciones;

-- Mostrar algunos ejemplos de notificaciones con el nuevo campo
SELECT 
    n.id,
    n.usuario_id,
    u.nombre,
    u.rol,
    n.tipo_rol,
    n.mensaje,
    n.leido,
    n.fecha_creacion
FROM notificaciones n
JOIN usuarios u ON n.usuario_id = u.id
ORDER BY n.fecha_creacion DESC
LIMIT 10;

-- Contar notificaciones por tipo de rol
SELECT 
    tipo_rol,
    COUNT(*) as total_notificaciones,
    SUM(CASE WHEN leido = 0 THEN 1 ELSE 0 END) as no_leidas
FROM notificaciones 
GROUP BY tipo_rol; 