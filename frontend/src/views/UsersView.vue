<script setup>
import { ref, computed, onMounted } from 'vue';

const props = defineProps({
  users: {
    type: Array,
    default: () => []
  },
  loading: Boolean,
  currentUser: {
    type: Object,
    default: null
  }
});

const emit = defineEmits(['open-create-modal', 'edit-user', 'delete-user', 'refresh']);

const searchQuery = ref('');
const selectedRole = ref('all'); // 'all' | 'Chofer' | 'Administrador' | 'SuperAdmin'
const onlyActive = ref(true);

// Filtrado reactivo en cliente (más filtros del backend)
const filteredUsers = computed(() => {
  return props.users.filter((u) => {
    // Filtro por rol
    if (selectedRole.value !== 'all' && u.role !== selectedRole.value) {
      return false;
    }

    // Filtro activo
    if (onlyActive.value && !u.isActive) {
      return false;
    }

    // Búsqueda por texto (nombre, email, código)
    if (searchQuery.value) {
      const q = searchQuery.value.toLowerCase().trim();
      const matchName = (u.fullName || '').toLowerCase().includes(q);
      const matchEmail = (u.email || '').toLowerCase().includes(q);
      const matchCode = (u.code || '').toLowerCase().includes(q);
      const matchDept = (u.department || '').toLowerCase().includes(q);
      return matchName || matchEmail || matchCode || matchDept;
    }

    return true;
  });
});

// Métricas rápidas
const totalDrivers = computed(() => props.users.filter(u => u.role === 'Chofer').length);
const totalAdmins = computed(() => props.users.filter(u => u.role === 'Administrador' || u.role === 'SuperAdmin').length);
const activeCount = computed(() => props.users.filter(u => u.isActive).length);

const confirmDelete = (u) => {
  if (u.id === props.currentUser?.id || u.isSuperAdmin) {
    alert('No puedes eliminar tu propia cuenta de SuperAdmin.');
    return;
  }
  const confirmed = confirm(
    `¿Estás seguro de que deseas dar de baja (Soft Delete) a ${u.fullName} (${u.code})?\n\nEl usuario no se borrará físicamente para preservar su historial de viajes y comisiones.`
  );
  if (confirmed) {
    emit('delete-user', u.id);
  }
};
</script>

