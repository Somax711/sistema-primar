-- Script simple para agregar columnas críticas faltantes
-- Ejecutar este script en la base de datos MySQL

-- Deshabilitar modo seguro temporalmente
SET SQL_SAFE_UPDATES = 0;

-- Agregar columnas críticas a la tabla rendiciones
ALTER TABLE rendiciones ADD COLUMN aprobador_1_id INT NULL;
ALTER TABLE rendiciones ADD COLUMN aprobador_2_id INT NULL;
ALTER TABLE rendiciones ADD COLUMN fecha_aprobacion_1 DATETIME NULL;
ALTER TABLE rendiciones ADD COLUMN fecha_aprobacion_2 DATETIME NULL;
ALTER TABLE rendiciones ADD COLUMN fecha_pago DATETIME NULL;
ALTER TABLE rendiciones ADD COLUMN comentarios_aprobador TEXT NULL;
ALTER TABLE rendiciones ADD COLUMN aprobado_por_1 INT NULL;
ALTER TABLE rendiciones ADD COLUMN aprobado_por_2 INT NULL;
ALTER TABLE rendiciones ADD COLUMN nombre_completo VARCHAR(100) NULL;
ALTER TABLE rendiciones ADD COLUMN rut VARCHAR(20) NULL;
ALTER TABLE rendiciones ADD COLUMN telefono VARCHAR(20) NULL;
ALTER TABLE rendiciones ADD COLUMN cargo VARCHAR(100) NULL;
ALTER TABLE rendiciones ADD COLUMN departamento VARCHAR(100) NULL;

-- Agregar columnas faltantes a la tabla usuarios
ALTER TABLE usuarios ADD COLUMN nombre_completo VARCHAR(100) NULL;
ALTER TABLE usuarios ADD COLUMN telefono VARCHAR(20) NULL;
ALTER TABLE usuarios ADD COLUMN cargo VARCHAR(100) NULL;
ALTER TABLE usuarios ADD COLUMN departamento VARCHAR(100) NULL;
ALTER TABLE usuarios ADD COLUMN activo BOOLEAN DEFAULT TRUE;
ALTER TABLE usuarios ADD COLUMN fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP;

-- Crear tablas si no existen
CREATE TABLE IF NOT EXISTS archivos_adjuntos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    rendicion_id INT NOT NULL,
    nombre_archivo VARCHAR(255) NOT NULL,
    ruta_archivo VARCHAR(500) NOT NULL,
    tipo_archivo VARCHAR(50) NULL,
    tamano_archivo BIGINT NULL,
    fecha_subida DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS notificaciones (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL,
    rendicion_id INT NULL,
    mensaje TEXT NOT NULL,
    leido BOOLEAN DEFAULT FALSE,
    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Habilitar modo seguro nuevamente
SET SQL_SAFE_UPDATES = 1;

-- Verificar estructura
DESCRIBE rendiciones; 