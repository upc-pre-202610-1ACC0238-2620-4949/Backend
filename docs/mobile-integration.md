# Integración con la app Android

Cliente de referencia: `FrancoDiegoR/Proyecto_avance`, servicios Retrofit de
`app/src/main/java/com/smartgas_frontend/*/data/remote`.

## Arranque local

1. Iniciar PostgreSQL con la conexión configurada en `appsettings.json`:
   base `smartgas_db`, puerto `5432`, usuario `smartgas_user` y contraseña
   de desarrollo `smartgas_password`.
2. Ejecutar `dotnet run --launch-profile http` en la carpeta del backend.
   El backend aplica las migraciones y crea los planes automáticamente.
   La nueva migración `AddSensorLocationDetail` añade una columna de texto
   con valor vacío para los sensores existentes.
3. Conectar un teléfono Android con depuración USB autorizada o iniciar
   el emulador. Ejecutar `adb reverse tcp:5048 tcp:5048`.
4. Iniciar la app. Su URL base es `http://localhost:5048/api/v1/`.
   Swagger local: `http://localhost:5048/swagger`.

## Endpoints usados por el frontend

Todas las rutas siguientes llevan el prefijo `/api/v1/`.
Las listas son arrays JSON y las operaciones individuales devuelven objetos JSON.

| Método | Ruta | Parámetros / cuerpo |
| --- | --- | --- |
| POST | auth/sign-in | email, password |
| POST | auth/sign-up | email, password, fullName, businessName, phone, district, role |
| GET | profiles/{accountId} | accountId en ruta |
| PATCH | profiles/{accountId} | fullName, businessName, phone, district, address |
| GET | settings/{accountId} | accountId en ruta |
| PATCH | settings/{accountId} | language, darkMode, notificationsEnabled, gasThreshold, temperatureThreshold |
| GET | emergency-contacts/{accountId} | accountId en ruta |
| PATCH | emergency-contacts/{accountId} | name, phone, email |
| GET | zones | accountId en query |
| POST | zones | accountId, name, description, sensitivity |
| GET | sensors | accountId en query |
| POST | sensors | accountId, zoneId, code, name, type; locationDetail opcional |
| PATCH | sensors/{id} | name, code, type, zoneId, locationDetail, status; todos opcionales |
| GET | sensor-readings | accountId en query |
| POST | sensor-readings | sensorCode, gasLevel, temperature |
| GET | incidents | accountId en query |
| PATCH | incidents/{id}/review | sin cuerpo |
| PATCH | incidents/{id}/resolve | sin cuerpo |
| PATCH | incidents/{id}/false-alarm | sin cuerpo |
| GET | alerts | accountId en query |
| GET | notifications | accountId en query |
| PATCH | notifications/{id}/read | sin cuerpo |
| PATCH | notifications/{id}/confirm | sin cuerpo |
| GET | plans | sin parámetros |
| GET | subscriptions/current/{accountId} | accountId en ruta |
| PATCH | subscriptions/current/{accountId}/change-plan | planId |
| GET | external/weather/current | latitude, longitude en query |

## Ajustes del contrato

- Los 27 endpoints Retrofit ya tenían sus rutas equivalentes en el backend.
- Autenticación: devuelve también `businessName` para conservar el nombre del
  negocio en la sesión. `accountId` identifica la cuenta.
- Perfil: devuelve `email`, `role` y `accountType` (`domestic` o `commercial`).
- Sensores: `locationDetail` se guarda y se devuelve al consultar, crear o editar.
  Omitirlo en PATCH conserva el valor; enviar una cadena vacía lo borra.
- Zonas: incluye `sensorCount`, últimas lecturas disponibles de gas y temperatura,
  `riskLevel` y `lastUpdated`. Sin lecturas, los valores numéricos son null.
- Lecturas: incluyen `sensorCode` además de `sensorId`.
- Incidentes: incluyen `gasLevel`, `temperature` y `updatedAt`, además del texto
  `detectedValue`. Se devuelve la fecha `detectedAt` real del incidente.
- Alertas: incluyen `zoneName` e `isResolved`, también para falsas alarmas.
- Notificaciones: el cliente admite los campos existentes `isRead` e
  `isConfirmed`; no requiere alias `read` o `confirmed`.

El frontend consulta notificaciones periódicamente; esta integración no introduce
un proveedor push. El frontend actual no envía `locationDetail` al crear un sensor,
pero sí al editarlo: el backend admite ambos casos. Tampoco consume endpoints para
recuperar contraseña o guardar notas de incidentes; esas pantallas siguen pendientes.

## Comprobación manual pendiente

1. Registrar una cuenta doméstica y otra comercial (`HomeOwner` y
   `RestaurantAdministrator`), e iniciar sesión con cada una.
2. Crear una zona y un sensor. Editar la ubicación del sensor y recargar para
   comprobar que persiste. Desactivarlo y reactivarlo.
3. Enviar una lectura con el código real del sensor. Con umbrales predeterminados,
   un sensor tipo Gas y `gasLevel: 78` debe generar un incidente crítico.
4. Revisar gas y temperatura en zona, sensor e historial. Verificar el nombre
   de zona en alertas, marcar una notificación leída y confirmarla.
5. Resolver el incidente o marcarlo como falsa alarma; recargar las alertas y
   verificar `isResolved: true` y las fechas de actualización.
6. Editar perfil, contacto, preferencias y umbrales; recargar para comprobarlos.
7. Consultar y cambiar el plan; comprobar los límites de zonas y sensores.
8. Consultar clima con Internet disponible y comprobar el modo offline de la app.

La revisión del contrato y la compilación no sustituyen estas pruebas con la app,
PostgreSQL y un dispositivo funcionando juntos.
