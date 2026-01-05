-- Script para agregar las columnas nombre y apellidos a la tabla rendiciones
-- Ejecutar este script en la base de datos para actualizar la estructura

USE sistema_rendiciones;

-- Agregar columna nombre a la tabla rendiciones
ALTER TABLE rendiciones 
ADD COLUMN nombre VARCHAR(100) NOT NULL DEFAULT '' AFTER descripcion;

-- Agregar columna apellidos a la tabla rendiciones
ALTER TABLE rendiciones 
ADD COLUMN apellidos VARCHAR(100) NOT NULL DEFAULT '' AFTER nombre;

-- Actualizar las columnas existentes con datos de la tabla usuarios
UPDATE rendiciones r 
INNER JOIN usuarios u ON r.usuario_id = u.id 
SET r.nombre = u.nombre, r.apellidos = u.apellidos;

-- Verificar que las columnas se agregaron correctamente
DESCRIBE rendiciones; 