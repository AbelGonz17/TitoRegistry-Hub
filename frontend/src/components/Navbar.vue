<script setup>
import { ref } from 'vue';

defineProps({
  employees: {
    type: Array,
    default: () => []
  },
  currentEmployeeId: [Number, String],
  backendOnline: Boolean,
  currentTab: String,
  currentUser: {
    type: Object,
    default: null
  }
});

defineEmits(['change-employee', 'change-tab', 'open-new-service', 'open-auth', 'logout']);

const showUserMenu = ref(false);
</script>

<template>
  <header class="navbar-wrapper">
    <div class="navbar-container">
      <!-- Logo y Branding Transporte Tito -->
      <div class="navbar-brand">
        <img src="/logo.png" alt="Transporte Tito" class="brand-logo" />
        <div class="brand-text">
          <span class="brand-title">Transporte Tito</span>
          <span class="brand-slogan">"Servirle es nuestra prioridad"</span>
        </div>
      </div>

      <!-- Navegación central (visible en Desktop) -->
      <nav class="navbar-nav desktop-only">
        <button 
          :class="['nav-link', { active: currentTab === 'dashboard' }]"
          @click="$emit('change-tab', 'dashboard')"
        >
          <span class="nav-icon">📊</span>
          <span>Panel de Viajes</span>
        </button>
        <button 
          :class="['nav-link', { active: currentTab === 'catalog' }]"
          @click="$emit('change-tab', 'catalog')"
        >
          <span class="nav-icon">📋</span>
          <span>Catálogo de Rutas</span>
        </button>
      </nav>

      <!-- Selector de Chofer y Acciones -->
      <div class="navbar-actions">
        <!-- Indicador de conexión con el Servidor -->
        <div class="connection-status" :title="backendOnline ? 'Servidor .NET Conectado' : 'Sin conexión al servidor'">
          <span :class="['status-dot', { online: backendOnline }]"></span>
          <span class="status-label desktop-only">{{ backendOnline ? 'API Conectada' : 'Offline' }}</span>
        </div>

        <!-- Selector de Chofer Activo -->
        <div class="driver-picker desktop-only">
          <label class="picker-label">Chofer Activo:</label>
          <select 
            class="picker-select" 
            :value="currentEmployeeId" 
            @change="$emit('change-employee', Number($event.target.value))"
          >
            <option v-for="emp in employees" :key="emp.id" :value="emp.id">
              {{ emp.fullName }} ({{ emp.code }} - {{ emp.commissionRate }}%)
            </option>
          </select>
        </div>

        <!-- Botón de acción rápida: Registrar Viaje -->
        <button class="btn btn-primary desktop-only" @click="$emit('open-new-service')">
          <span>+</span>
          <span>Registrar Viaje</span>
        </button>

        <!-- Bloque de Autenticación / Perfil de Usuario -->
        <div v-if="currentUser" class="user-profile-wrapper">
          <div class="user-chip" @click="showUserMenu = !showUserMenu">
            <span class="user-avatar">{{ currentUser.avatar || '👤' }}</span>
            <div class="user-info desktop-only">
              <span class="user-name">{{ currentUser.fullName }}</span>
              <span class="user-role">{{ currentUser.role || 'Conductor' }}</span>
            </div>
            <span class="dropdown-chevron">▾</span>
          </div>

          <!-- Menú desplegable del usuario -->
          <div v-if="showUserMenu" class="user-dropdown-menu" @click="showUserMenu = false">
            <div class="dropdown-header">
              <span class="dropdown-user-name">{{ currentUser.fullName }}</span>
              <span class="dropdown-user-email">{{ currentUser.email }}</span>
              <span class="dropdown-badge">{{ currentUser.isSuperAdmin ? 'SUPER ADMIN 👑' : (currentUser.code || 'ACTIVO') }}</span>
            </div>
            <div class="dropdown-divider"></div>
            <button class="dropdown-item logout-item" @click="$emit('logout')">
              <span>🚪</span>
              <span>Cerrar Sesión</span>
            </button>
          </div>
        </div>

        <!-- Botón cuando no ha iniciado sesión -->
        <div v-else class="auth-cta-group">
          <button class="btn btn-primary" @click="$emit('open-auth', 'login')">
            <span class="auth-icon">🔑</span>
            <span>Ingresar</span>
          </button>
        </div>
      </div>
    </div>
  </header>
</template>

<style scoped>
.navbar-wrapper {
  background: rgba(18, 25, 20, 0.95);
  border-bottom: 1px solid var(--brand-border);
  backdrop-filter: blur(16px);
  position: sticky;
  top: 0;
  z-index: 500;
}