<template>
  <div class="users-view-wrapper">
    <!-- Encabezado de la Sección -->
    <div class="view-header">
      <div class="header-text">
        <h1 class="view-title">Personal & Usuarios</h1>
        <p class="view-description">
          Administración centralizada de choferes, personal administrativo y roles de Transporte Tito.
        </p>
      </div>

      <div class="header-actions">
        <button class="btn btn-outline" @click="$emit('refresh')" :disabled="loading" title="Refrescar lista">
          <span :class="['refresh-icon', { spinning: loading }]">🔄</span>
          <span>Actualizar</span>
        </button>
        <button class="btn btn-primary" @click="$emit('open-create-modal')">
          <span>+</span>
          <span>Nuevo Usuario</span>
        </button>
      </div>
    </div>

    <!-- Tarjetas de Métricas -->
    <div class="metrics-grid">
      <div class="metric-card">
        <div class="metric-icon green">👥</div>
        <div class="metric-info">
          <span class="metric-label">Total Registrados</span>
          <span class="metric-value">{{ users.length }}</span>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon blue">🚚</div>
        <div class="metric-info">
          <span class="metric-label">Choferes Activos</span>
          <span class="metric-value">{{ totalDrivers }}</span>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon purple">👔</div>
        <div class="metric-info">
          <span class="metric-label">Administradores</span>
          <span class="metric-value">{{ totalAdmins }}</span>
        </div>
      </div>

      <div class="metric-card">
        <div class="metric-icon emerald">✓</div>
        <div class="metric-info">
          <span class="metric-label">Cuentas Activas</span>
          <span class="metric-value">{{ activeCount }}</span>
        </div>
      </div>
    </div>

    <!-- Barra de Filtros y Búsqueda -->
    <div class="filters-bar">
      <!-- Campo de Búsqueda -->
      <div class="search-box">
        <span class="search-icon">🔍</span>
        <input 
          v-model="searchQuery" 
          type="text" 
          class="search-input" 
          placeholder="Buscar por nombre, correo, código (ej. CHO-001)..." 
        />
        <button v-if="searchQuery" class="clear-search" @click="searchQuery = ''">&times;</button>
      </div>

      <!-- Filtros por Rol -->
      <div class="role-pills">
        <button 
          :class="['role-pill', { active: selectedRole === 'all' }]"
          @click="selectedRole = 'all'"
        >
          Todos ({{ users.length }})
        </button>
        <button 
          :class="['role-pill', { active: selectedRole === 'Chofer' }]"
          @click="selectedRole = 'Chofer'"
        >
          🚚 Choferes
        </button>
        <button 
          :class="['role-pill', { active: selectedRole === 'Administrador' }]"
          @click="selectedRole = 'Administrador'"
        >
          👔 Admins
        </button>
        <button 
          :class="['role-pill', { active: selectedRole === 'SuperAdmin' }]"
          @click="selectedRole = 'SuperAdmin'"
        >
          👑 SuperAdmin
        </button>
      </div>

      <!-- Filtro Solo Activos -->
      <label class="active-toggle">
        <input type="checkbox" v-model="onlyActive" class="custom-checkbox" />
        <span>Solo Activos</span>
      </label>
    </div>

    <!-- Estado de Carga -->
    <div v-if="loading" class="loading-state">
      <div class="spinner-large"></div>
      <p>Cargando lista de usuarios del backend...</p>
    </div>

    <!-- Estado Vacío -->
    <div v-else-if="filteredUsers.length === 0" class="empty-state">
      <div class="empty-icon">📂</div>
      <h3>No se encontraron usuarios</h3>
      <p>No hay registros que coincidan con los filtros o la búsqueda ingresada.</p>
      <button class="btn btn-outline" @click="searchQuery = ''; selectedRole = 'all'; onlyActive = false">
        Restablecer Filtros
      </button>
    </div>

    <!-- Tabla de Usuarios (Desktop) -->
    <div v-else class="users-table-container desktop-only">
      <table class="users-table">
        <thead>
          <tr>
            <th>Código / Usuario</th>
            <th>Rol</th>
            <th>Contacto</th>
            <th>Departamento / Ruta</th>
            <th>Comisión</th>
            <th>Estado</th>
            <th class="text-right">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in filteredUsers" :key="u.id" :class="{ 'row-inactive': !u.isActive }">
            <!-- Usuario & Código -->
            <td>
              <div class="user-cell">
                <span class="user-avatar-badge">{{ u.avatar || (u.isSuperAdmin ? '👑' : (u.role === 'Chofer' ? '🚚' : '👤')) }}</span>
                <div class="user-cell-meta">
                  <span class="user-full-name">{{ u.fullName }}</span>
                  <span class="user-code-tag">{{ u.code || 'S/C' }}</span>
                </div>
              </div>
            </td>

            <!-- Rol -->
            <td>
              <span :class="['badge-role', u.role ? u.role.toLowerCase() : '']">
                {{ u.role }}
              </span>
            </td>

            <!-- Contacto -->
            <td>
              <div class="contact-cell">
                <span class="contact-email">{{ u.email }}</span>
                <span v-if="u.phone" class="contact-phone">{{ u.phone }}</span>
              </div>
            </td>

            <!-- Departamento -->
            <td>
              <span class="dept-text">{{ u.department || 'General' }}</span>
            </td>

            <!-- Comisión -->
            <td>
              <span v-if="u.role === 'Chofer'" class="commission-badge">
                {{ Number(u.commissionRate || 0).toFixed(1) }}%
              </span>
              <span v-else class="text-dim">—</span>
            </td>

            <!-- Estado -->
            <td>
              <span :class="['status-pill', u.isActive ? 'active' : 'inactive']">
                <span class="dot"></span>
                {{ u.isActive ? 'Activo' : 'Inactivo' }}
              </span>
            </td>

            <!-- Acciones -->
            <td class="text-right">
              <div class="actions-group">
                <button 
                  class="action-btn edit" 
                  @click="$emit('edit-user', u)" 
                  title="Editar datos de usuario"
                >
                  ✏️
                </button>
                <button 
                  class="action-btn delete" 
                  @click="confirmDelete(u)" 
                  :disabled="u.isSuperAdmin || u.id === currentUser?.id"
                  :title="u.isSuperAdmin ? 'No se puede eliminar al SuperAdmin' : 'Dar de baja (Soft delete)'"
                >
                  🗑️
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Lista de Tarjetas (Móvil) -->
    <div v-if="filteredUsers.length > 0" class="users-mobile-list mobile-only">
      <div v-for="u in filteredUsers" :key="u.id" :class="['user-mobile-card', { 'card-inactive': !u.isActive }]">
        <div class="card-header">
          <div class="user-cell">
            <span class="user-avatar-badge">{{ u.avatar || (u.isSuperAdmin ? '👑' : (u.role === 'Chofer' ? '🚚' : '👤')) }}</span>
            <div class="user-cell-meta">
              <span class="user-full-name">{{ u.fullName }}</span>
              <span class="user-code-tag">{{ u.code }}</span>
            </div>
          </div>
          <span :class="['badge-role', u.role ? u.role.toLowerCase() : '']">{{ u.role }}</span>
        </div>

        <div class="card-body">
          <div class="info-row">
            <span class="info-label">Email:</span>
            <span class="info-val">{{ u.email }}</span>
          </div>
          <div v-if="u.phone" class="info-row">
            <span class="info-label">Teléfono:</span>
            <span class="info-val">{{ u.phone }}</span>
          </div>
          <div class="info-row">
            <span class="info-label">Ruta / Dept:</span>
            <span class="info-val">{{ u.department || 'General' }}</span>
          </div>
          <div v-if="u.role === 'Chofer'" class="info-row">
            <span class="info-label">Comisión:</span>
            <span class="info-val highlight">{{ Number(u.commissionRate || 0).toFixed(1) }}%</span>
          </div>
        </div>

        <div class="card-footer">
          <span :class="['status-pill', u.isActive ? 'active' : 'inactive']">
            <span class="dot"></span>
            {{ u.isActive ? 'Activo' : 'Inactivo' }}
          </span>

          <div class="actions-group">
            <button class="btn btn-outline btn-sm" @click="$emit('edit-user', u)">
              Editar
            </button>
            <button 
              class="btn btn-danger btn-sm" 
              @click="confirmDelete(u)"
              :disabled="u.isSuperAdmin || u.id === currentUser?.id"
            >
              Baja
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.users-view-wrapper {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* Header */
.view-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
}

