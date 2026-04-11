# 🔐 Sistema de Autorización Basado en Menús

## 📋 Cómo Funciona

El nuevo sistema de autorización se basa en:
1. Usuario tiene **Roles** (tabla `user_roles`)
2. Roles tienen acceso a **Menús** (tabla `menu_role`)
3. Menús tienen **Claims** (campos `claim_action` y `claim_subject` en `menu_item`)
4. Los endpoints verifican si el usuario tiene acceso al claim requerido

**Flujo de autorización:**
```
Usuario -> Roles -> Menús Asignados -> Claims -> Acceso a Endpoints
```

---

## 🎯 Uso en Controllers

### Opción 1: Usar `[MenuAuthorize]` con Action y Subject

```csharp
using FeborBack.Api.Authorization;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // Solo usuarios con acceso al menú que tiene claim_action="create" y claim_subject="users"
    [HttpPost]
    [MenuAuthorize("create", "users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // Lógica de creación
        return Ok();
    }

    // Solo usuarios con acceso al menú que tiene claim_action="read" y claim_subject="users"
    [HttpGet]
    [MenuAuthorize("read", "users")]
    public async Task<IActionResult> GetAllUsers()
    {
        // Lógica de consulta
        return Ok();
    }

    // Solo usuarios con acceso al menú que tiene claim_action="update" y claim_subject="users"
    [HttpPut("{id}")]
    [MenuAuthorize("update", "users")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        // Lógica de actualización
        return Ok();
    }

    // Solo usuarios con acceso al menú que tiene claim_action="delete" y claim_subject="users"
    [HttpDelete("{id}")]
    [MenuAuthorize("delete", "users")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        // Lógica de eliminación
        return Ok();
    }
}
```

### Opción 2: Usar `[MenuKeyAuthorize]` con el menu_key

```csharp
using FeborBack.Api.Authorization;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    // Solo usuarios que tengan acceso al menú con menu_key="reports-view"
    [HttpGet]
    [MenuKeyAuthorize("reports-view")]
    public async Task<IActionResult> GetReports()
    {
        return Ok();
    }

    // Solo usuarios que tengan acceso al menú con menu_key="reports-export"
    [HttpGet("export")]
    [MenuKeyAuthorize("reports-export")]
    public async Task<IActionResult> ExportReports()
    {
        return Ok();
    }
}
```

### Opción 3: Aplicar a toda la clase

```csharp
[ApiController]
[Route("api/[controller]")]
[MenuAuthorize("manage", "admin-panel")]  // Todos los endpoints requieren este claim
public class AdminController : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        return Ok();
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings()
    {
        return Ok();
    }
}
```

---

## 📊 Ejemplo Completo: Gestión de Usuarios

### 1. Configurar los Menús en la Base de Datos

```sql
-- Menú principal "Usuarios"
INSERT INTO menu.menu_item (title, menu_key, route_path, icon, claim_action, claim_subject, order_index, created_by)
VALUES ('Usuarios', 'users', '/users', 'tabler-users', 'read', 'users', 0, 1);

-- Submenú "Crear Usuario"
INSERT INTO menu.menu_item (parent_id, title, menu_key, route_path, claim_action, claim_subject, order_index, created_by)
VALUES (
    (SELECT menu_item_id FROM menu.menu_item WHERE menu_key = 'users'),
    'Crear Usuario',
    'users-create',
    '/users/create',
    'create',
    'users',
    0,
    1
);

-- Asignar menús al rol "Administrador"
INSERT INTO menu.menu_role (menu_item_id, role_id, assigned_by)
SELECT menu_item_id, 1, 1  -- role_id 1 = Administrador
FROM menu.menu_item
WHERE menu_key IN ('users', 'users-create');
```

### 2. Crear el Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeborBack.Api.Authorization;
using FeborBack.Application.Services.User;

