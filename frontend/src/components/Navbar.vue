<script setup>
defineProps({
  employees: {
    type: Array,
    default: () => []
  },
  currentEmployeeId: [Number, String],
  backendOnline: Boolean,
  currentTab: String
});

defineEmits(['change-employee', 'change-tab', 'open-new-service']);
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
        <!-- Indicador de conexión -->
        <div class="connection-status" :title="backendOnline ? 'Servidor Conectado' : 'Sin conexión al servidor'">
          <span :class="['status-dot', { online: backendOnline }]"></span>
          <span class="status-label desktop-only">{{ backendOnline ? 'API Conectada' : 'Desconectada' }}</span>
        </div>

        <!-- Selector de Chofer Activo -->
        <div class="driver-picker">
          <label class="picker-label desktop-only">Chofer Activo:</label>
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

        <!-- Botón de acción rápida (Desktop) -->
        <button class="btn btn-primary desktop-only" @click="$emit('open-new-service')">
          <span>+</span>
          <span>Registrar Viaje</span>
        </button>
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
  height: 48px;
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
  padding: 4px 8px;
  background: rgba(0, 0, 0, 0.3);
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
  .picker-select {
    max-width: 170px;
    font-size: 0.8rem;
    padding: 6px 8px;
  }
}
</style>
