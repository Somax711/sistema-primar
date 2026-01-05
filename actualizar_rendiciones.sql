-- Script para actualizar la tabla rendiciones con información personal
-- Ejecutar en MySQL Workbench o consola MySQL

USE rendiciones_primar;

-- Agregar columnas de información personal a la tabla rendiciones
ALTER TABLE rendiciones 
ADD COLUMN nombre_completo VARCHAR(100) NOT NULL DEFAULT '' AFTER descripcion,
ADD COLUMN rut VARCHAR(20) NOT NULL DEFAULT '' AFTER nombre_completo,
ADD COLUMN telefono VARCHAR(20) NULL AFTER rut,
ADD COLUMN cargo VARCHAR(100) NULL AFTER telefono,
ADD COLUMN departamento VARCHAR(100) NULL AFTER cargo;

-- Actualizar registros existentes con información del usuario (si existe)
UPDATE rendiciones r 
INNER JOIN usuarios u ON r.usuario_id = u.id 
SET r.nombre_completo = COALESCE(u.nombre_completo, u.nombre),
    r.rut = COALESCE(u.rut, '00000000-0'),
    r.telefono = u.telefono,
    r.cargo = u.cargo,
    r.departamento = u.departamento
WHERE r.nombre_completo = '' OR r.rut = '';

-- Verificar que las columnas se agregaron correctamente
SELECT 'Tabla rendiciones actualizada exitosamente' AS Mensaje;
DESCRIBE rendiciones; 