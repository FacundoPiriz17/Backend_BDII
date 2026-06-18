# Cambios en el backend (Backend_BDII - copia)

> Registro dedicado de los cambios de backend de esta sesión.
> (El `git diff` del sandbox no captura ediciones in-situ por una limitación del
>  FUSE; este manifiesto + el `CHANGELOG.md` de la raíz son el registro autoritativo.
>  Para el diff unificado real, correr `git diff` en la copia local.)

## Equipos / Infraestructura
- **`Modules/Infraestructura/Repositories/InfraestructuraRepository.cs`** — la query de
  equipos seleccionaba `escudo_url`, columna inexistente en el esquema → se quitó
  (ahora `SELECT codigo_fifa, nombre_equipo, grupo`).
- **`Modules/Infraestructura/DTOs/EquipoResponse.cs`** — se removió la propiedad
  `EscudoUrl`.

## Auth
- **`Modules/Auth/Controllers/AuthControler.cs`** — nuevos endpoints
  `POST /api/auth/refresh` (renovar JWT) y `POST /api/auth/cambiar-contrasena`.
- **`Modules/Auth/Services/IAuthService.cs` + `AuthService.cs`** — `RefreshAsync`
  (reemite token validando habilitación) y `CambiarContrasenaAsync` (verifica la
  actual, valida la nueva ≥6 y distinta).
- **`Modules/Auth/Repositories/IAuthRepository.cs` + `AuthRepository.cs`** —
  `ActualizarContrasenaAsync` (UPDATE login.contrasena).
- **`Modules/Auth/DTOs/CambiarContrasenaRequest.cs`** — NUEVO.

## Usuarios
- **`Modules/Usuarios/DTOs/ActualizarMiPerfilRequest.cs`** — NUEVO (edición propia:
  nombre, dirección, teléfonos; sin documento ni habilitación).
- **`Modules/Usuarios/Services/IUsuarioService.cs` + `UsuarioService.cs`**:
  - `ActualizarMiPerfilAsync` (self-service `PUT /api/usuarios/me`, preserva
    documento y habilitación).
  - El admin **no puede** cambiar sus propios roles ni deshabilitarse
    (guard en `ActualizarRolesAsync` y `ActualizarHabilitacionAsync`).
  - Al **crear** usuario, se rechaza más de un rol.
- **`Modules/Usuarios/Controllers/UsuariosController.cs`** — endpoint
  `PUT /api/usuarios/me`.

## Eventos
- **`Modules/Eventos/Repositories/IEventoRepository.cs` + `EventoRepository.cs`**:
  - `GetContextoEventoAsync` (+ record `EventoCreacionContexto`): país del admin,
    país del estadio y grupo de cada equipo en una sola consulta.
  - Búsqueda por **nombre** de selección (subconsulta `EXISTS` sobre `equipo`),
    además de código FIFA / estadio / ciudad.
- **`Modules/Eventos/Services/EventoService.cs`**:
  - Alta/edición valida **jurisdicción** (estadio del país sede del admin),
    **mismo grupo** en fase de grupos y **fecha no anterior** (alta).
  - Edición por estado: **terminado** no editable; **empezado** solo permite
    modificar el marcador.

## Entradas (seguridad / IDOR)
- **`Modules/Entradas/Repositories/EntradaRepository.cs`** — el detalle de entrada
  permitía verla a comprador original u origen/destino de una transferencia; ahora
  solo la ve el **propietario actual** (o admin/funcionario).

## Compras
- **`Modules/Compras/Repositories/CompraRepository.cs`**:
  - `GetMisEntradasAsync` tenía `e.estado='activa'` hardcodeado (rompía el filtro de
    estado) → se quitó; por defecto oculta canceladas y respeta el filtro.
  - Búsqueda de "mis entradas" por **nombre** de selección (join a `equipo`).

## Transferencias
- **`Modules/Transferencias/Repositories/TransferenciaRepository.cs`** — al crear, se
  rechaza transferir entradas de un partido **ya empezado o terminado** (además de
  las validaciones previas: propiedad, activa, restantes, compra paga, sin pendiente).

## Reportes / Auditoría
- **`Modules/Reportes/DTOs/AuditoriaEntradaResponse.cs`** — NUEVO.
- **`Modules/Reportes/Repositories/IReporteRepository.cs` + `ReporteRepository.cs`** —
  `GetAuditoriaAsync` (UNION de compras, transferencias y validaciones).
- **`Modules/Reportes/Services/IReporteService.cs` + `ReporteService.cs`** —
  `GetAuditoriaAsync` (valida `tipo`, limita el `limit`).
- **`Modules/Reportes/Controllers/ReportesController.cs`** — endpoint
  `GET /api/reportes/auditoria?tipo=&limit=` (solo Admin).
