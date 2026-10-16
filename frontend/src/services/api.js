// Cliente API centralizado para TitoRegistry-Hub (Transporte Tito)
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5107/api';

async function request(endpoint, options = {}) {
  const url = `${API_BASE_URL}${endpoint}`;
  const token = localStorage.getItem('auth_token') || sessionStorage.getItem('auth_token');

  const config = {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
    ...options,
  };

  try {
    const response = await fetch(url, config);

    if (response.status === 204) {
      return null;
    }

    const data = await response.json().catch(() => null);

    if (!response.ok) {
      const errorMessage = data?.message || data?.title || `Error en la solicitud: ${response.status} ${response.statusText}`;
      throw new Error(errorMessage);
    }

    return data;
  } catch (error) {
    console.error(`[API Error] en ${endpoint}:`, error);
    throw error;
  }
}

export const api = {
  // Diagnóstico / Health Check
  getStatus: () => request('/status'),

  // Autenticación (/api/auth)
  login: (credentials) => request('/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials),
  }),
  getMe: () => request('/auth/me'),

  // Gestión de Usuarios (/api/users) - Admins & Choferes
  getUsers: (params = {}) => {
    const query = new URLSearchParams();
    if (params.role) query.append('role', params.role);
    if (params.search) query.append('search', params.search);
    if (params.onlyActive !== undefined) query.append('onlyActive', params.onlyActive);
    const queryString = query.toString() ? `?${query.toString()}` : '';
    return request(`/users${queryString}`);
  },
  getUserById: (id) => request(`/users/${id}`),
  createUser: (payload) => request('/users', {
    method: 'POST',
    body: JSON.stringify(payload),
  }),
  updateUser: (id, payload) => request(`/users/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  }),
  deleteUser: (id) => request(`/users/${id}`, {
    method: 'DELETE',
  }),

  // Empleados / Choferes (legacy / vista rápida)
  getEmployees: () => request('/employees'),
  getEmployeeSummary: (id) => request(`/employees/${id}/summary`),

  // Servicios de Transporte Realizados
  getServicesByEmployee: (employeeId, params = {}) => {
    const query = new URLSearchParams();
    if (params.from) query.append('from', params.from);
    if (params.to) query.append('to', params.to);
    const queryString = query.toString() ? `?${query.toString()}` : '';
    return request(`/services/employee/${employeeId}${queryString}`);
  },
  registerService: (payload) => request('/services', {
    method: 'POST',
    body: JSON.stringify(payload),
  }),

  // Catálogo Dinámico de Servicios (CRUD)
  getServiceTypes: () => request('/servicetypes'),
  getServiceTypeById: (id) => request(`/servicetypes/${id}`),
  createServiceType: (payload) => request('/servicetypes', {
    method: 'POST',
    body: JSON.stringify(payload),
  }),
  updateServiceType: (id, payload) => request(`/servicetypes/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  }),
  softDeleteServiceType: (id) => request(`/servicetypes/${id}`, {
    method: 'DELETE',
  }),
};

export default api;
