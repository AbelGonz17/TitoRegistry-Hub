<script setup>
import { ref, computed } from 'vue';

const props = defineProps({
  currentUser: {
    type: Object,
    default: () => ({ fullName: 'Super Administrador', role: 'SuperAdmin', email: 'admin@titotransporte.com' })
  },
  employees: {
    type: Array,
    default: () => []
  },
  users: {
    type: Array,
    default: () => []
  },
  allServices: {
    type: Array,
    default: () => []
  },
  serviceTypes: {
    type: Array,
    default: () => []
  },
  driverSummaries: {
    type: Array,
    default: () => []
  },
  backendOnline: Boolean,
  loading: Boolean
});

const emit = defineEmits(['open-new-service', 'inspect-driver', 'change-tab', 'refresh']);

// Filtro rápido de actividad reciente
const searchQuery = ref('');
const timeFilter = ref('todos'); // 'todos' | 'hoy' | 'semana' | 'mes'

// Métricas de Rendimiento Global de la Empresa
const totalFleetBilled = computed(() => {
  return props.allServices.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0);
});

// Margen oficial de Transporte Tito: 30% del bruto total
const companyMargin = computed(() => {
  return totalFleetBilled.value * 0.30;
});

// Subtotal operativo asignado a servicios: 70%
const operationalSubTotal = computed(() => {
  return totalFleetBilled.value * 0.70;
});

// Comisiones acumuladas de los choferes
const totalDriverCommissions = computed(() => {
  if (props.driverSummaries.length > 0) {
    return props.driverSummaries.reduce((sum, d) => sum + (Number(d.driverCommissionAmount) || (Number(d.totalBilled) * 0.70 * 0.25) || 0), 0);
  }
  return props.allServices.reduce((sum, s) => sum + (Number(s.employeeEarnings) || 0), 0);
});

// Total de Retenciones Fiscales (TSS + ISR) de la flota
const totalDeductions = computed(() => {
  return props.driverSummaries.reduce((sum, d) => {
    const isr = Number(d.isrDeduction) || 0;
    const tss = Number(d.tssDeduction) || 0;
    return sum + isr + tss;
  }, 0);
});

// Total Neto Pagable a todos los choferes
const totalNetPayableDrivers = computed(() => {
  return props.driverSummaries.reduce((sum, d) => sum + (Number(d.totalEarningsAccumulated) || Number(d.totalNetPayable) || 0), 0);
});

// Servicios filtrados para el Live Feed
const filteredRecentServices = computed(() => {
  let list = [...props.allServices];

  // Ordenar los más recientes primero
  list.sort((a, b) => new Date(b.serviceDate || b.createdAt) - new Date(a.serviceDate || a.createdAt));

  const now = new Date();
  if (timeFilter.value === 'hoy') {
    const todayStr = now.toISOString().slice(0, 10);
    list = list.filter(s => (s.serviceDate || '').slice(0, 10) === todayStr);
  } else if (timeFilter.value === 'semana') {
    const weekAgo = new Date();
    weekAgo.setDate(now.getDate() - 7);
    list = list.filter(s => new Date(s.serviceDate) >= weekAgo);
  } else if (timeFilter.value === 'mes') {
    const monthAgo = new Date();
    monthAgo.setMonth(now.getMonth() - 1);
    list = list.filter(s => new Date(s.serviceDate) >= monthAgo);
  }

  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim();
    list = list.filter(s => 
      (s.clientName || '').toLowerCase().includes(q) ||
      (s.driverName || '').toLowerCase().includes(q) ||
      (s.serviceTypeName || '').toLowerCase().includes(q) ||
      (s.serviceCode || '').toLowerCase().includes(q)
    );
  }

  return list;
});

