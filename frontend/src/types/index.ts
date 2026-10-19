// Definiciones de tipos e interfaces TypeScript para TitoRegistry-Hub

export interface User {
  id: number;
  code?: string;
  fullName: string;
  email: string;
  phone?: string;
  role: 'Chofer' | 'Administrador' | 'SuperAdmin' | string;
  department?: string;
  commissionRate?: number;
  defaultPhoneIncentive?: number;
  defaultIsr?: number;
  defaultTss?: number;
  isActive?: boolean;
  isSuperAdmin?: boolean;
  avatar?: string;
  createdAt?: string;
  loggedAt?: string;
}

export interface ServiceType {
  id: number;
  code: string;
  name: string;
  category: string;
  basePrice: number;
  estimatedDuration?: string;
  routeZone?: string;
  description?: string;
  isActive: boolean;
  createdAt?: string;
}

export interface Service {
  id: number;
  serviceCode: string;
  driverId: number;
  employeeId?: number;
  driverName?: string;
  employeeName?: string;
  driverCode?: string;
  serviceTypeId: number;
  serviceTypeName?: string;
  serviceCategory?: string;
  clientReference?: string;
  clientName?: string;
  clientPhone?: string;
  clientAddress?: string;
  description?: string;
  serviceDate: string;
  priceCharged: number;
  employeeEarnings?: number;
  status?: number;
  statusName?: string;
  notes?: string;
  createdAt?: string;
}

export interface SettlementBreakdown {
  grossBilled: number;
  companyRetentionRate: number;
  companyRetentionAmount: number;
  operationalSubTotal: number;
  commissionAmount: number;
  tripCommissionEarned: number;
  phoneIncentive: number;
  otherIncentives: number;
  tssDeduction: number;
  isrDeduction: number;
  otherDeductions: number;
  totalNetPayable: number;
  monthName: string;
  year: number;
}

export interface EmployeeSummary {
  id?: number;
  code: string;
  fullName: string;
  role: string;
  department: string;
  commissionRate: number;
  totalEarningsAccumulated: number;
  tripCommissionEarned?: number;
  settlementCommission?: number;
  totalServicesCompleted: number;
  totalBilled: number;
  averageEarningsPerService: number;
  recentServices: Service[];
  settlementBreakdown?: SettlementBreakdown | null;
}

export interface AuthSession {
  token: string;
  expiration?: string;
  user: User;
}

export interface ToastNotification {
  show: boolean;
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
}
