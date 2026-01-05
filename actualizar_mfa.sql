-- Script para agregar campos MFA (Multi-Factor Authentication) a la tabla usuarios
-- Ejecutar este script en la base de datos MySQL

USE rendiciones_primar;

-- Agregar columnas para MFA
ALTER TABLE usuarios 
ADD COLUMN mfa_secret_key VARCHAR(32) NULL COMMENT 'Clave secreta para TOTP',
ADD COLUMN mfa_habilitado BOOLEAN DEFAULT FALSE COMMENT 'Indica si MFA está habilitado para el usuario',
ADD COLUMN mfa_fecha_habilitacion DATETIME NULL COMMENT 'Fecha cuando se habilitó MFA',
ADD COLUMN mfa_codigo_verificacion VARCHAR(6) NULL COMMENT 'Código de verificación temporal para MFA',
ADD COLUMN mfa_codigo_expira DATETIME NULL COMMENT 'Fecha de expiración del código de verificación';

-- Crear índices para mejorar el rendimiento
CREATE INDEX idx_usuarios_mfa_codigo ON usuarios(mfa_codigo_verificacion, mfa_codigo_expira);
CREATE INDEX idx_usuarios_email_activo ON usuarios(email, activo);

-- Verificar que las columnas se agregaron correctamente
DESCRIBE usuarios;

-- Mostrar información sobre los campos MFA
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'rendiciones_primar' 
AND TABLE_NAME = 'usuarios' 
AND COLUMN_NAME LIKE 'mfa%';

-- Limpiar códigos de verificación expirados (opcional)
UPDATE usuarios 
SET mfa_codigo_verificacion = NULL, 
    mfa_codigo_expira = NULL 
WHERE mfa_codigo_expira IS NOT NULL 
AND mfa_codigo_expira < NOW();

-- Mostrar estadísticas de usuarios con MFA
SELECT 
    COUNT(*) as total_usuarios,
    SUM(CASE WHEN mfa_habilitado = 1 THEN 1 ELSE 0 END) as usuarios_con_mfa,
    SUM(CASE WHEN mfa_habilitado = 0 OR mfa_habilitado IS NULL THEN 1 ELSE 0 END) as usuarios_sin_mfa
FROM usuarios 
WHERE activo = 1; 