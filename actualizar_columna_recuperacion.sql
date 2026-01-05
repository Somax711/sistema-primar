-- Script para actualizar la columna CodigoRecuperacion para tokens de 32 caracteres
-- Ejecutar este script en MySQL Workbench o phpMyAdmin

USE rendiciones_primar;

-- Actualizar la columna CodigoRecuperacion para que pueda almacenar tokens de 32 caracteres
ALTER TABLE usuarios MODIFY COLUMN CodigoRecuperacion VARCHAR(64) NULL;

-- Verificar que se actualizó correctamente
DESCRIBE usuarios; 