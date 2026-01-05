-- Script para actualizar la tabla usuarios con los nuevos campos
-- Ejecutar este script en la base de datos MySQL

USE rendiciones_primar;

-- Agregar las nuevas columnas a la tabla usuarios
ALTER TABLE usuarios 
ADD COLUMN nombre_completo VARCHAR(100) NOT NULL DEFAULT '' AFTER nombre,
ADD COLUMN rut VARCHAR(20) NOT NULL DEFAULT '' AFTER nombre_completo,
ADD COLUMN telefono VARCHAR(20) NULL AFTER email,
ADD COLUMN cargo VARCHAR(100) NULL AFTER rol,
ADD COLUMN departamento VARCHAR(100) NULL AFTER cargo;

-- Actualizar los registros existentes para que tengan valores por defecto
UPDATE usuarios SET 
nombre_completo = nombre,
rut = '00000000-0',
telefono = NULL,
cargo = NULL,
departamento = NULL
WHERE nombre_completo = '' OR rut = '';

-- Verificar que las columnas se agregaron correctamente
DESCRIBE usuarios;

-- Renombrar la columna nombre_completo a apellidos
ALTER TABLE usuarios CHANGE nombre_completo apellidos VARCHAR(100) NOT NULL DEFAULT ''; 