// Resumen comparativo de la flota de choferes
const fleetDriversTable = computed(() => {
  const drivers = (props.employees.length > 0 ? props.employees : props.users).filter(u => u.role === 'Chofer');
  
  return drivers.map(d => {
    // Buscar resumen calculado
    const summary = props.driverSummaries.find(s => s.id === d.id);
    const driverServices = props.allServices.filter(s => s.driverId === d.id || s.employeeId === d.id);
    const billed = summary ? summary.totalBilled : driverServices.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0);
    const comp30 = billed * 0.30;
    const comm = summary ? (summary.driverCommissionAmount || (billed * 0.70 * 0.25)) : (billed * 0.70 * 0.25);
    const net = summary ? summary.totalEarningsAccumulated : comm;
    const trips = summary ? summary.totalServicesCompleted : driverServices.length;

    return {
      id: d.id,
      code: d.code || `CHO-${d.id}`,
      fullName: d.fullName,
      email: d.email,
      department: d.department || 'Operaciones',
      commissionRate: d.commissionRate || 25,
      totalServices: trips,
      totalBilled: billed,
      companyMargin: comp30,
      driverCommission: comm,
      netPayable: net,
      avatar: d.avatar || '🚐'
    };
  });
});

const formatDate = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('es-DO', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  });
};
</script>

