 1. Un usuario no puede modificar sus propios permisos
                                                                                                                                                  var currentUserId = _userContext.UserId; // o como lo expongas en IUserContext                                                                

  if (userDTO.IdUsuario == currentUserId)
  {
      var currentPerms = await _permitRepository.GetPermissionsUserAsync(userDTO.IdUsuario);
      bool permissionsChanged = userDTO.Permisos.Except(currentPerms).Any()
                             || currentPerms.Except(userDTO.Permisos).Any();

      if (permissionsChanged)
          return Result<bool>.Failure(UserErrorCode.cannot_modify_own_permissions);
  }

  ---
  2. Solo el admin maestro puede modificar los permisos del admin maestro

  var targetUser = await _userRepository.GetActiveByIdAsync(userDTO.IdUsuario);
  if (targetUser is null) return Result<bool>.Failure(UserErrorCode.user_not_found);

  var currentUserIsRoot = await _userRepository.IsRootAsync(currentUserId);

  if (targetUser.Root && !currentUserIsRoot)
  {
      var currentPerms = await _permitRepository.GetPermissionsUserAsync(userDTO.IdUsuario);
      bool permissionsChanged = userDTO.Permisos.Except(currentPerms).Any()
                             || currentPerms.Except(userDTO.Permisos).Any();

      if (permissionsChanged)
          return Result<bool>.Failure(UserErrorCode.cannot_modify_master_permissions);
  }

  ---
  3. El campo Root no debe ser modificable por la API

  UserUpdateDTO no debería tener el campo Root. Si lo tiene, ignoralo en el mapper o simplemente no lo incluyas en el DTO. El Root solo se toca 
  directo en la DB.

  ---
  Códigos de error a agregar

  public enum UserErrorCode
  {
      // ... existentes ...
      cannot_modify_own_permissions,
      cannot_modify_master_permissions,
  }

  Y sus mensajes en UserErrorDictionary.

  ---
  Dónde va en el flujo del UpdateAsync

  public async Task<Result<bool>> UpdateAsync(UserUpdateDTO userDTO)
  {
      using var transaction = ...

      var currentUserId = _userContext.UserId;

      // 1. Existe?
      bool exists = await _userRepository.ExistsActive(userDTO.IdUsuario);
      if (!exists) return Result<bool>.Failure(UserErrorCode.user_not_found);

      // 2. Validaciones de permisos
      var targetUser = await _userRepository.GetActiveByIdAsync(userDTO.IdUsuario);
      var currentUserIsRoot = await _userRepository.IsRootAsync(currentUserId);

      if (userDTO.IdUsuario == currentUserId || (targetUser.Root && !currentUserIsRoot))
      {
          var currentPerms = await _permitRepository.GetPermissionsUserAsync(userDTO.IdUsuario);
          bool permissionsChanged = userDTO.Permisos.Except(currentPerms).Any()
                                 || currentPerms.Except(userDTO.Permisos).Any();

          if (permissionsChanged)
          {
              return userDTO.IdUsuario == currentUserId
                  ? Result<bool>.Failure(UserErrorCode.cannot_modify_own_permissions)
                  : Result<bool>.Failure(UserErrorCode.cannot_modify_master_permissions);
          }
      }

      // 3. Username/email duplicados
      // ... resto del flujo existente
  }

  ---
  También vas a necesitar IsRootAsync en el repositorio:

  public async Task<bool> IsRootAsync(int userId)
  {
      return await _dbContext.Usuarios
          .AnyAsync(u => u.IdUsuario == userId && u.Root);
  }
  
  ---
   Bug fix
                                                                                                                                                - Rol pre-seleccionado en edición: se agregó key={rol-${form.idUsuario || "new"}} al <Select> de rol, forzando a Radix UI a remontarse al   
  cambiar de usuario y mostrando correctamente el rol actual.

  ---
  Permisos por acción (guards visuales)

  Cada acción del módulo ahora está protegida en el frontend:

  ┌─────────────────────────────┬─────────────────────┐
  │           Acción            │       Permiso       │
  ├─────────────────────────────┼─────────────────────┤
  │ Ver tabla / Tabs / Buscador │ USR_READ            │
  ├─────────────────────────────┼─────────────────────┤
  │ + Nuevo Usuario             │ USR_CREATE          │
  ├─────────────────────────────┼─────────────────────┤
  │ Editar usuario              │ USR_UPDATE          │
  ├─────────────────────────────┼─────────────────────┤
  │ Cambiar contraseña          │ USR_PASSWORD_UPDATE │
  ├─────────────────────────────┼─────────────────────┤
  │ Eliminar usuario            │ USR_DELETE          │
  ├─────────────────────────────┼─────────────────────┤
  │ Activar usuario             │ USR_DELETE          │
  └─────────────────────────────┴─────────────────────┘

  ---
  Admin maestro (Root)

  - Se agregó campo Root (bool) a la DB, entidad, UserDTO y LoginResponseDTO.
  - AuthService.js expone root en getCurrentUser().
  - Lógica getPermissionsLockReason() en UserPage determina si los permisos son editables:
    - Admin maestro → puede editar los permisos de cualquiera incluyendo los propios.
    - Otros usuarios → no pueden editar sus propios permisos ni los del admin maestro.
  - UserFormDrawer recibe permissionsLockReason (string o null): si viene con texto, muestra aviso amarillo y deshabilita los checkboxes con  
  el motivo exacto.

  ---
  Limpieza de permisos

  - Eliminado USR_ROLE_ASSIGN de permissions.js y de la DB — era redundante porque la asignación de permisos ya está embebida en crear/editar 
  usuario.
  - PASSWORD_UPDATE: "USR_PASSWORD_UPDATE" agregado al grupo USERS en permissions.js.

  ---
  Validaciones backend recomendadas (para implementar)

  En UpdateAsync:
  1. Si el usuario se edita a sí mismo y cambia sus permisos → rechazar con cannot_modify_own_permissions.
  2. Si el target es Root y el usuario logueado no lo es → rechazar cambios de permisos con cannot_modify_master_permissions.
  3. El campo Root no debe estar en UserUpdateDTO — solo se modifica directo en la DB.