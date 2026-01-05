-- Script para agregar columnas de recuperación de contraseña
-- Ejecutar este script en la base de datos para habilitar la funcionalidad de recuperación

USE rendiciones_primar;

-- Verificar si la columna CodigoRecuperacion existe en la tabla usuarios
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'usuarios' 
     AND COLUMN_NAME = 'CodigoRecuperacion') = 0,
    'ALTER TABLE usuarios ADD COLUMN CodigoRecuperacion VARCHAR(6) NULL',
    'SELECT "CodigoRecuperacion ya existe en usuarios" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar si la columna CodigoRecuperacionExpira existe en la tabla usuarios
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'usuarios' 
     AND COLUMN_NAME = 'CodigoRecuperacionExpira') = 0,
    'ALTER TABLE usuarios ADD COLUMN CodigoRecuperacionExpira DATETIME NULL',
    'SELECT "CodigoRecuperacionExpira ya existe en usuarios" as mensaje'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificar que las columnas se agregaron correctamente
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE, 
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
AND TABLE_NAME = 'usuarios' 
AND COLUMN_NAME IN ('CodigoRecuperacion', 'CodigoRecuperacionExpira')
ORDER BY COLUMN_NAME;

-- Mostrar mensaje de confirmación
SELECT 'Columnas de recuperación de contraseña agregadas exitosamente' as resultado; 