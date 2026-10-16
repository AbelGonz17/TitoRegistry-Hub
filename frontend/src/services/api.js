// Cliente API centralizado para TitoRegistry-Hub (Transporte Tito)
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5107/api';

async function request(endpoint, options = {}) {
  const url = `${API_BASE_URL}${endpoint}`;
  const config = {
    headers: {
      'Content-Type': 'application/json',
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
      const errorMessage = data?.message || `Error en la solicitud: ${response.status} ${response.statusText}`;
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

  // Empleados / Choferes
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
