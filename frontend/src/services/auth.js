// Servicio de Autenticación y Gestión de Usuarios para Transporte Tito
const USERS_STORAGE_KEY = 'tito_registered_users';
const CURRENT_USER_KEY = 'tito_current_user';
const REMEMBERED_EMAIL_KEY = 'tito_remembered_email';

export const authService = {
  // Obtener correo recordado
  getRememberedEmail() {
    try {
      return localStorage.getItem(REMEMBERED_EMAIL_KEY) || '';
    } catch {
      return '';
    }
  },

  // Guardar o limpiar correo recordado
  setRememberedEmail(email, remember) {
    try {
      if (remember && email) {
        localStorage.setItem(REMEMBERED_EMAIL_KEY, email.trim());
      } else {
        localStorage.removeItem(REMEMBERED_EMAIL_KEY);
      }
    } catch (e) {
      console.warn('Error al persistir email recordado:', e);
    }
  },

  // Obtener todos los usuarios registrados
  getUsers() {
    try {
      const stored = localStorage.getItem(USERS_STORAGE_KEY);
      if (!stored) {
        return [];
      }
      const parsed = JSON.parse(stored);
      return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
      console.warn('Error al leer usuarios de localStorage:', e);
      return [];
    }
  },

  // Obtener usuario con sesión activa
  getCurrentUser() {
    try {
      const stored = localStorage.getItem(CURRENT_USER_KEY) || sessionStorage.getItem(CURRENT_USER_KEY);
      if (stored) {
        return JSON.parse(stored);
      }
      return null;
    } catch (e) {
      console.warn('Error al leer sesión actual:', e);
      return null;
    }
  },

  // Iniciar sesión con email y contraseña
  // Si es la primera vez que se ingresa al sistema y no hay SuperAdmin, se crea automáticamente como SuperAdmin
  login(email, password, rememberUser = true) {
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        const users = this.getUsers();
        const cleanEmail = (email || '').trim().toLowerCase();

        if (!cleanEmail || !cleanEmail.includes('@')) {
          return reject(new Error('Por favor ingresa un correo electrónico válido.'));
        }

        if (!password || password.length < 4) {
          return reject(new Error('Por favor ingresa tu contraseña.'));
        }

        const user = users.find(u => u.email.toLowerCase() === cleanEmail);

        // Si aún no existe el usuario
        if (!user) {
          const hasSuperAdmin = users.some(u => u.isSuperAdmin);

          // Si el sistema no tiene SuperAdmin, este primer login lo registra automáticamente como SuperAdmin
          if (!hasSuperAdmin || users.length === 0) {
            const displayName = cleanEmail.split('@')[0]
              .replace(/[._-]/g, ' ')
              .replace(/\b\w/g, l => l.toUpperCase()) || 'Super Administrador';

            const newSuperAdmin = {
              id: 1,
              code: 'ADM-001',
              fullName: displayName,
              email: cleanEmail,
              password: password,
              phone: '',
              role: 'SuperAdmin',
              department: 'Dirección General & Propietario',
              commissionRate: 0,
              isSuperAdmin: true,
              avatar: '👑',
              createdAt: new Date().toISOString(),
              isActive: true
            };

            const updatedUsers = [...users, newSuperAdmin];
            localStorage.setItem(USERS_STORAGE_KEY, JSON.stringify(updatedUsers));
            this.setRememberedEmail(cleanEmail, rememberUser);

            const sessionUser = {
              id: newSuperAdmin.id,
              code: newSuperAdmin.code,
              fullName: newSuperAdmin.fullName,
              email: newSuperAdmin.email,
              phone: newSuperAdmin.phone,
              role: newSuperAdmin.role,
              department: newSuperAdmin.department,
              commissionRate: 0,
              isSuperAdmin: true,
              avatar: newSuperAdmin.avatar,
              loggedAt: new Date().toISOString(),
              isFirstSuperAdmin: true
            };

            if (rememberUser) {
              localStorage.setItem(CURRENT_USER_KEY, JSON.stringify(sessionUser));
            } else {
              sessionStorage.setItem(CURRENT_USER_KEY, JSON.stringify(sessionUser));
            }

            return resolve(sessionUser);
          }

          return reject(new Error('Credenciales incorrectas o usuario no registrado.'));
        }

        if (user.password && user.password !== password) {
          return reject(new Error('Contraseña incorrecta. Verifique sus datos e intente nuevamente.'));
        }

        // Manejar persistencia de "Recordar mi usuario"
        this.setRememberedEmail(cleanEmail, rememberUser);

        // Crear objeto de sesión
        const sessionUser = {
          id: user.id,
          code: user.code,
          fullName: user.fullName,
          email: user.email,
          phone: user.phone || '',
          role: user.role || 'SuperAdmin',
          department: user.department || 'Operaciones',
          commissionRate: user.commissionRate ?? 0,
          isSuperAdmin: user.isSuperAdmin ?? (user.role === 'SuperAdmin'),
          avatar: user.avatar || (user.isSuperAdmin ? '👑' : '👤'),
          loggedAt: new Date().toISOString()
        };

        if (rememberUser) {
          localStorage.setItem(CURRENT_USER_KEY, JSON.stringify(sessionUser));
        } else {
          sessionStorage.setItem(CURRENT_USER_KEY, JSON.stringify(sessionUser));
          localStorage.removeItem(CURRENT_USER_KEY);
        }

        resolve(sessionUser);
      }, 350);
    });
  },

  // Creación de Administradores o Choferes (usado internamente por el SuperAdmin)
  createAccount(accountData) {
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        const users = this.getUsers();
        const cleanEmail = (accountData.email || '').trim().toLowerCase();

        if (!accountData.fullName || accountData.fullName.trim().length < 3) {
          return reject(new Error('El nombre debe tener al menos 3 caracteres.'));
        }

        if (!cleanEmail || !cleanEmail.includes('@')) {
          return reject(new Error('Correo electrónico inválido.'));
        }

        const existing = users.find(u => u.email.toLowerCase() === cleanEmail);
        if (existing) {
          return reject(new Error('Ya existe una cuenta con este correo electrónico.'));
        }

        const maxId = users.reduce((max, u) => (u.id > max ? u.id : max), 0);
        const newId = maxId + 1;
        const isDriver = accountData.role === 'Chofer' || accountData.role === 'Conductor';
        const prefix = isDriver ? 'CHO' : 'ADM';
        const generatedCode = `${prefix}-${String(newId).padStart(3, '0')}`;

        const newAccount = {
          id: newId,
          code: generatedCode,
          fullName: accountData.fullName.trim(),
          email: cleanEmail,
          password: accountData.password || '123456',
          phone: accountData.phone ? accountData.phone.trim() : '',
          role: accountData.role || 'Conductor',
          department: accountData.department || (isDriver ? 'Flota de Rutas' : 'Administración'),
          commissionRate: Number(accountData.commissionRate) || (isDriver ? 25 : 0),
          isSuperAdmin: false,
          avatar: isDriver ? '🚚' : '👔',
          createdAt: new Date().toISOString(),
          isActive: true
        };

        const updatedUsers = [...users, newAccount];
        localStorage.setItem(USERS_STORAGE_KEY, JSON.stringify(updatedUsers));
        resolve(newAccount);
      }, 300);
    });
  },

  // Cerrar sesión
  logout() {
    localStorage.removeItem(CURRENT_USER_KEY);
    sessionStorage.removeItem(CURRENT_USER_KEY);
  }
};

export default authService;
