-- Script compatible con versiones anteriores de MySQL
-- Ejecutar este script en MySQL Workbench o phpMyAdmin

USE rendiciones_primar;

-- Agregar columna para el código de recuperación (si no existe)
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'usuarios' 
     AND COLUMN_NAME = 'CodigoRecuperacion') = 0,
    'ALTER TABLE usuarios ADD COLUMN CodigoRecuperacion VARCHAR(6) NULL',
    'SELECT "CodigoRecuperacion ya existe" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Agregar columna para la expiración del código (si no existe)
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'usuarios' 
     AND COLUMN_NAME = 'CodigoRecuperacionExpira') = 0,
    'ALTER TABLE usuarios ADD COLUMN CodigoRecuperacionExpira DATETIME NULL',
    'SELECT "CodigoRecuperacionExpira ya existe" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar que se agregaron las columnas
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
AND TABLE_NAME = 'usuarios' 
AND COLUMN_NAME IN ('CodigoRecuperacion', 'CodigoRecuperacionExpira')
ORDER BY COLUMN_NAME; 