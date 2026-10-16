<script setup>
import { ref } from 'vue';

defineProps({
  serviceTypes: {
    type: Array,
    default: () => []
  },
  loading: Boolean
});

const emit = defineEmits(['open-create-modal', 'edit-service-type', 'delete-service-type']);

const confirmDeleteId = ref(null);

const requestDelete = (id) => {
  confirmDeleteId.value = id;
};

const cancelDelete = () => {
  confirmDeleteId.value = null;
};

const executeDelete = (id) => {
  emit('delete-service-type', id);
  confirmDeleteId.value = null;
};
</script>

<template>
  <div class="catalog-view">
    <!-- Encabezado de Gestión de Rutas -->
    <div class="catalog-header glass-card">
      <div class="header-text">
        <div class="badge-tag">📋 Gestión de Transporte</div>
        <h1 class="header-title">Catálogo de Servicios y Rutas</h1>
        <p class="header-desc">
          Administra las tarifas base y tipos de servicios que los choferes pueden registrar.
          Las eliminaciones son <strong>Soft Delete</strong> (borrado lógico) para proteger los acumulados históricos de los choferes.
        </p>
      </div>
      <button class="btn btn-primary" @click="$emit('open-create-modal')">
        <span>+</span>
        <span>Crear Nueva Ruta</span>
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="state-container glass-card">
      <div class="spinner"></div>
      <p>Cargando catálogo de servicios...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="serviceTypes.length === 0" class="state-container glass-card empty">
      <div class="empty-icon">📋</div>
      <h3>El catálogo está vacío</h3>
      <p>Aún no has registrado rutas o tipos de servicios. Crea el primer servicio para que los choferes puedan registrar sus viajes.</p>
      <button class="btn btn-primary" @click="$emit('open-create-modal')">
        + Crear Primer Tipo de Servicio
      </button>
    </div>

    <!-- Lista / Grid de Tipos de Servicio -->
    <div v-else class="services-grid">
      <div v-for="st in serviceTypes" :key="st.id" class="service-type-card glass-card">
        <div class="card-header">
          <span class="category-badge">{{ st.category }}</span>
          <div class="price-tag">
            <span class="currency">$</span>
            <span class="price-val">{{ Number(st.defaultPrice).toFixed(2) }}</span>
          </div>
        </div>

        <h3 class="service-title">{{ st.name }}</h3>
        <p class="service-desc">{{ st.description || 'Sin descripción adicional para este servicio.' }}</p>

        <!-- Diálogo de confirmación de Soft Delete en línea -->
        <div v-if="confirmDeleteId === st.id" class="delete-confirmation-box">
          <p class="confirm-text">¿Deseas eliminar lógicamente este servicio del catálogo?</p>
          <div class="confirm-actions">
            <button class="btn btn-secondary btn-sm" @click="cancelDelete">Cancelar</button>
            <button class="btn btn-danger btn-sm" @click="executeDelete(st.id)">Confirmar Borrado</button>
          </div>
        </div>

        <div v-else class="card-footer">
          <button class="btn btn-secondary btn-sm" @click="$emit('edit-service-type', st)">
            <span>✏️</span>
            <span>Editar</span>
          </button>
          <button class="btn btn-danger btn-sm" @click="requestDelete(st.id)">
            <span>🗑️</span>
            <span>Eliminar</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.catalog-view {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.catalog-header {
  padding: 24px 28px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  border-left: 4px solid var(--brand-green);
}

.badge-tag {
  font-size: 0.75rem;
  color: var(--brand-green-light);
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 4px;
}

.header-title {
  font-size: 1.5rem;
  color: #ffffff;
  margin-bottom: 6px;
}

.header-desc {
  font-size: 0.88rem;
  color: var(--text-muted);
  max-width: 720px;
  line-height: 1.4;
}

.services-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 18px;
}

.service-type-card {
  padding: 22px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  border-top: 2px solid rgba(34, 197, 94, 0.4);
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.category-badge {
  background: rgba(34, 197, 94, 0.12);
  color: var(--brand-green-light);
  border: 1px solid rgba(34, 197, 94, 0.3);
  padding: 3px 10px;
  border-radius: var(--radius-full);
  font-size: 0.75rem;
  font-weight: 700;
}

.price-tag {
  display: flex;
  align-items: baseline;
  font-family: var(--font-heading);
  font-weight: 800;
  color: #ffffff;
}

.price-tag .currency {
  font-size: 0.95rem;
  color: var(--brand-green-light);
  margin-right: 2px;
}

.price-tag .price-val {
  font-size: 1.4rem;
}

.service-title {
  font-size: 1.15rem;
  color: #ffffff;
  line-height: 1.3;
}

.service-desc {
  font-size: 0.85rem;
  color: var(--text-muted);
  flex: 1;
  line-height: 1.4;
}

.card-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
  padding-top: 14px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}

.btn-sm {
  padding: 6px 14px;
  font-size: 0.82rem;
  min-height: 36px;
}

.delete-confirmation-box {
  background: rgba(239, 68, 68, 0.12);
  border: 1px solid rgba(239, 68, 68, 0.4);
  border-radius: var(--radius-md);
  padding: 12px;
  margin-top: 8px;
}

.confirm-text {
  font-size: 0.8rem;
  color: #fca5a5;
  margin-bottom: 8px;
}

.confirm-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

/* Empty y Loading */
.state-container {
  padding: 60px 20px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 14px;
}

.empty-icon {
  font-size: 3rem;
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
  .catalog-header {
    flex-direction: column;
    align-items: flex-start;
  }
  .catalog-header .btn {
    width: 100%;
  }
}
</style>
