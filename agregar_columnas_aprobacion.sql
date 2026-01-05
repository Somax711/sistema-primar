-- Script para agregar columnas de aprobación faltantes a la tabla rendiciones
-- Ejecutar este script en MySQL para corregir el error "Unknown column 'r.AprobadoPor1'"

-- Agregar columnas de aprobación
ALTER TABLE rendiciones 
ADD COLUMN aprobado_por_1 INT NULL,
ADD COLUMN aprobado_por_2 INT NULL;

-- Agregar índices para mejorar el rendimiento
CREATE INDEX idx_rendiciones_aprobado_por_1 ON rendiciones(aprobado_por_1);
CREATE INDEX idx_rendiciones_aprobado_por_2 ON rendiciones(aprobado_por_2);

-- Actualizar registros existentes con valores por defecto
UPDATE rendiciones 
SET aprobado_por_1 = NULL, 
    aprobado_por_2 = NULL 
WHERE aprobado_por_1 IS NULL OR aprobado_por_2 IS NULL;

-- Verificar que las columnas se agregaron correctamente
DESCRIBE rendiciones; 