<template>
  <div class="superadmin-dashboard">
    <!-- 1. HERO EJECUTIVO Y LEAD DEVELOPER -->
    <div class="dev-hero glass-card">
      <div class="hero-main-row">
        <div class="dev-profile-cluster">
          <div class="dev-avatar-ring">
            <span class="avatar-icon">👑</span>
            <span class="status-indicator-live" :class="{ online: backendOnline }"></span>
          </div>

          <div class="dev-text-group">
            <div class="role-badge-pill-row">
              <span class="role-badge-primary">SUPER ADMIN & LEAD DEVELOPER</span>
              <span :class="['api-status-chip', { online: backendOnline }]">
                {{ backendOnline ? '🟢 .NET 10 API Online' : '🔴 API Desconectada' }}
              </span>
              <span class="env-chip desktop-only">⚡ v1.0.0 Clean Architecture</span>
            </div>

            <h1 class="dev-title">
              Panel de Control de <span class="highlight-name">{{ currentUser.fullName || 'Tito González' }}</span>
            </h1>

            <p class="dev-tagline">
              Centro de monitoreo global de facturación, liquidaciones de choferes y rendimiento de Transporte Tito en tiempo real.
            </p>
          </div>
        </div>

        <div class="dev-actions-cluster">
          <button class="btn btn-primary" @click="$emit('open-new-service')">
            <span class="btn-icon">+</span>
            <span>Registrar Viaje</span>
          </button>
          <button class="btn btn-secondary" @click="$emit('change-tab', 'users')">
            <span class="btn-icon">👥</span>
            <span>Personal</span>
          </button>
          <button class="btn btn-outline" @click="$emit('refresh')" title="Recargar métricas">
            <span class="btn-icon">🔄</span>
          </button>
        </div>
      </div>
    </div>

    <!-- 2. CUADRÍCULA DE MÉTRICAS FINANCIERAS CLAVE (KPIs DEL NEGOCIO) -->
    <div class="kpi-grid">
      <!-- KPI 1: Facturación Bruta Total de la Flota -->
      <div class="kpi-card glass-card">
        <div class="kpi-header">
          <span class="kpi-label">Facturación Bruta Total</span>
          <span class="kpi-icon">💵</span>
        </div>
        <div class="kpi-body">
          <span class="kpi-currency">RD$</span>
          <span class="kpi-number">{{ totalFleetBilled.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</span>
        </div>
        <div class="kpi-footer">
          <span class="trend-badge positive">100% Servicios</span>
          <span class="kpi-desc">Volumen bruto generado por la flota</span>
        </div>
      </div>

      <!-- KPI 2: Margen Transporte Tito (30% Empresa) - DESTACADA EN ESMERALDA -->
      <div class="kpi-card glass-card highlight-card">
        <div class="kpi-header">
          <span class="kpi-label">Margen Empresa (30%)</span>
          <span class="kpi-icon">🏢</span>
        </div>
        <div class="kpi-body highlight-text">
          <span class="kpi-currency">RD$</span>
          <span class="kpi-number">{{ companyMargin.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</span>
        </div>
        <div class="kpi-footer">
          <span class="badge-company">Retención Oficial</span>
          <span class="kpi-desc">Beneficio operativo neto de la empresa</span>
        </div>
      </div>

      <!-- KPI 3: Comisiones a Choferes (25% sobre Subtotal) -->
      <div class="kpi-card glass-card">
        <div class="kpi-header">
          <span class="kpi-label">Comisiones de Choferes</span>
          <span class="kpi-icon">🚐</span>
        </div>
        <div class="kpi-body">
          <span class="kpi-currency">RD$</span>
          <span class="kpi-number">{{ totalDriverCommissions.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</span>
        </div>
        <div class="kpi-footer">
          <span class="trend-badge neutral">25% Bruto</span>
          <span class="kpi-desc">Fondo asignado a conductores</span>
        </div>
      </div>

      <!-- KPI 4: Retenciones de Ley (TSS + ISR) -->
      <div class="kpi-card glass-card">
        <div class="kpi-header">
          <span class="kpi-label">Retenciones Ley (TSS / ISR)</span>
          <span class="kpi-icon">⚖️</span>
        </div>
        <div class="kpi-body">
          <span class="kpi-currency">RD$</span>
          <span class="kpi-number">{{ totalDeductions.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</span>
        </div>
        <div class="kpi-footer">
          <span class="badge-deduction">Fiscal RD</span>
          <span class="kpi-desc">TSS (5.91%) e ISR de nómina</span>
        </div>
      </div>

      <!-- KPI 5: Viajes Totales Operados -->
      <div class="kpi-card glass-card">
        <div class="kpi-header">
          <span class="kpi-label">Viajes Totales Realizados</span>
          <span class="kpi-icon">🛣️</span>
        </div>
        <div class="kpi-body">
          <span class="kpi-number count">{{ allServices.length }}</span>
        </div>
        <div class="kpi-footer">
          <span class="badge-green">Actividad Total</span>
          <span class="kpi-desc">Servicios registrados en el hub</span>
        </div>
      </div>
    </div>

    <!-- 3. PANEL DE CONTROL DE SERVIDOR & ESTADO TÉCNICO (DEV CONSOLE) -->
    <div class="devops-console glass-card">
      <div class="console-header">
        <div class="console-title-group">
          <span class="console-icon">💻</span>
          <h2 class="console-title">Centro de Salud del Sistema (Developer Console)</h2>
        </div>
        <span class="console-badge">ENTORNO LOCAL ACTIVO</span>
      </div>

      <div class="console-grid">
        <div class="console-item">
          <span class="item-title">Backend API</span>
          <span class="item-val font-mono">.NET 10 Web API • C# Clean Architecture</span>
          <span class="item-status status-good">✓ Endpoints REST Saludables</span>
        </div>

        <div class="console-item">
          <span class="item-title">Base de Datos</span>
          <span class="item-val font-mono">PostgreSQL / EF Core 10</span>
          <span class="item-status status-good">✓ 8 Entidades Sincronizadas</span>
        </div>

        <div class="console-item">
          <span class="item-title">Seguridad & Sesiones</span>
          <span class="item-val font-mono">HMAC-SHA256 JWT (Expiración: 7d)</span>
          <span class="item-status status-good">✓ Roles: SuperAdmin, Admin, Chofer</span>
        </div>

        <div class="console-item">
          <span class="item-title">Catálogo & Rutas</span>
          <span class="item-val font-mono">{{ serviceTypes.length }} Rutas Activas</span>
          <span class="item-status status-info">Urbano, Interurbano, Aeropuerto</span>
        </div>
      </div>
    </div>

    <!-- 4. TABLA COMPARATIVA DE RENDIMIENTO DE LA FLOTA -->
    <div class="fleet-section glass-card">
      <div class="section-top-bar">
        <div class="title-cluster">
          <h2 class="section-heading">Desglose de Rendimiento por Chofer</h2>
          <span class="badge-counter">{{ fleetDriversTable.length }} Conductores</span>
        </div>
        <p class="section-subtext">Resumen consolidado de facturación, comisiones y liquidación neta por cada chofer.</p>
      </div>

      <div class="table-responsive">
        <table class="superadmin-table">
          <thead>
            <tr>
              <th>Chofer</th>
              <th>Departamento / Zona</th>
              <th class="text-center">Viajes</th>
              <th class="text-right">Total Bruto Cobrado</th>
              <th class="text-right">Empresa (30%)</th>
              <th class="text-right">Comisión Chofer (25%)</th>
              <th class="text-right">Neto Estimado</th>
              <th class="text-center">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="driver in fleetDriversTable" :key="driver.id" class="driver-row">
              <td>
                <div class="driver-identity-cell">
                  <span class="driver-table-avatar">{{ driver.avatar }}</span>
                  <div class="driver-names">
                    <strong class="driver-full-name">{{ driver.fullName }}</strong>
                    <span class="driver-code-tag">{{ driver.code }} (Tasa: {{ driver.commissionRate }}%)</span>
                  </div>
                </div>
              </td>
              <td>
                <span class="driver-dept-badge">{{ driver.department }}</span>
              </td>
              <td class="text-center">
                <span class="trips-pill">{{ driver.totalServices }}</span>
              </td>
              <td class="text-right font-mono font-bold">
                RD$ {{ driver.totalBilled.toLocaleString('es-DO', { minimumFractionDigits: 2 }) }}
              </td>
              <td class="text-right font-mono text-emerald">
                RD$ {{ driver.companyMargin.toLocaleString('es-DO', { minimumFractionDigits: 2 }) }}
              </td>
              <td class="text-right font-mono text-cyan">
                RD$ {{ driver.driverCommission.toLocaleString('es-DO', { minimumFractionDigits: 2 }) }}
              </td>
              <td class="text-right font-mono font-bold text-accent">
                RD$ {{ driver.netPayable.toLocaleString('es-DO', { minimumFractionDigits: 2 }) }}
              </td>
              <td class="text-center">
                <button 
                  class="btn-inspect-driver"
                  @click="$emit('inspect-driver', driver.id)"
                  title="Auditar liquidación completa del chofer"
                >
                  <span>🔍</span>
                  <span>Auditar Chofer</span>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 5. FEED EN VIVO DE ÚLTIMOS VIAJES REGISTRADOS EN TODA LA FLOTA -->
    <div class="activity-feed-section glass-card">
      <div class="feed-header-row">
        <div class="feed-title-wrap">
          <h2 class="section-heading">Actividad de Viajes en Tiempo Real</h2>
          <span class="badge-counter">{{ filteredRecentServices.length }} Viajes Registrados</span>
        </div>

        <div class="feed-controls">
          <!-- Filtros de tiempo -->
          <div class="time-pills">
            <button 
              :class="['time-btn', { active: timeFilter === 'todos' }]" 
              @click="timeFilter = 'todos'"
            >
              Todos
            </button>
            <button 
              :class="['time-btn', { active: timeFilter === 'hoy' }]" 
              @click="timeFilter = 'hoy'"
            >
              Hoy
            </button>
            <button 
              :class="['time-btn', { active: timeFilter === 'semana' }]" 
              @click="timeFilter = 'semana'"
            >
              Esta Semana
            </button>
            <button 
              :class="['time-btn', { active: timeFilter === 'mes' }]" 
              @click="timeFilter = 'mes'"
            >
              Este Mes
            </button>
          </div>

          <!-- Buscador -->
          <div class="search-input-wrap">
            <span class="search-ico">🔍</span>
            <input 
              v-model="searchQuery"
              type="text" 
              class="search-input-field" 
              placeholder="Buscar cliente, chofer o ruta..."
            />
          </div>
        </div>
      </div>

      <!-- Estado vacío -->
      <div v-if="filteredRecentServices.length === 0" class="empty-feed">
        <span class="empty-ico">🚐</span>
        <h3>No hay servicios registrados en este filtro</h3>
        <p>Utiliza el botón de registrar para añadir el primer viaje de la flota.</p>
        <button class="btn btn-primary" @click="$emit('open-new-service')">Registrar Primer Viaje</button>
      </div>

      <!-- Tabla de Viajes de Toda la Flota -->
      <div v-else class="table-responsive">
        <table class="superadmin-table feed-table">
          <thead>
            <tr>
              <th>Código</th>
              <th>Fecha</th>
              <th>Chofer</th>
              <th>Ruta / Servicio</th>
              <th>Cliente & Destino</th>
              <th class="text-right">Cobrado</th>
              <th class="text-right">Empresa (30%)</th>
              <th class="text-right">Comisión Chofer</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="srv in filteredRecentServices" :key="srv.id" class="feed-row">
              <td>
                <span class="srv-code">{{ srv.serviceCode }}</span>
              </td>
              <td class="text-muted">{{ formatDate(srv.serviceDate || srv.createdAt) }}</td>
              <td>
                <div class="srv-driver-cell">
                  <span class="driver-icon">🚐</span>
                  <strong>{{ srv.driverName || 'Chofer' }}</strong>
                </div>
              </td>
              <td>
                <span class="route-tag">{{ srv.serviceTypeName }}</span>
              </td>
              <td>
                <div class="client-details">
                  <span class="client-name">{{ srv.clientName }}</span>
                  <span v-if="srv.clientAddress" class="client-addr">📍 {{ srv.clientAddress }}</span>
                </div>
              </td>
              <td class="text-right font-mono font-bold">
                RD$ {{ Number(srv.priceCharged).toFixed(2) }}
              </td>
              <td class="text-right font-mono text-emerald">
                RD$ {{ (Number(srv.priceCharged) * 0.30).toFixed(2) }}
              </td>
              <td class="text-right font-mono text-cyan">
                +RD$ {{ Number(srv.employeeEarnings).toFixed(2) }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.superadmin-dashboard {
  display: flex;
  flex-direction: column;
  gap: 24px;
  animation: fadeIn 0.35s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(8px); }
  to { opacity: 1; transform: translateY(0); }
}

/* 1. HERO EJECUTIVO */
.dev-hero {
  background: linear-gradient(135deg, rgba(16, 45, 28, 0.85) 0%, rgba(13, 26, 20, 0.95) 100%);
  border: 1px solid rgba(34, 197, 94, 0.35);
  box-shadow: 0 12px 36px rgba(0, 0, 0, 0.4), inset 0 1px 0 rgba(255, 255, 255, 0.1);
  border-radius: var(--radius-xl);
  padding: 28px 32px;
}

.hero-main-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 20px;
}

.dev-profile-cluster {
  display: flex;
  align-items: center;
  gap: 20px;
}

.dev-avatar-ring {
  position: relative;
  width: 72px;
  height: 72px;
  border-radius: var(--radius-lg);
  background: linear-gradient(135deg, #22c55e, #15803d);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 0 24px rgba(34, 197, 94, 0.5);
  flex-shrink: 0;
}

.avatar-icon {
  font-size: 2.2rem;
}

.status-indicator-live {
  position: absolute;
  bottom: -4px;
  right: -4px;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  background: #ef4444;
  border: 2px solid #0d1a14;
}

.status-indicator-live.online {
  background: #22c55e;
  box-shadow: 0 0 10px #22c55e;
}

.role-badge-pill-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 6px;
}

.role-badge-primary {
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  background: rgba(245, 158, 11, 0.15);
  color: #fbbf24;
  border: 1px solid rgba(245, 158, 11, 0.4);
  padding: 3px 10px;
  border-radius: var(--radius-full);
}

.api-status-chip {
  font-size: 0.72rem;
  font-weight: 700;
  padding: 3px 10px;
  border-radius: var(--radius-full);
  background: rgba(239, 68, 68, 0.15);
  color: #ef4444;
  border: 1px solid rgba(239, 68, 68, 0.3);
}

.api-status-chip.online {
  background: rgba(34, 197, 94, 0.15);
  color: #4ade80;
  border-color: rgba(34, 197, 94, 0.35);
}

.env-chip {
  font-size: 0.72rem;
  font-weight: 600;
  color: var(--text-muted);
  background: rgba(255, 255, 255, 0.05);
  padding: 3px 8px;
  border-radius: var(--radius-sm);
}

.dev-title {
  font-size: 1.85rem;
  font-weight: 800;
  color: #ffffff;
  margin: 0;
  letter-spacing: -0.02em;
}

.highlight-name {
  background: linear-gradient(135deg, #ffffff 0%, #4ade80 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.dev-tagline {
  color: var(--text-muted);
  font-size: 0.95rem;
  margin: 6px 0 0 0;
  max-width: 600px;
}

.dev-actions-cluster {
  display: flex;
  align-items: center;
  gap: 12px;
}

/* 2. CUADRÍCULA DE KPIS */
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
}

.kpi-card {
  padding: 20px;
  border-radius: var(--radius-lg);
  border: 1px solid var(--brand-border);
  background: rgba(18, 25, 20, 0.75);
  backdrop-filter: blur(12px);
  display: flex;
  flex-direction: column;
  gap: 10px;
  transition: transform var(--transition-fast), border-color var(--transition-fast);
}

.kpi-card:hover {
  transform: translateY(-2px);
  border-color: var(--brand-border-strong);
}

.kpi-card.highlight-card {
  background: linear-gradient(145deg, rgba(34, 197, 94, 0.15), rgba(18, 25, 20, 0.9));
  border-color: rgba(34, 197, 94, 0.45);
  box-shadow: 0 8px 24px rgba(34, 197, 94, 0.15);
}

.kpi-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.kpi-label {
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.kpi-icon {
  font-size: 1.3rem;
}

.kpi-body {
  display: flex;
  align-items: baseline;
  gap: 4px;
}

.kpi-currency {
  font-size: 1rem;
  font-weight: 700;
  color: var(--text-muted);
}

.kpi-number {
  font-family: var(--font-heading);
  font-size: 1.65rem;
  font-weight: 800;
  color: #ffffff;
}

.kpi-number.count {
  font-size: 2.1rem;
  color: var(--brand-green-light);
}

.highlight-text .kpi-number {
  color: #4ade80;
}

.highlight-text .kpi-currency {
  color: #22c55e;
}

.kpi-footer {
  display: flex;
  flex-direction: column;
  gap: 4px;
  border-top: 1px solid rgba(255, 255, 255, 0.05);
  padding-top: 8px;
}

.kpi-desc {
  font-size: 0.75rem;
  color: var(--text-muted);
}

.trend-badge {
  font-size: 0.7rem;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: var(--radius-sm);
  width: fit-content;
}

.trend-badge.positive {
  background: rgba(34, 197, 94, 0.2);
  color: #4ade80;
}

.trend-badge.neutral {
  background: rgba(56, 189, 248, 0.2);
  color: #38bdf8;
}

.badge-company {
  font-size: 0.7rem;
  font-weight: 800;
  background: rgba(34, 197, 94, 0.3);
  color: #22c55e;
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  width: fit-content;
}

.badge-deduction {
  font-size: 0.7rem;
  font-weight: 700;
  background: rgba(244, 63, 94, 0.2);
  color: #fb7185;
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  width: fit-content;
}

/* 3. DEVOPS CONSOLE */
.devops-console {
  background: rgba(10, 16, 12, 0.85);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: var(--radius-lg);
  padding: 20px 24px;
}

.console-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.console-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.console-icon {
  font-size: 1.2rem;
}

.console-title {
  font-size: 1.05rem;
  font-weight: 700;
  color: #ffffff;
  margin: 0;
}

.console-badge {
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  background: rgba(34, 197, 94, 0.15);
  color: var(--brand-green-light);
  border: 1px solid rgba(34, 197, 94, 0.3);
  padding: 4px 10px;
  border-radius: var(--radius-full);
}

.console-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
}

.console-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
  background: rgba(0, 0, 0, 0.3);
  padding: 12px 14px;
  border-radius: var(--radius-md);
  border: 1px solid rgba(255, 255, 255, 0.04);
}

.item-title {
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
}

.item-val {
  font-size: 0.85rem;
  color: #ffffff;
}

.item-status {
  font-size: 0.75rem;
  font-weight: 600;
  margin-top: 4px;
}

.item-status.status-good {
  color: #4ade80;
}

.item-status.status-info {
  color: #38bdf8;
}

/* 4. SECCIÓN DE FLOTA Y TABLA */
.fleet-section,
.activity-feed-section {
  padding: 24px;
  border-radius: var(--radius-xl);
  background: rgba(18, 25, 20, 0.85);
  border: 1px solid var(--brand-border);
}

.section-top-bar {
  margin-bottom: 20px;
}

.title-cluster {
  display: flex;
  align-items: center;
  gap: 12px;
}

.section-heading {
  font-size: 1.25rem;
  font-weight: 800;
  color: #ffffff;
  margin: 0;
}

.badge-counter {
  font-size: 0.75rem;
  font-weight: 700;
  background: rgba(34, 197, 94, 0.15);
  color: var(--brand-green-light);
  padding: 4px 10px;
  border-radius: var(--radius-full);
}

.section-subtext {
  color: var(--text-muted);
  font-size: 0.85rem;
  margin: 4px 0 0 0;
}

/* Tablas Modernas */
.table-responsive {
  overflow-x: auto;
}

.superadmin-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.88rem;
}

.superadmin-table th {
  text-align: left;
  padding: 12px 16px;
  color: var(--text-muted);
  font-weight: 700;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.superadmin-table td {
  padding: 14px 16px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  vertical-align: middle;
}

.driver-row:hover,
.feed-row:hover {
  background: rgba(255, 255, 255, 0.02);
}

.driver-identity-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.driver-table-avatar {
  font-size: 1.4rem;
  width: 36px;
  height: 36px;
  background: rgba(34, 197, 94, 0.15);
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
}

.driver-names {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.driver-full-name {
  color: #ffffff;
  font-size: 0.92rem;
}

.driver-code-tag {
  font-size: 0.75rem;
  color: var(--text-muted);
}

.driver-dept-badge {
  font-size: 0.78rem;
  background: rgba(255, 255, 255, 0.05);
  color: var(--text-muted);
  padding: 4px 8px;
  border-radius: var(--radius-sm);
}

.trips-pill {
  font-size: 0.8rem;
  font-weight: 700;
  background: rgba(56, 189, 248, 0.15);
  color: #38bdf8;
  padding: 3px 10px;
  border-radius: var(--radius-full);
}

.font-mono {
  font-family: var(--font-heading);
}

.font-bold {
  font-weight: 700;
}

.text-emerald {
  color: #4ade80;
  font-weight: 700;
}

.text-cyan {
  color: #38bdf8;
}

.text-accent {
  color: #fbbf24;
}

.text-right {
  text-align: right;
}

.text-center {
  text-align: center;
}

.btn-inspect-driver {
  background: rgba(34, 197, 94, 0.12);
  border: 1px solid rgba(34, 197, 94, 0.35);
  color: var(--brand-green-light);
  font-size: 0.78rem;
  font-weight: 700;
  padding: 6px 12px;
  border-radius: var(--radius-md);
  display: inline-flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.btn-inspect-driver:hover {
  background: var(--brand-green);
  color: #ffffff;
  border-color: var(--brand-green);
  transform: translateY(-1px);
}

/* 5. ACTIVIDAD EN VIVO */
.feed-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 20px;
}

.feed-title-wrap {
  display: flex;
  align-items: center;
  gap: 12px;
}

.feed-controls {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.time-pills {
  display: flex;
  background: rgba(0, 0, 0, 0.3);
  padding: 4px;
  border-radius: var(--radius-md);
  border: 1px solid var(--brand-border);
}

.time-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 0.78rem;
  font-weight: 600;
  padding: 5px 12px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.time-btn.active {
  background: var(--brand-green);
  color: #ffffff;
}

.search-input-wrap {
  position: relative;
  min-width: 240px;
}

.search-ico {
  position: absolute;
  left: 10px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 0.8rem;
}

.search-input-field {
  width: 100%;
  background: rgba(0, 0, 0, 0.3);
  border: 1px solid var(--brand-border);
  color: #ffffff;
  font-size: 0.8rem;
  padding: 7px 10px 7px 32px;
  border-radius: var(--radius-md);
  outline: none;
}

.search-input-field:focus {
  border-color: var(--brand-green-light);
}

.srv-code {
  font-family: var(--font-heading);
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--brand-green-light);
  background: rgba(34, 197, 94, 0.1);
  padding: 2px 6px;
  border-radius: var(--radius-sm);
}

.srv-driver-cell {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #ffffff;
}

.route-tag {
  font-size: 0.8rem;
  background: rgba(255, 255, 255, 0.05);
  padding: 3px 8px;
  border-radius: var(--radius-sm);
  color: #ffffff;
}

.client-details {
  display: flex;
  flex-direction: column;
}

.client-name {
  color: #ffffff;
  font-weight: 600;
}

.client-addr {
  font-size: 0.75rem;
  color: var(--text-muted);
}

.empty-feed {
  padding: 40px 20px;
  text-align: center;
  color: var(--text-muted);
}

.empty-ico {
  font-size: 2.5rem;
  display: block;
  margin-bottom: 12px;
}

@media (max-width: 768px) {
  .dev-hero {
    padding: 20px;
  }
  .dev-title {
    font-size: 1.4rem;
  }
  .kpi-grid {
    grid-template-columns: 1fr;
  }
  .feed-controls {
    width: 100%;
  }
  .search-input-wrap {
    width: 100%;
  }
}
</style>
