// Cliente API centralizado y tipado para TitoRegistry-Hub (Transporte Tito)
import type { User, Service, ServiceType, EmployeeSummary } from '../types';

const API_BASE_URL = (import.meta as any).env.VITE_API_URL || 'http://localhost:5107/api';

interface RequestOptions extends RequestInit {
  headers?: Record<string, string>;
}

async function request<T = any>(endpoint: string, options: RequestOptions = {}): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;
  const token = localStorage.getItem('auth_token') || sessionStorage.getItem('auth_token');

  const config: RequestOptions = {
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
      return null as any;
    }

    const data = await response.json().catch(() => null);

    if (!response.ok) {
      if (response.status === 401) {
        localStorage.removeItem('auth_token');
        sessionStorage.removeItem('auth_token');
      }
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
  getStatus: () => request<{ status: string }>('/status'),

  // Autenticación (/api/auth)
  login: (credentials: { email: string; password: string }) => request('/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials),
  }),
  getMe: () => request<User>('/auth/me'),

  // Gestión de Usuarios (/api/users) - Admins & Choferes
  getUsers: (params: { role?: string; search?: string; onlyActive?: boolean } = {}): Promise<User[]> => {
    const query = new URLSearchParams();
    if (params.role) query.append('role', params.role);
    if (params.search) query.append('search', params.search);
    if (params.onlyActive !== undefined) query.append('onlyActive', String(params.onlyActive));
    const queryString = query.toString() ? `?${query.toString()}` : '';
    return request<User[]>(`/users${queryString}`);
  },
  getUserById: (id: number | string): Promise<User> => request<User>(`/users/${id}`),
  createUser: (payload: Partial<User>): Promise<User> => request<User>('/users', {
    method: 'POST',
    body: JSON.stringify(payload),
  }),
  updateUser: (id: number | string, payload: Partial<User>): Promise<User> => request<User>(`/users/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  }),
  deleteUser: (id: number | string): Promise<void> => request(`/users/${id}`, {
    method: 'DELETE',
  }),

  // Empleados / Choferes (integrado con /api/users)
  getEmployees: async (): Promise<User[]> => {
    const users = await request<User[]>('/users');
    const drivers = (users || []).filter(u => u.role === 'Chofer');
    return drivers.length > 0 ? drivers : (users || []);
  },
  getEmployeeSummary: async (id: number | string): Promise<EmployeeSummary | null> => {
    const now = new Date();
    try {
      const settlement = await request<any>(`/settlements/calculate?driverId=${id}&month=${now.getMonth() + 1}&year=${now.getFullYear()}`);
      
      const rate = settlement.driverCommissionRate || 25;
      const tripCommissionEarned = (settlement.services || []).reduce((sum: number, s: any) => {
        return sum + ((Number(s.priceCharged) || 0) * (rate / 100));
      }, 0);

      return {
        id: settlement.driverId,
        code: settlement.driverCode,
        fullName: settlement.driverName,
        role: 'Chofer',
        department: 'Operaciones',
        commissionRate: rate,
        totalEarningsAccumulated: settlement.totalNetPayable,
        tripCommissionEarned,
        settlementCommission: settlement.commissionAmount,
        totalServicesCompleted: settlement.services?.length || 0,
        totalBilled: settlement.totalGrossAmount,
        averageEarningsPerService: settlement.services?.length > 0 
          ? Math.round(settlement.totalGrossAmount / settlement.services.length) 
          : 0,
        recentServices: settlement.services || [],
        settlementBreakdown: {
          grossBilled: settlement.totalGrossAmount || 0,
          companyRetentionRate: settlement.companyRetentionRate || 0,
          companyRetentionAmount: settlement.companyRetentionAmount || 0,
          operationalSubTotal: settlement.operationalSubTotal || 0,
          commissionAmount: settlement.commissionAmount || 0,
          tripCommissionEarned,
          phoneIncentive: settlement.phoneIncentive || 0,
          otherIncentives: settlement.otherIncentives || 0,
          tssDeduction: settlement.tssDeduction || 0,
          isrDeduction: settlement.isrDeduction || 0,
          otherDeductions: settlement.otherDeductions || 0,
          totalNetPayable: settlement.totalNetPayable || 0,
          monthName: settlement.monthName || 'Mes Actual',
          year: settlement.year || now.getFullYear()
        }
      };
    } catch {
      return null;
    }
  },

  // Servicios de Transporte Realizados (/api/services)
  getAllServices: (params: { driverId?: number | string; from?: string; to?: string; month?: number; year?: number } = {}): Promise<Service[]> => {
    const query = new URLSearchParams();
    if (params.driverId) query.append('driverId', String(params.driverId));
    if (params.from) query.append('from', params.from);
    if (params.to) query.append('to', params.to);
    if (params.month) query.append('month', String(params.month));
    if (params.year) query.append('year', String(params.year));
    const queryString = query.toString() ? `?${query.toString()}` : '';
    return request<Service[]>(`/services${queryString}`);
  },
  getServicesByEmployee: (employeeId?: number | string, params: { from?: string; to?: string } = {}): Promise<Service[]> => {
    const query = new URLSearchParams();
    if (employeeId) query.append('driverId', String(employeeId));
    if (params.from) query.append('from', params.from);
    if (params.to) query.append('to', params.to);
    const queryString = query.toString() ? `?${query.toString()}` : '';
    return request<Service[]>(`/services${queryString}`);
  },
  registerService: (payload: any): Promise<Service> => {
    const body = {
      driverId: payload.driverId || payload.employeeId,
      serviceTypeId: payload.serviceTypeId,
      clientReference: payload.clientReference || payload.clientName,
      description: payload.description || payload.clientAddress || `Servicio realizado para ${payload.clientName || 'cliente'}`,
      serviceDate: payload.serviceDate || new Date().toISOString(),
      priceCharged: Number(payload.priceCharged),
      notes: payload.notes || ''
    };
    return request<Service>('/services', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  // Catálogo Dinámico de Servicios (CRUD)
  getServiceTypes: (): Promise<ServiceType[]> => request<ServiceType[]>('/servicetypes'),
  getServiceTypeById: (id: number | string): Promise<ServiceType> => request<ServiceType>(`/servicetypes/${id}`),
  createServiceType: (payload: Partial<ServiceType>): Promise<ServiceType> => request<ServiceType>('/servicetypes', {
    method: 'POST',
    body: JSON.stringify(payload),
  }),
  updateServiceType: (id: number | string, payload: Partial<ServiceType>): Promise<ServiceType> => request<ServiceType>(`/servicetypes/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  }),
  softDeleteServiceType: (id: number | string): Promise<void> => request(`/servicetypes/${id}`, {
    method: 'DELETE',
  }),

  // Liquidaciones y Planilla Mensual (/api/settlements)
  calculateSettlement: (params: {
    driverId: number | string;
    month: number;
    year: number;
    companyRetentionRate?: number;
    driverCommissionRate?: number;
    isr?: number;
    tss?: number;
    phoneIncentive?: number;
    otherIncentives?: number;
    otherDeductions?: number;
  }): Promise<any> => {
    const query = new URLSearchParams();
    query.append('driverId', String(params.driverId));
    query.append('month', String(params.month));
    query.append('year', String(params.year));
    if (params.companyRetentionRate !== undefined) query.append('companyRetentionRate', String(params.companyRetentionRate));
    if (params.driverCommissionRate !== undefined) query.append('driverCommissionRate', String(params.driverCommissionRate));
    if (params.isr !== undefined) query.append('isr', String(params.isr));
    if (params.tss !== undefined) query.append('tss', String(params.tss));
    if (params.phoneIncentive !== undefined) query.append('phoneIncentive', String(params.phoneIncentive));
    if (params.otherIncentives !== undefined) query.append('otherIncentives', String(params.otherIncentives));
    if (params.otherDeductions !== undefined) query.append('otherDeductions', String(params.otherDeductions));
    return request<any>(`/settlements/calculate?${query.toString()}`);
  },
  saveSettlement: (payload: any): Promise<any> => request<any>('/settlements', {
    method: 'POST',
    body: JSON.stringify(payload),
  }),
  getSettlementHistory: (params: { driverId?: number | string; year?: number } = {}): Promise<any[]> => {
    const query = new URLSearchParams();
    if (params.driverId) query.append('driverId', String(params.driverId));
    if (params.year) query.append('year', String(params.year));
    const qs = query.toString() ? `?${query.toString()}` : '';
    return request<any[]>(`/settlements/history${qs}`);
  },
};

export default api;
