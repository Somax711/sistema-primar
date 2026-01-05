# 🏢 Sistema de Rendiciones Primar S.A. - Instrucciones Completas

## 📋 Configuración Inicial

### 1. Actualizar Base de Datos
Ejecuta estos scripts en MySQL en el siguiente orden:

```sql
-- 1. Actualizar estructura de tablas
SOURCE agregar_columnas_criticas.sql;

-- 2. Crear usuarios de prueba
SOURCE crear_usuarios_prueba.sql;
```

### 2. Configurar Emails (Opcional)
Edita `appsettings.json` para configurar el envío de emails:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "tu-email@gmail.com",
    "SmtpPassword": "tu-password",
    "FromEmail": "sistema@primar.cl",
    "FromName": "Sistema de Rendiciones"
  }
}
```

## 👥 Usuarios de Prueba

### Credenciales de Acceso:
- **Camila Flores** (Supervisora)
  - Email: `camila.flores@primar.cl`
  - Password: `password`
  - Rol: Aprobador 1

- **Don Juan** (Gerente)
  - Email: `don.juan@primar.cl`
  - Password: `password`
  - Rol: Aprobador 2

- **Catalina Núñez** (Empleada)
  - Email: `catalina.nunez@primar.cl`
  - Password: `password`
  - Rol: Empleado

## 🔄 Flujo de Trabajo

### 1. Empleado Crea Rendición
1. Inicia sesión como **Catalina Núñez**
2. Ve a "Rendiciones" → "Nueva Rendición"
3. Completa el formulario con:
   - Título: "Gastos de viaje a Santiago"
   - Descripción: "Viaje de trabajo para reunión con cliente"
   - Monto: $150,000
   - Archivos: Adjunta facturas/boletas
4. Haz clic en "Enviar Rendición"
5. ✅ Se genera número de ticket único
6. ✅ Se envía notificación a Camila Flores

### 2. Camila Flores Aprueba (Primera Instancia)
1. Inicia sesión como **Camila Flores**
2. Ve a "Notificaciones" → Verás la notificación nueva
3. Haz clic en "Ver rendición [NÚMERO]"
4. Revisa los detalles y archivos
5. Haz clic en "Aprobar" o "Rechazar"
6. ✅ Si aprueba: Estado cambia a "Aprobado 1°"
7. ✅ Se envía notificación a Don Juan

### 3. Don Juan Aprueba (Aprobación Final)
1. Inicia sesión como **Don Juan**
2. Ve a "Notificaciones" → Verás la notificación nueva
3. Haz clic en "Ver rendición [NÚMERO]"
4. Revisa los detalles
5. Haz clic en "Aprobación Final" o "Rechazar"
6. ✅ Si aprueba: Estado cambia a "Aprobado Final"
7. ✅ Se envía notificación a Camila para pago

### 4. Camila Marca como Pagada
1. Inicia sesión como **Camila Flores**
2. Ve a "Notificaciones" → Verás notificación de pago
3. Haz clic en "Ver rendición [NÚMERO]"
4. Haz clic en "Marcar como Pagada"
5. ✅ Estado cambia a "Pagado ✓✓"
6. ✅ Se envía notificación a Catalina

## 📊 Estados de Rendición

- ⏳ **Pendiente**: Esperando primera aprobación
- ✅ **Aprobado 1°**: Camila Flores aprobó
- ✅✅ **Aprobado Final**: Don Juan aprobó
- 💰 **Pagado ✓✓**: Ya se pagó (doble check)
- ❌ **Rechazado**: Fue rechazada por algún aprobador

## 🔔 Sistema de Notificaciones

### Notificaciones Automáticas:
- 📧 **Nueva rendición**: Camila Flores recibe notificación
- 📧 **Primera aprobación**: Don Juan recibe notificación
- 📧 **Aprobación final**: Camila recibe notificación para pago
- 📧 **Pago completado**: Empleado recibe notificación
- 📧 **Rechazo**: Empleado recibe notificación de rechazo

### Contador de Notificaciones:
- 🔴 El número rojo aparece en el menú
- ✅ Desaparece automáticamente al leer las notificaciones
- 📱 Se actualiza en tiempo real

## 🎯 Funcionalidades por Rol

### Empleados:
- ✅ Crear rendiciones
- ✅ Subir archivos adjuntos
- ✅ Ver estado de sus rendiciones
- ✅ Recibir notificaciones
- ✅ Ver historial completo

### Camila Flores (Aprobador 1):
- ✅ Ver rendiciones pendientes
- ✅ Aprobar/rechazar rendiciones
- ✅ Ver archivos adjuntos
- ✅ Marcar como pagadas
- ✅ Recibir notificaciones

### Don Juan (Aprobador 2):
- ✅ Ver rendiciones aprobadas por Camila
- ✅ Aprobación final
- ✅ Rechazar rendiciones
- ✅ Recibir notificaciones

## 🛠️ Solución de Problemas

### Error de Base de Datos:
```sql
-- Si hay errores de columnas faltantes
SOURCE agregar_columnas_criticas.sql;
```

### Error de Modelo:
```bash
# Limpiar y reconstruir
dotnet clean
dotnet build
```

### Notificaciones no aparecen:
1. Verifica que las tablas existan
2. Revisa la configuración de email
3. Verifica que los usuarios tengan el rol correcto

## 📱 Características del Sistema

### ✅ Implementado:
- 🔐 Autenticación y autorización
- 📄 Subida múltiple de archivos
- 🔢 Números de ticket únicos
- 📊 Dashboard personalizado por rol
- 🔔 Sistema de notificaciones
- 📧 Notificaciones por email
- 📱 Diseño responsive
- 🎨 Interfaz moderna y profesional
- 📈 Métricas y estadísticas
- 🔍 Búsqueda y filtros
- 📋 Historial completo

### 🚀 Próximas Mejoras:
- 📊 Reportes avanzados
- 📱 App móvil
- 🔗 Integración con sistemas contables
- 📧 Plantillas de email personalizables
- 🔐 Autenticación de dos factores

## 🎉 ¡Listo para Usar!

El sistema está completamente funcional y listo para manejar las rendiciones de los 40 empleados de Primar S.A. con el flujo de aprobación de Camila Flores → Don Juan → Pago. 