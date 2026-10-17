<script setup>
import { ref, computed, watch } from 'vue';
import SuperAdminDashboard from '../components/SuperAdminDashboard.vue';

const props = defineProps({
  employeeSummary: {
    type: Object,
    default: () => ({
      fullName: 'Chofer',
      code: 'CHO-001',
      role: 'Conductor',
      department: 'Operaciones',
      commissionRate: 25,
      totalEarningsAccumulated: 0,
      totalServicesCompleted: 0,
      totalBilled: 0,
      averageEarningsPerService: 0,
      recentServices: []
    })
  },
  services: {
    type: Array,
    default: () => []
  },
  allServices: {
    type: Array,
    default: () => []
  },
  employees: {
    type: Array,
    default: () => []
  },
  users: {
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
  currentUser: {
    type: Object,
    default: null
  },
  backendOnline: Boolean,
  loading: Boolean
});

const emit = defineEmits(['open-new-service', 'refresh', 'change-employee', 'change-tab']);

const isSuperAdminOrAdmin = computed(() => {
  return props.currentUser && (
    props.currentUser.role === 'SuperAdmin' || 
    props.currentUser.isSuperAdmin || 
    props.currentUser.role === 'Administrador'
  );
});

// Por defecto, si es SuperAdmin/Admin, arranca en la Visión Global de Flota; si es Chofer, en su panel de chofer
const viewMode = ref(isSuperAdminOrAdmin.value ? 'global' : 'driver');

watch(() => props.currentUser, (newVal) => {
  if (newVal?.role === 'Chofer') {
    viewMode.value = 'driver';
  } else if (newVal?.role === 'SuperAdmin' || newVal?.isSuperAdmin) {
    viewMode.value = 'global';
  }
}, { immediate: true });

const onInspectDriver = (driverId) => {
  emit('change-employee', driverId);
  viewMode.value = 'driver';
};

const searchQuery = ref('');
const activeFilter = ref('todos'); // 'todos' | 'hoy' | 'semana' | 'mes'

// Filtrado de servicios
const filteredServices = computed(() => {
  let list = props.services || [];

  // Filtro de tiempo
  const now = new Date();
  if (activeFilter.value === 'hoy') {
    const todayStr = now.toISOString().slice(0, 10);
    list = list.filter(s => s.serviceDate?.slice(0, 10) === todayStr);
  } else if (activeFilter.value === 'semana') {
    const weekAgo = new Date();
    weekAgo.setDate(now.getDate() - 7);
    list = list.filter(s => new Date(s.serviceDate) >= weekAgo);
  } else if (activeFilter.value === 'mes') {
    const monthAgo = new Date();
    monthAgo.setMonth(now.getMonth() - 1);
    list = list.filter(s => new Date(s.serviceDate) >= monthAgo);
  }

  // Filtro de búsqueda por cliente o ruta
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase();
    list = list.filter(s => 
      s.clientName?.toLowerCase().includes(q) ||
      s.clientAddress?.toLowerCase().includes(q) ||
      s.serviceTypeName?.toLowerCase().includes(q) ||
      s.serviceCode?.toLowerCase().includes(q)
    );
  }

  return list;
});

const formatDate = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('es-DO', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  });
};
</script>

