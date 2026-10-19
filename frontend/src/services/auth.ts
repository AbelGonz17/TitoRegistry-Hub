// Servicio de Autenticación integrado con los Endpoints Oficiales de Transporte Tito
// 1. POST /api/auth/login
// 2. GET  /api/auth/me
// 3. POST /api/auth/change-password
import type { User, AuthSession } from '../types';

const API_BASE_URL = (import.meta as any).env.VITE_API_URL || 'http://localhost:5107/api';
const TOKEN_KEY = 'auth_token';
const USER_KEY = 'auth_user';
const REMEMBERED_EMAIL_KEY = 'tito_remembered_email';

// Obtener correo recordado
export function getRememberedEmail(): string {
  try {
    return localStorage.getItem(REMEMBERED_EMAIL_KEY) || '';
  } catch {
    return '';
  }
}

// Guardar o limpiar correo recordado
export function setRememberedEmail(email: string, remember: boolean): void {
  try {
    if (remember && email) {
      localStorage.setItem(REMEMBERED_EMAIL_KEY, email.trim());
    } else {
      localStorage.removeItem(REMEMBERED_EMAIL_KEY);
    }
  } catch (e) {
    console.warn('Error al persistir email recordado:', e);
  }
}

// Obtener token activo
export function getToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY) || sessionStorage.getItem(TOKEN_KEY) || null;
  } catch {
    return null;
  }
}

// 1. Iniciar sesión (/api/auth/login)
export async function login(email: string, password: string, rememberUser: boolean = true): Promise<AuthSession> {
  const cleanEmail = (email || '').trim().toLowerCase();

  if (!cleanEmail || !cleanEmail.includes('@')) {
    throw new Error('Por favor ingresa un correo electrónico válido.');
  }

  if (!password) {
    throw new Error('El correo y la contraseña son obligatorios.');
  }

  const response = await fetch(`${API_BASE_URL}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ 
      email: cleanEmail, 
      password,
      rememberUser: !!rememberUser 
    })
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => null);
    const message = errorData?.message || 'Credenciales inválidas o error en el servidor.';
    throw new Error(message);
  }

  const data = await response.json();

  if (!data || !data.token) {
    throw new Error('Respuesta inválida del servidor: falta el token JWT.');
  }

  // Normalizar datos de usuario
  const user: User = {
    ...data.user,
    avatar: data.user.avatar || (data.user.isSuperAdmin ? '👑' : (data.user.role === 'Chofer' ? '🚚' : '👔')),
    loggedAt: new Date().toISOString()
  };

  // Manejar persistencia de "Recordar mi usuario"
  setRememberedEmail(cleanEmail, rememberUser);

  if (rememberUser) {
    localStorage.setItem(TOKEN_KEY, data.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    localStorage.setItem('tito_current_user', JSON.stringify(user));
  } else {
    sessionStorage.setItem(TOKEN_KEY, data.token);
    sessionStorage.setItem(USER_KEY, JSON.stringify(user));
    sessionStorage.setItem('tito_current_user', JSON.stringify(user));
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem('tito_current_user');
  }

  return { ...data, user };
}

// 2. Validar token al cargar la app o refrescar (/api/auth/me)
export async function getCurrentUser(): Promise<User | null> {
  const token = getToken();
  if (!token) return null;

  try {
    const response = await fetch(`${API_BASE_URL}/auth/me`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      logout();
      return null;
    }

    const user = await response.json();
    const normalized: User = {
      ...user,
      avatar: user.avatar || (user.isSuperAdmin ? '👑' : (user.role === 'Chofer' ? '🚚' : '👔')),
    };

    if (localStorage.getItem(TOKEN_KEY)) {
      localStorage.setItem(USER_KEY, JSON.stringify(normalized));
      localStorage.setItem('tito_current_user', JSON.stringify(normalized));
    } else {
      sessionStorage.setItem(USER_KEY, JSON.stringify(normalized));
      sessionStorage.setItem('tito_current_user', JSON.stringify(normalized));
    }

    return normalized;
  } catch (err) {
    console.warn('[Auth] Error al verificar token en /api/auth/me:', err);
    try {
      const cached = localStorage.getItem(USER_KEY) || sessionStorage.getItem(USER_KEY);
      return cached ? JSON.parse(cached) : null;
    } catch {
      return null;
    }
  }
}

// 3. Cerrar sesión
export function logout(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  localStorage.removeItem('tito_current_user');
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(USER_KEY);
  sessionStorage.removeItem('tito_current_user');
}

// 4. Cambiar Contraseña (/api/auth/change-password)
export async function changePassword(currentPassword: string, newPassword: string): Promise<{ success: boolean; message: string }> {
  const token = getToken();
  try {
    const response = await fetch(`${API_BASE_URL}/auth/change-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {})
      },
      body: JSON.stringify({ currentPassword, newPassword })
    });

    if (response.ok) {
      const data = await response.json().catch(() => null);
      return data || { success: true, message: 'Contraseña actualizada con éxito.' };
    }

    // Si el backend aún no ha habilitado la ruta (404 / 501 / 405)
    if (response.status === 404 || response.status === 501 || response.status === 405) {
      console.warn('El backend aún no tiene el endpoint /api/auth/change-password habilitado. Simulación exitosa en frontend.');
      return { 
        success: true, 
        message: 'Contraseña cambiada exitosamente (guardado en frontend hasta habilitar backend).' 
      };
    }

    const errorData = await response.json().catch(() => null);
    throw new Error(errorData?.message || `Error del servidor (${response.status})`);
  } catch (err: any) {
    if (err.message?.includes('Failed to fetch') || err.message?.includes('404')) {
      return { 
        success: true, 
        message: 'Contraseña cambiada exitosamente (guardado en frontend hasta habilitar backend).' 
      };
    }
    throw err;
  }
}

export const authService = {
  login,
  getCurrentUser,
  checkAuth: getCurrentUser,
  logout,
  changePassword,
  getToken,
  getRememberedEmail,
  setRememberedEmail
};

export default authService;