.view-title {
  font-size: 1.8rem;
  font-weight: 800;
  color: #ffffff;
  letter-spacing: -0.02em;
  margin-bottom: 4px;
}

.view-description {
  font-size: 0.92rem;
  color: var(--text-muted);
  max-width: 650px;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.refresh-icon.spinning {
  display: inline-block;
  animation: spin 0.8s linear infinite;
}

/* Metrics Grid */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
}

.metric-card {
  background: var(--brand-card);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  padding: 16px 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: all var(--transition-fast);
}

.metric-card:hover {
  border-color: var(--brand-border-strong);
  transform: translateY(-2px);
  box-shadow: var(--shadow-card);
}

.metric-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
}

.metric-icon.green {
  background: rgba(34, 197, 94, 0.15);
  color: var(--brand-green-light);
}

.metric-icon.blue {
  background: rgba(14, 165, 233, 0.15);
  color: #38bdf8;
}

.metric-icon.purple {
  background: rgba(168, 85, 247, 0.15);
  color: #c084fc;
}

.metric-icon.emerald {
  background: rgba(16, 185, 129, 0.15);
  color: #34d399;
}

.metric-info {
  display: flex;
  flex-direction: column;
}

.metric-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.metric-value {
  font-size: 1.6rem;
  font-weight: 800;
  font-family: var(--font-heading);
  color: #ffffff;
  line-height: 1.1;
}

/* Filters Bar */
.filters-bar {
  background: rgba(18, 25, 20, 0.7);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  padding: 12px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 14px;
}

.search-box {
  position: relative;
  flex: 1;
  min-width: 260px;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 12px;
  font-size: 0.9rem;
  opacity: 0.6;
}

.search-input {
  width: 100%;
  padding: 9px 36px 9px 36px;
  background: rgba(12, 17, 14, 0.9);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  color: #ffffff;
  font-size: 0.88rem;
}

.search-input:focus {
  outline: none;
  border-color: var(--brand-green-light);
  box-shadow: 0 0 0 2px rgba(34, 197, 94, 0.2);
}

.clear-search {
  position: absolute;
  right: 12px;
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 1.2rem;
}

