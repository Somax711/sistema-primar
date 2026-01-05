-- Script simple para agregar columnas de recuperación de contraseña
-- Ejecutar este script en MySQL Workbench o phpMyAdmin

USE rendiciones_primar;

-- Agregar columna para el código de recuperación
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS CodigoRecuperacion VARCHAR(6) NULL;

-- Agregar columna para la expiración del código
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS CodigoRecuperacionExpira DATETIME NULL;

-- Verificar que se agregaron las columnas
DESCRIBE usuarios; 