.navbar-container {
  max-width: 1300px;
  margin: 0 auto;
  padding: 10px 18px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.navbar-brand {
  display: flex;
  align-items: center;
  gap: 12px;
}

.brand-logo {
  height: 46px;
  width: auto;
  object-fit: contain;
  filter: drop-shadow(0 2px 6px rgba(0, 0, 0, 0.5));
}

.brand-text {
  display: flex;
  flex-direction: column;
}

.brand-title {
  font-family: var(--font-heading);
  font-weight: 800;
  font-size: 1.15rem;
  color: #ffffff;
  line-height: 1.1;
}

.brand-slogan {
  font-size: 0.75rem;
  color: var(--text-accent);
  font-style: italic;
  font-weight: 500;
}

.navbar-nav {
  display: flex;
  align-items: center;
  gap: 8px;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 8px;
  background: transparent;
  border: 1px solid transparent;
  color: var(--text-muted);
  font-family: var(--font-heading);
  font-weight: 600;
  padding: 8px 16px;
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.nav-link:hover {
  color: #ffffff;
  background: rgba(34, 197, 94, 0.08);
}

.nav-link.active {
  color: #ffffff;
  background: var(--brand-card);
  border-color: var(--brand-border-strong);
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
}

.navbar-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.connection-status {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 5px 10px;
  background: rgba(0, 0, 0, 0.35);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-full);
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #ef4444;
  box-shadow: 0 0 8px #ef4444;
}

.status-dot.online {
  background: #22c55e;
  box-shadow: 0 0 8px #22c55e;
}

.status-label {
  font-size: 0.75rem;
  color: var(--text-muted);
}

.driver-picker {
  display: flex;
  align-items: center;
  gap: 8px;
}

.picker-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-muted);
}

.picker-select {
  background: var(--brand-card);
  border: 1px solid var(--brand-border-strong);
  color: #ffffff;
  font-family: var(--font-body);
  font-weight: 600;
  font-size: 0.85rem;
  padding: 8px 12px;
  border-radius: var(--radius-md);
  outline: none;
  cursor: pointer;
}

.picker-select:focus {
  border-color: var(--brand-green-light);
}

/* Perfil de Usuario */
.user-profile-wrapper {
  position: relative;
}

.user-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 5px 12px 5px 6px;
  background: rgba(34, 197, 94, 0.12);
  border: 1px solid var(--brand-border-strong);
  border-radius: var(--radius-full);
  cursor: pointer;
  transition: all var(--transition-fast);
  user-select: none;
}

.user-chip:hover {
  background: rgba(34, 197, 94, 0.22);
  border-color: var(--brand-green-light);
  box-shadow: 0 0 12px rgba(34, 197, 94, 0.25);
}

.user-avatar {
  font-size: 1.25rem;
  line-height: 1;
}

.user-info {
  display: flex;
  flex-direction: column;
  line-height: 1.1;
  text-align: left;
}

.user-name {
  font-size: 0.85rem;
  font-weight: 700;
  color: #ffffff;
}

.user-role {
  font-size: 0.7rem;
  color: var(--brand-green-light);
  font-weight: 500;
}

.dropdown-chevron {
  font-size: 0.75rem;
  color: var(--text-muted);
}

/* Menú flotante del usuario */
.user-dropdown-menu {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  width: 230px;
  background: rgba(18, 26, 21, 0.98);
  border: 1px solid var(--brand-border-strong);
  box-shadow: 0 12px 35px rgba(0, 0, 0, 0.6);
  border-radius: var(--radius-md);
  padding: 12px;
  backdrop-filter: blur(16px);
  z-index: 600;
  display: flex;
  flex-direction: column;
  gap: 6px;
  animation: dropFade 0.2s ease;
}

@keyframes dropFade {
  from { opacity: 0; transform: translateY(-6px); }
  to { opacity: 1; transform: translateY(0); }
}

.dropdown-header {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding-bottom: 8px;
}

.dropdown-user-name {
  font-weight: 700;
  font-size: 0.92rem;
  color: #ffffff;
}

.dropdown-user-email {
  font-size: 0.76rem;
  color: var(--text-muted);
  word-break: break-all;
}

.dropdown-badge {
  display: inline-block;
  align-self: flex-start;
  margin-top: 4px;
  background: rgba(34, 197, 94, 0.15);
  color: var(--brand-green-light);
  font-size: 0.72rem;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: var(--radius-full);
}

.dropdown-divider {
  height: 1px;
  background: var(--brand-border);
  margin: 4px 0;
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 8px 10px;
  border: none;
  background: transparent;
  color: var(--text-main);
  font-size: 0.85rem;
  font-family: inherit;
  font-weight: 500;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
  text-align: left;
}

.dropdown-item:hover {
  background: rgba(34, 197, 94, 0.15);
  color: #ffffff;
}

.logout-item {
  color: #fca5a5;
}

.logout-item:hover {
  background: rgba(239, 68, 68, 0.2);
  color: #ef4444;
}

/* Botones de Auth para cuando no hay sesión */
.auth-cta-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-outline-auth {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 14px;
  background: rgba(34, 197, 94, 0.1);
  border: 1px solid var(--brand-border-strong);
  color: var(--brand-green-light);
  border-radius: var(--radius-md);
  font-weight: 600;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.btn-outline-auth:hover {
  background: var(--brand-green);
  color: #ffffff;
  box-shadow: 0 0 14px rgba(34, 197, 94, 0.4);
}

.btn-register-auth {
  padding: 7px 14px;
  background: linear-gradient(135deg, var(--brand-green), var(--brand-green-hover));
  border: 1px solid rgba(255, 255, 255, 0.2);
  color: #ffffff;
  border-radius: var(--radius-md);
  font-weight: 700;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.btn-register-auth:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 15px rgba(34, 197, 94, 0.45);
}

@media (max-width: 768px) {
  .desktop-only {
    display: none !important;
  }
  .brand-logo {
    height: 38px;
  }
  .brand-title {
    font-size: 1rem;
  }
  .brand-slogan {
    display: none;
  }
}
</style>