.role-pills {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.role-pill {
  padding: 6px 14px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--brand-border);
  color: var(--text-muted);
  border-radius: var(--radius-full);
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.role-pill:hover {
  background: rgba(34, 197, 94, 0.1);
  color: #ffffff;
}

.role-pill.active {
  background: var(--brand-green);
  border-color: var(--brand-green-light);
  color: #ffffff;
  box-shadow: 0 2px 10px rgba(34, 197, 94, 0.35);
}

.active-toggle {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 0.85rem;
  color: var(--text-muted);
  cursor: pointer;
  user-select: none;
}

.custom-checkbox {
  accent-color: var(--brand-green-light);
  width: 16px;
  height: 16px;
  cursor: pointer;
}

/* Loading & Empty States */
.loading-state, .empty-state {
  background: var(--brand-card);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  padding: 48px 24px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
}

.spinner-large {
  width: 36px;
  height: 36px;
  border: 3px solid rgba(255, 255, 255, 0.2);
  border-top-color: var(--brand-green-light);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.empty-icon {
  font-size: 3rem;
  opacity: 0.6;
}

.empty-state h3 {
  font-size: 1.2rem;
  color: #ffffff;
}

.empty-state p {
  color: var(--text-muted);
  font-size: 0.9rem;
  max-width: 400px;
}

/* Tabla Desktop */
.users-table-container {
  background: var(--brand-card);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  overflow: hidden;
  box-shadow: var(--shadow-card);
}

.users-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
  font-size: 0.9rem;
}

.users-table th {
  background: rgba(10, 15, 12, 0.85);
  padding: 14px 18px;
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-bottom: 1px solid var(--brand-border);
}

.users-table td {
  padding: 14px 18px;
  border-bottom: 1px solid rgba(34, 197, 94, 0.08);
  vertical-align: middle;
}

.users-table tbody tr {
  transition: background var(--transition-fast);
}

.users-table tbody tr:hover {
  background: rgba(34, 197, 94, 0.04);
}

.row-inactive {
  opacity: 0.55;
}

/* User cell */
.user-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-avatar-badge {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: rgba(34, 197, 94, 0.12);
  border: 1px solid var(--brand-border);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.2rem;
  flex-shrink: 0;
}

.user-cell-meta {
  display: flex;
  flex-direction: column;
}

.user-full-name {
  font-weight: 700;
  color: #ffffff;
}

.user-code-tag {
  font-size: 0.75rem;
  color: var(--brand-green-light);
  font-family: monospace;
  font-weight: 600;
}

/* Role badges */
.badge-role {
  display: inline-block;
  padding: 4px 10px;
  border-radius: var(--radius-full);
  font-size: 0.76rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.badge-role.chofer {
  background: rgba(34, 197, 94, 0.15);
  color: #4ade80;
  border: 1px solid rgba(34, 197, 94, 0.3);
}

.badge-role.administrador {
  background: rgba(14, 165, 233, 0.15);
  color: #38bdf8;
  border: 1px solid rgba(14, 165, 233, 0.3);
}

.badge-role.superadmin {
  background: rgba(234, 179, 8, 0.15);
  color: #facc15;
  border: 1px solid rgba(234, 179, 8, 0.3);
}

.contact-cell {
  display: flex;
  flex-direction: column;
}

.contact-email {
  color: #ffffff;
  font-size: 0.88rem;
}

.contact-phone {
  color: var(--text-dim);
  font-size: 0.78rem;
}

.dept-text {
  color: var(--text-muted);
}

.commission-badge {
  display: inline-block;
  padding: 3px 8px;
  border-radius: var(--radius-sm);
  background: rgba(34, 197, 94, 0.1);
  color: var(--brand-green-light);
  font-weight: 700;
  font-size: 0.85rem;
}

/* Status Pill */
.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: var(--radius-full);
  font-size: 0.78rem;
  font-weight: 600;
}

.status-pill .dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.status-pill.active {
  background: rgba(16, 185, 129, 0.15);
  color: #10b981;
}
.status-pill.active .dot {
  background: #10b981;
}

.status-pill.inactive {
  background: rgba(239, 68, 68, 0.15);
  color: #ef4444;
}
.status-pill.inactive .dot {
  background: #ef4444;
}

/* Acciones */
.text-right {
  text-align: right;
}

.actions-group {
  display: inline-flex;
  gap: 8px;
}

.action-btn {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-sm);
  width: 32px;
  height: 32px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 0.9rem;
  transition: all var(--transition-fast);
}

.action-btn:hover:not(:disabled) {
  background: var(--brand-card-hover);
  border-color: var(--brand-green-light);
  transform: translateY(-1px);
}

.action-btn:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

/* Móvil Card View */
.users-mobile-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.user-mobile-card {
  background: var(--brand-card);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.card-body {
  display: flex;
  flex-direction: column;
  gap: 6px;
  background: rgba(0, 0, 0, 0.2);
  padding: 10px 12px;
  border-radius: var(--radius-sm);
}

.info-row {
  display: flex;
  justify-content: space-between;
  font-size: 0.82rem;
}

.info-label {
  color: var(--text-muted);
}

.info-val {
  color: #ffffff;
  font-weight: 500;
}

.info-val.highlight {
  color: var(--brand-green-light);
  font-weight: 700;
}

.card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.btn-sm {
  padding: 6px 12px;
  font-size: 0.8rem;
}

.btn-danger {
  background: rgba(239, 68, 68, 0.2);
  color: #fca5a5;
  border: 1px solid rgba(239, 68, 68, 0.4);
}

.btn-danger:hover:not(:disabled) {
  background: #ef4444;
  color: #ffffff;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

@media (max-width: 768px) {
  .desktop-only {
    display: none !important;
  }
}
@media (min-width: 769px) {
  .mobile-only {
    display: none !important;
  }
}
</style>
