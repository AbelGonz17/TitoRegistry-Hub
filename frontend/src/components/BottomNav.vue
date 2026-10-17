<script setup>
defineProps({
  currentTab: String,
  currentUser: {
    type: Object,
    default: null
  }
});

defineEmits(['change-tab', 'open-new-service', 'refresh', 'open-auth']);
</script>

<template>
  <div class="bottom-nav-wrapper mobile-only">
    <div class="bottom-nav-container">
      <!-- 1. Panel -->
      <button 
        :class="['bottom-nav-btn', { active: currentTab === 'dashboard' }]"
        @click="$emit('change-tab', 'dashboard')"
      >
        <span class="icon">📊</span>
        <span class="label">Panel</span>
      </button>

      <!-- 2. Catálogo -->
      <button 
        :class="['bottom-nav-btn', { active: currentTab === 'catalog' }]"
        @click="$emit('change-tab', 'catalog')"
      >
        <span class="icon">📋</span>
        <span class="label">Catálogo</span>
      </button>

      <!-- 3. Botón central (+) exactamente en el medio de todos -->
      <button 
        class="bottom-nav-fab"
        @click="$emit('open-new-service')"
        title="Registrar Nuevo Viaje"
      >
        <span class="fab-icon">+</span>
      </button>

      <!-- 4. Personal (Solo Admins y SuperAdmins) -->
      <button 
        v-if="currentUser && (currentUser.isSuperAdmin || currentUser.role === 'Administrador' || currentUser.role === 'SuperAdmin')"
        :class="['bottom-nav-btn', { active: currentTab === 'users' }]"
        @click="$emit('change-tab', 'users')"
      >
        <span class="icon">👥</span>
        <span class="label">Personal</span>
      </button>

      <!-- 5. Perfil / Login en móvil -->
      <button 
        class="bottom-nav-btn"
        @click="$emit('open-auth', currentUser ? 'profile' : 'login')"
        :title="currentUser ? (currentUser.fullName || currentUser.name || currentUser.email || 'Usuario') : 'Iniciar Sesión / Registrarse'"
      >
        <span class="icon">{{ currentUser ? (currentUser.avatar || (currentUser.role === 'Chofer' ? '🚐' : '👑')) : '👤' }}</span>
        <span class="label">{{ currentUser ? (((currentUser.fullName || currentUser.name || currentUser.email || 'Usuario').split(' ')[0])) : 'Ingresar' }}</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.bottom-nav-wrapper {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 900;
  background: rgba(18, 25, 20, 0.96);
  border-top: 1px solid var(--brand-border);
  backdrop-filter: blur(16px);
  padding: 8px 12px calc(8px + env(safe-area-inset-bottom, 0px));
}

.bottom-nav-container {
  display: flex;
  align-items: center;
  justify-content: space-around;
  max-width: 500px;
  margin: 0 auto;
  position: relative;
}

.bottom-nav-btn {
  background: transparent;
  border: none;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 3px;
  color: var(--text-muted);
  cursor: pointer;
  padding: 6px 10px;
  border-radius: var(--radius-md);
  transition: all var(--transition-fast);
  min-width: 54px;
}

.bottom-nav-btn .icon {
  font-size: 1.25rem;
  line-height: 1;
}

.bottom-nav-btn .label {
  font-size: 0.72rem;
  font-weight: 600;
  font-family: var(--font-heading);
}

.bottom-nav-btn.active {
  color: var(--brand-green-light);
}

.bottom-nav-fab {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--brand-green-hover), var(--brand-green));
  color: #ffffff;
  border: 2px solid rgba(255, 255, 255, 0.2);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 18px rgba(34, 197, 94, 0.6);
  cursor: pointer;
  transform: translateY(-14px);
  transition: transform var(--transition-fast), box-shadow var(--transition-fast);
  flex-shrink: 0;
  margin: 0 4px;
}

.bottom-nav-fab:hover {
  box-shadow: 0 6px 22px rgba(34, 197, 94, 0.8);
}

.bottom-nav-fab:active {
  transform: translateY(-10px) scale(0.95);
}

.fab-icon {
  font-size: 2rem;
  font-weight: 300;
  line-height: 1;
  margin-top: -2px;
}

@media (min-width: 769px) {
  .mobile-only {
    display: none !important;
  }
}
</style>