namespace FeborBack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // Requiere autenticación JWT
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public UsersController(IUserManagementService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Listar usuarios - Requiere menú con claim "read users"
    /// </summary>
    [HttpGet]
    [MenuAuthorize("read", "users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(new { success = true, data = users });
    }

    /// <summary>
    /// Crear usuario - Requiere menú con claim "create users"
    /// </summary>
    [HttpPost]
    [MenuAuthorize("create", "users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var userId = await _userService.CreateUserAsync(dto);
        return Ok(new { success = true, userId });
    }

    /// <summary>
    /// Actualizar usuario - Requiere menú con claim "update users"
    /// </summary>
    [HttpPut("{id}")]
    [MenuAuthorize("update", "users")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        await _userService.UpdateUserAsync(id, dto);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Eliminar usuario - Requiere menú con claim "delete users"
    /// </summary>
    [HttpDelete("{id}")]
    [MenuAuthorize("delete", "users")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _userService.DeleteUserAsync(id);
        return Ok(new { success = true });
    }
}
```

---

## 🔍 Verificación Manual de Permisos

Si necesitas verificar permisos en código (fuera de attributes):

```csharp
using FeborBack.Application.Services.Authorization;

public class MiServicio
{
    private readonly IMenuAuthorizationService _authService;

    public MiServicio(IMenuAuthorizationService authService)
    {
        _authService = authService;
    }

    public async Task<bool> PuedeCrearUsuarios(int userId)
    {
        return await _authService.UserHasClaimAccessAsync(userId, "create", "users");
    }

    public async Task<bool> TieneAccesoAlMenu(int userId, string menuKey)
    {
        return await _authService.UserHasMenuKeyAccessAsync(userId, menuKey);
    }

    public async Task<IEnumerable<UserClaimDto>> ObtenerTodosLosPermisos(int userId)
    {
        return await _authService.GetUserClaimsAsync(userId);
    }
}
```

---

## 🚀 Instalación

### 1. Ejecutar el Script SQL

```cmd
cd C:\Users\Pc-bmx\Music\FeborBack\database
psql -U postgres -d FeborBASE -f 009_menu_authorization.sql
```

### 2. Compilar el Proyecto

```cmd
cd C:\Users\Pc-bmx\Music\FeborBack
dotnet build
```

### 3. Ejecutar la Aplicación

```cmd
cd FeborBack.Api
dotnet run
```

---

## ✅ Ventajas de este Sistema

1. **Gestión Visual**: Los permisos se asignan desde el administrador de menús
2. **Flexible**: Un usuario normal puede tener permisos específicos sin ser admin
3. **Granular**: Permisos por acción (create, read, update, delete) y recurso
4. **Dinámico**: Se pueden agregar/quitar permisos sin cambiar código
5. **Auditable**: Todas las asignaciones quedan registradas en `menu_role`

---

## 📖 Convenciones de Naming

### Actions (claim_action)
- `create` - Crear nuevos registros
- `read` - Ver/listar registros
- `update` - Modificar registros existentes
- `delete` - Eliminar registros
- `manage` - Gestión completa (CRUD)
- `export` - Exportar datos
- `import` - Importar datos
- `approve` - Aprobar/rechazar

### Subjects (claim_subject)
- `users` - Usuarios del sistema
- `roles` - Roles y permisos
- `reports` - Reportes
- `simulations` - Simulaciones de crédito
- `contact-requests` - Solicitudes de contacto
- `admin-panel` - Panel administrativo

### Menu Keys
Formato: `{subject}-{action}`
- `users-create`
- `users-read`
- `users-update`
- `users-delete`
- `reports-view`
- `reports-export`

---

## 🔧 Troubleshooting

### Error: "403 Forbidden"
El usuario no tiene acceso al claim requerido. Verifica:
1. El usuario tiene un rol asignado
2. El rol tiene acceso al menú correspondiente
3. El menú tiene el `claim_action` y `claim_subject` correctos

### Error: "Usuario no autenticado"
Falta el token JWT en el header `Authorization: Bearer {token}`

### Error: "No se pudo identificar al usuario"
El token JWT no contiene un `NameIdentifier` válido

---

¡Listo! Ahora tienes un sistema de autorización completo basado en menús. 🎉
