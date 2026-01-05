# INSTRUCCIONES PARA ESTRUCTURA REAL DE BASE DE DATOS

## Estructura Actual Identificada

Tu base de datos tiene la siguiente estructura:

### Tabla `usuarios`:
- `id` (int, PK, AI)
- `nombre` (varchar(100))
- `apellido` (varchar(100)) ← **SINGULAR, no plural**
- `email` (varchar(150))
- `password` (varchar(255))
- `tipo_usuario` (int) ← **No es 'rol'**
- `activo` (tinyint(1))
- `fecha_creacion` (datetime)
- `nombre_completo` (varchar(100))
- `telefono` (varchar(20))
- `cargo` (varchar(100))
- `departamento` (varchar(100))

## Problemas Identificados

1. **Campo RUT faltante** en el formulario de creación de rendiciones
2. **Estructura de base de datos diferente** a la esperada
3. **Mapeo de roles** necesario (tipo_usuario → rol)

## Scripts Creados Específicamente para tu Estructura

### 1. `test_estructura_real.sql` - Diagnóstico
```sql
-- Ejecutar para verificar la estructura actual
SOURCE test_estructura_real.sql;
```

### 2. `actualizar_estructura_real.sql` - Actualización
```sql
-- Ejecutar para actualizar la estructura
SOURCE actualizar_estructura_real.sql;
```

## Pasos para Solucionar el Problema

### Paso 1: Ejecutar diagnóstico
```sql
USE sistema_rendiciones;
SOURCE test_estructura_real.sql;
```

### Paso 2: Ejecutar actualización
```sql
USE sistema_rendiciones;
SOURCE actualizar_estructura_real.sql;
```

### Paso 3: Verificar mapeo de roles
El script mapeará automáticamente:
- `tipo_usuario = 1` → `rol = 'empleado'`
- `tipo_usuario = 2` → `rol = 'aprobador1'`
- `tipo_usuario = 3` → `rol = 'aprobador2'`
- `tipo_usuario = 4` → `rol = 'admin'`

### Paso 4: Reiniciar aplicación
```bash
dotnet run
```

### Paso 5: Probar creación de rendiciones
1. Iniciar sesión como usuario con `tipo_usuario = 1`
2. Ir a "Crear Rendición"
3. Completar todos los campos obligatorios
4. Hacer clic en "Enviar Rendición"

## Cambios Realizados en el Código

### ✅ Formulario de Creación (`Views/Rendiciones/Crear.cshtml`)
- Agregado campo RUT faltante
- Mejorado JavaScript para evitar errores

### ✅ ViewModel (`Models/ViewModels/CrearRendicionViewModel.cs`)
- Corregidas validaciones para ser consistentes
- Agregadas validaciones Required faltantes

### ✅ Scripts de Base de Datos
- `test_estructura_real.sql` - Diagnóstico específico
- `actualizar_estructura_real.sql` - Actualización compatible

## Verificación del Funcionamiento

### ✅ Indicadores de éxito:
1. El formulario se envía sin errores de validación
2. Se muestra: "Rendición [NÚMERO] creada exitosamente"
3. Se redirige a la lista de rendiciones
4. La rendición aparece en estado "pendiente"
5. Se crea una notificación para el aprobador

### ❌ Si persiste el problema:
1. Verificar que el usuario tenga `tipo_usuario = 1`
2. Verificar que exista un usuario con `tipo_usuario = 2` (aprobador1)
3. Ejecutar `test_estructura_real.sql` para diagnosticar
4. Verificar logs de la aplicación

## Campos Obligatorios en el Formulario

| Campo | Estado | Validación |
|-------|--------|------------|
| Nombre | ✅ Obligatorio | Required + StringLength(100) |
| Apellidos | ✅ Obligatorio | Required + StringLength(100) |
| RUT | ✅ Obligatorio | Required + StringLength(20) |
| Título | ✅ Obligatorio | Required + StringLength(200) |
| Descripción | ✅ Obligatorio | Required + StringLength(1000) |
| Monto Total | ✅ Obligatorio | Required + Range(0, 9999999999) |
| Teléfono | ⚪ Opcional | StringLength(20) |
| Cargo | ⚪ Opcional | StringLength(100) |
| Departamento | ⚪ Opcional | StringLength(100) |
| Archivos | ⚪ Opcional | Múltiples archivos permitidos |

## Notas Importantes

- **Base de datos**: Tu estructura usa `apellido` (singular) y `tipo_usuario` (int)
- **Roles**: Se mapean automáticamente de `tipo_usuario` a `rol`
- **Permisos**: El usuario debe tener `tipo_usuario = 1` para crear rendiciones
- **Aprobadores**: Debe existir un usuario con `tipo_usuario = 2` para aprobar
- **Archivos**: La carpeta `wwwroot/uploads` debe tener permisos de escritura

## Solución de Errores Específicos

### Error: "Unknown column 'apellidos' in 'field list'"
- **Causa**: Tu tabla usa `apellido` (singular)
- **Solución**: El script `actualizar_estructura_real.sql` maneja esto

### Error: "Unknown column 'rol' in 'field list'"
- **Causa**: Tu tabla usa `tipo_usuario` (int)
- **Solución**: El script agrega la columna `rol` y la mapea automáticamente

### Error: "You have an error in your SQL syntax near 'IF NOT EXISTS'"
- **Causa**: Tu versión de MySQL no soporta esta sintaxis
- **Solución**: Los scripts usan verificaciones dinámicas compatibles

---

**Estado**: ✅ ADAPTADO A TU ESTRUCTURA REAL
**Fecha**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss") 