<template>
  <div class="dashboard-view">
    <!-- Switcher de Modo de Vista (Solo SuperAdmin y Administrador) -->
    <div v-if="isSuperAdminOrAdmin" class="view-mode-bar">
      <div class="view-mode-pills">
        <button 
          :class="['mode-pill', { active: viewMode === 'global' }]"
          @click="viewMode = 'global'"
        >
          <span class="pill-icon">👑</span>
          <span class="pill-text">Visión Global de Flota & Dev</span>
        </button>
        <button 
          :class="['mode-pill', { active: viewMode === 'driver' }]"
          @click="viewMode = 'driver'"
        >
          <span class="pill-icon">🚐</span>
          <span class="pill-text">Auditoría Individual: {{ employeeSummary.fullName }}</span>
        </button>
      </div>

      <div class="view-mode-meta desktop-only">
        <span class="meta-tag">
          👤 Conectado: <strong>{{ currentUser?.fullName || 'SuperAdmin' }}</strong> ({{ currentUser?.role }})
        </span>
      </div>
    </div>

    <!-- Vista 1: Panel Global del SuperAdmin / Developer Command Center -->
    <SuperAdminDashboard
      v-if="isSuperAdminOrAdmin && viewMode === 'global'"
      :currentUser="currentUser"
      :employees="employees"
      :users="users"
      :allServices="allServices"
      :serviceTypes="serviceTypes"
      :driverSummaries="driverSummaries"
      :backendOnline="backendOnline"
      :loading="loading"
      @open-new-service="$emit('open-new-service')"
      @inspect-driver="onInspectDriver"
      @change-tab="tab => $emit('change-tab', tab)"
      @refresh="$emit('refresh')"
    />

    <!-- Vista 2: Panel Individual del Chofer (O Auditoría para SuperAdmin) -->
    <div v-else class="driver-mode-container">
      <!-- Hero / Tarjeta de Bienvenida del Chofer -->
      <div class="driver-hero glass-card">
      <div class="hero-content">
        <div class="driver-avatar">
          <span class="avatar-icon">🚐</span>
        </div>
        <div class="driver-info">
          <div class="driver-badge">
            <span>{{ employeeSummary.code }}</span>
            <span class="badge-dot">•</span>
            <span>Tasa de Comisión: {{ employeeSummary.commissionRate }}%</span>
          </div>
          <h1 class="driver-name">{{ employeeSummary.fullName }}</h1>
          <p class="driver-role">{{ employeeSummary.role }} — {{ employeeSummary.department }}</p>
        </div>
      </div>
      <div class="hero-actions">
        <button class="btn btn-primary btn-cta" @click="$emit('open-new-service')">
          <span class="plus-sign">+</span>
          <span>Registrar Nuevo Viaje</span>
        </button>
      </div>
    </div>

    <!-- Cuadrícula de Métricas Clave de Acumulados -->
    <div class="metrics-grid">
      <!-- Tarjeta 1: Total Acumulado (Destacada en Verde Brillante) -->
      <div class="metric-card glass-card featured">
        <div class="metric-header">
          <span class="metric-label">Tu Ganancia Acumulada</span>
          <span class="metric-icon">💰</span>
        </div>
        <div class="metric-value">
          <span class="currency">$</span>
          <span class="amount">{{ (employeeSummary.totalEarningsAccumulated || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</span>
        </div>
        <div class="metric-subtext">
          <span class="badge-green">Acumulado Neto</span>
          <span>Disponible según tus comisiones</span>
        </div>
      </div>

      <!-- Tarjeta 2: Viajes Realizados -->
      <div class="metric-card glass-card">
        <div class="metric-header">
          <span class="metric-label">Viajes Completados</span>
          <span class="metric-icon">🛣️</span>
        </div>
        <div class="metric-value">
          <span class="amount-plain">{{ employeeSummary.totalServicesCompleted || 0 }}</span>
        </div>
        <div class="metric-subtext">
          <span>Servicios registrados exitosamente</span>
        </div>
      </div>

      <!-- Tarjeta 3: Total Facturado a Clientes -->
      <div class="metric-card glass-card">
        <div class="metric-header">
          <span class="metric-label">Total Facturado</span>
          <span class="metric-icon">💵</span>
        </div>
        <div class="metric-value">
          <span class="currency">$</span>
          <span class="amount-plain">{{ (employeeSummary.totalBilled || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</span>
        </div>
        <div class="metric-subtext">
          <span>Volumen total cobrado en viajes</span>
        </div>
      </div>

      <!-- Tarjeta 4: Promedio por Viaje -->
      <div class="metric-card glass-card">
        <div class="metric-header">
          <span class="metric-label">Promedio por Viaje</span>
          <span class="metric-icon">📈</span>
        </div>
        <div class="metric-value">
          <span class="currency">$</span>
          <span class="amount-plain">{{ (employeeSummary.averageEarningsPerService || 0).toFixed(2) }}</span>
        </div>
        <div class="metric-subtext">
          <span>Ganancia promedio por servicio</span>
        </div>
      </div>
    </div>

    <!-- Sección de Historial de Viajes Registrados -->
    <div class="history-section glass-card">
      <div class="section-toolbar">
        <div class="toolbar-title-wrap">
          <h2 class="section-title">Historial de Viajes Registrados</h2>
          <span class="count-badge">{{ filteredServices.length }} viajes</span>
        </div>

        <!-- Filtros y Buscador -->
        <div class="toolbar-controls">
          <!-- Filtro rápido de tiempo -->
          <div class="filter-pills">
            <button 
              :class="['pill-btn', { active: activeFilter === 'todos' }]" 
              @click="activeFilter = 'todos'"
            >
              Todos
            </button>
            <button 
              :class="['pill-btn', { active: activeFilter === 'hoy' }]" 
              @click="activeFilter = 'hoy'"
            >
              Hoy
            </button>
            <button 
              :class="['pill-btn', { active: activeFilter === 'semana' }]" 
              @click="activeFilter = 'semana'"
            >
              Esta Semana
            </button>
            <button 
              :class="['pill-btn', { active: activeFilter === 'mes' }]" 
              @click="activeFilter = 'mes'"
            >
              Este Mes
            </button>
          </div>

          <!-- Campo de búsqueda -->
          <div class="search-box">
            <span class="search-icon">🔍</span>
            <input 
              v-model="searchQuery"
              type="text" 
              class="search-input" 
              placeholder="Buscar por cliente, ruta o código..."
            />
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="state-container">
        <div class="spinner"></div>
        <p>Cargando información de viajes...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="filteredServices.length === 0" class="state-container empty">
        <div class="empty-icon">🚐</div>
        <h3>No se encontraron viajes registrados</h3>
        <p>Aún no tienes viajes con este filtro o búsqueda. ¡Registra tu primer viaje para comenzar a acumular!</p>
        <button class="btn btn-primary" @click="$emit('open-new-service')">
          Registrar Primer Viaje
        </button>
      </div>

      <!-- Tabla para Escritorio (Desktop > 768px) -->
      <div v-else class="table-responsive desktop-only">
        <table class="services-table">
          <thead>
            <tr>
              <th>Código</th>
              <th>Fecha</th>
              <th>Cliente</th>
              <th>Ruta / Servicio</th>
              <th>Estado</th>
              <th class="text-right">Cobrado</th>
              <th class="text-right">Tu Acumulado</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="srv in filteredServices" :key="srv.id" class="table-row">
              <td>
                <span class="code-badge">{{ srv.serviceCode }}</span>
              </td>
              <td class="text-muted">{{ formatDate(srv.serviceDate) }}</td>
              <td>
                <div class="client-cell">
                  <strong>{{ srv.clientName }}</strong>
                  <span v-if="srv.clientPhone" class="client-phone">{{ srv.clientPhone }}</span>
                </div>
              </td>
              <td>
                <div class="service-cell">
                  <span class="service-name">{{ srv.serviceTypeName }}</span>
                  <span v-if="srv.clientAddress" class="service-route">📍 {{ srv.clientAddress }}</span>
                </div>
              </td>
              <td>
                <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                  {{ srv.statusName || 'Completado' }}
                </span>
              </td>
              <td class="text-right font-display text-muted">
                ${{ Number(srv.priceCharged).toFixed(2) }}
              </td>
              <td class="text-right">
                <span class="earnings-cell font-display">
                  +${{ Number(srv.employeeEarnings).toFixed(2) }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Vista de Tarjetas para Móviles (Mobile <= 768px) -->
      <div v-if="!loading && filteredServices.length > 0" class="mobile-cards-list mobile-only">
        <div v-for="srv in filteredServices" :key="srv.id" class="service-card glass-card">
          <div class="card-top">
            <span class="code-badge">{{ srv.serviceCode }}</span>
            <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
              {{ srv.statusName || 'Completado' }}
            </span>
          </div>

          <div class="card-main">
            <h3 class="card-client">{{ srv.clientName }}</h3>
            <p class="card-service-type">🚐 {{ srv.serviceTypeName }}</p>
            <p v-if="srv.clientAddress" class="card-route">📍 {{ srv.clientAddress }}</p>
          </div>

          <div class="card-bottom">
            <div class="card-date">
              <span>📅</span>
              <span>{{ formatDate(srv.serviceDate) }}</span>
            </div>
            <div class="card-financials">
              <span class="card-charged">Cobrado: ${{ Number(srv.priceCharged).toFixed(2) }}</span>
              <span class="card-earnings">+${{ Number(srv.employeeEarnings).toFixed(2) }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
    </div>
  </div>
</template>

<style scoped>
.dashboard-view {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* Switcher de Modos de Vista */
.view-mode-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 12px;
  background: rgba(18, 25, 20, 0.85);
  border: 1px solid var(--brand-border);
  padding: 8px 16px;
  border-radius: var(--radius-lg);
  backdrop-filter: blur(12px);
}

.view-mode-pills {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgba(0, 0, 0, 0.4);
  padding: 4px;
  border-radius: var(--radius-md);
  border: 1px solid var(--brand-border);
}

.mode-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-family: var(--font-heading);
  font-size: 0.82rem;
  font-weight: 700;
  padding: 8px 16px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.mode-pill:hover {
  color: #ffffff;
}

.mode-pill.active {
  background: var(--brand-green);
  color: #ffffff;
  box-shadow: 0 2px 12px rgba(34, 197, 94, 0.45);
}

.view-mode-meta .meta-tag {
  font-size: 0.8rem;
  color: var(--text-muted);
  background: rgba(255, 255, 255, 0.04);
  padding: 6px 12px;
  border-radius: var(--radius-full);
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.view-mode-meta strong {
  color: #ffffff;
}

.driver-mode-container {
  display: flex;
  flex-direction: column;
  gap: 24px;
  animation: fadeIn 0.35s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

/* Hero del Chofer */
.driver-hero {
  padding: 24px 28px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  background: linear-gradient(135deg, rgba(22, 32, 26, 0.95), rgba(12, 17, 14, 0.98));
  border-left: 4px solid var(--brand-green-light);
}

.hero-content {
  display: flex;
  align-items: center;
  gap: 18px;
}

.driver-avatar {
  width: 64px;
  height: 64px;
  border-radius: var(--radius-md);
  background: linear-gradient(135deg, var(--brand-green), var(--brand-charcoal));
  border: 1px solid var(--brand-border-strong);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2rem;
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.4);
}

.driver-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: rgba(34, 197, 94, 0.12);
  color: var(--brand-green-light);
  border: 1px solid rgba(34, 197, 94, 0.3);
  padding: 2px 10px;
  border-radius: var(--radius-full);
  font-size: 0.75rem;
  font-weight: 700;
  margin-bottom: 6px;
}

.driver-name {
  font-size: 1.6rem;
  color: #ffffff;
  line-height: 1.2;
}

.driver-role {
  font-size: 0.9rem;
  color: var(--text-muted);
}

.btn-cta {
  padding: 12px 24px;
  font-size: 1.05rem;
}

.plus-sign {
  font-size: 1.3rem;
  line-height: 1;
}

/* Cuadrícula de Métricas */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 18px;
}

.metric-card {
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  position: relative;
  overflow: hidden;
}

.metric-card.featured {
  background: linear-gradient(145deg, rgba(21, 128, 61, 0.25), rgba(19, 27, 22, 0.95));
  border-color: rgba(34, 197, 94, 0.4);
  box-shadow: 0 8px 30px rgba(34, 197, 94, 0.15);
}

.metric-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.metric-label {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-muted);
}

.metric-icon {
  font-size: 1.4rem;
}

.metric-value {
  display: flex;
  align-items: baseline;
  font-family: var(--font-heading);
  font-weight: 800;
}

.metric-value .currency {
  font-size: 1.3rem;
  color: var(--brand-green-light);
  margin-right: 4px;
}

.metric-value .amount {
  font-size: 2.2rem;
  color: #ffffff;
  text-shadow: 0 0 20px var(--brand-green-glow);
}

.metric-value .amount-plain {
  font-size: 2.2rem;
  color: #ffffff;
}

.metric-subtext {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.78rem;
  color: var(--text-dim);
}

.badge-green {
  background: var(--brand-green);
  color: #ffffff;
  padding: 1px 7px;
  border-radius: var(--radius-full);
  font-weight: 700;
  font-size: 0.7rem;
}

/* Sección de Historial */
.history-section {
  padding: 24px;
}

.section-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 20px;
}

.toolbar-title-wrap {
  display: flex;
  align-items: center;
  gap: 12px;
}

.section-title {
  font-size: 1.3rem;
  color: #ffffff;
}

.count-badge {
  background: rgba(34, 197, 94, 0.15);
  color: var(--brand-green-light);
  padding: 2px 10px;
  border-radius: var(--radius-full);
  font-size: 0.8rem;
  font-weight: 700;
}

.toolbar-controls {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.filter-pills {
  display: flex;
  background: var(--brand-charcoal);
  padding: 3px;
  border-radius: var(--radius-md);
  border: 1px solid var(--brand-border);
}

.pill-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 0.8rem;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.pill-btn.active {
  background: var(--brand-green);
  color: #ffffff;
}

.search-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--brand-charcoal);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  padding: 6px 12px;
}

.search-input {
  background: transparent;
  border: none;
  color: #ffffff;
  outline: none;
  font-size: 0.85rem;
  width: 200px;
}

/* Tabla Desktop */
.table-responsive {
  overflow-x: auto;
}

.services-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}

.services-table th {
  padding: 12px 14px;
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--text-dim);
  border-bottom: 1px solid var(--brand-border);
}

.services-table td {
  padding: 14px;
  border-bottom: 1px solid rgba(34, 197, 94, 0.08);
  font-size: 0.9rem;
}

.table-row:hover td {
  background: rgba(34, 197, 94, 0.04);
}

.code-badge {
  font-family: monospace;
  font-size: 0.8rem;
  background: rgba(255, 255, 255, 0.08);
  padding: 3px 8px;
  border-radius: var(--radius-sm);
  color: #ffffff;
  font-weight: 600;
}

.client-cell {
  display: flex;
  flex-direction: column;
}

.client-phone {
  font-size: 0.78rem;
  color: var(--text-dim);
}

.service-cell {
  display: flex;
  flex-direction: column;
}

.service-route {
  font-size: 0.78rem;
  color: var(--brand-green-light);
}

.earnings-cell {
  color: var(--brand-green-light);
  font-weight: 800;
  font-size: 1.05rem;
  text-shadow: 0 0 10px var(--brand-green-glow);
}

.text-right {
  text-align: right;
}

/* Tarjetas Móviles */
.mobile-cards-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.service-card {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  border-left: 3px solid var(--brand-green);
}

.card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.card-client {
  font-size: 1.1rem;
  color: #ffffff;
  margin-bottom: 4px;
}

.card-service-type {
  font-size: 0.85rem;
  color: var(--text-muted);
}

.card-route {
  font-size: 0.8rem;
  color: var(--brand-green-light);
  margin-top: 2px;
}

.card-bottom {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 10px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}

.card-date {
  font-size: 0.8rem;
  color: var(--text-dim);
  display: flex;
  align-items: center;
  gap: 4px;
}

.card-financials {
  display: flex;
  align-items: baseline;
  gap: 10px;
}

.card-charged {
  font-size: 0.8rem;
  color: var(--text-dim);
}

.card-earnings {
  font-family: var(--font-heading);
  font-weight: 800;
  font-size: 1.15rem;
  color: var(--brand-green-light);
}

/* Estados Vacíos y de Carga */
.state-container {
  padding: 48px 20px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 14px;
  color: var(--text-muted);
}

.empty-icon {
  font-size: 3rem;
  filter: grayscale(0.5);
}

.spinner {
  width: 36px;
  height: 36px;
  border: 3px solid var(--brand-border);
  border-top-color: var(--brand-green-light);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

@media (max-width: 768px) {
  .driver-hero {
    flex-direction: column;
    align-items: flex-start;
    padding: 18px;
  }
  .hero-actions {
    width: 100%;
  }
  .btn-cta {
    width: 100%;
  }
  .metrics-grid {
    grid-template-columns: 1fr 1fr;
    gap: 12px;
  }
  .metric-card {
    padding: 14px;
  }
  .metric-value .amount,
  .metric-value .amount-plain {
    font-size: 1.6rem;
  }
  .search-box {
    width: 100%;
  }
  .search-input {
    width: 100%;
  }
  .filter-pills {
    width: 100%;
    justify-content: space-between;
  }
  .desktop-only {
    display: none !important;
  }
}
</style>
