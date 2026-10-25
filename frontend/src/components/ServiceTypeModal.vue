<script setup lang="ts">
import { ref, watch } from 'vue';
import type { ServiceType } from '../types';

const props = defineProps<{
  show: boolean;
  serviceType?: ServiceType | null;
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'submit', payload: Partial<ServiceType>): void;
}>();

const formData = ref({
  name: '',
  category: 'Viajes Interurbanos',
  defaultPrice: '',
  description: ''
});

const errorMessage = ref('');

watch(() => props.show, (newVal) => {
  if (newVal) {
    errorMessage.value = '';
    if (props.serviceType) {
      formData.value = {
        name: props.serviceType.name,
        category: props.serviceType.category,
        defaultPrice: props.serviceType.defaultPrice,
        description: props.serviceType.description || ''
      };
    } else {
      formData.value = {
        name: '',
        category: 'Viajes Interurbanos',
        defaultPrice: '',
        description: ''
      };
    }
  }
});

const handleSubmit = () => {
  errorMessage.value = '';
  if (!formData.value.name.trim()) {
    errorMessage.value = 'El nombre de la ruta o servicio es obligatorio.';
    return;
  }
  const price = parseFloat(formData.value.defaultPrice);
  if (isNaN(price) || price < 0) {
    errorMessage.value = 'El precio base debe ser un número igual o mayor a 0.';
    return;
  }

  emit('submit', {
    name: formData.value.name.trim(),
    category: formData.value.category.trim(),
    defaultPrice: price,
    description: formData.value.description.trim()
  });
};
</script>

<template>
  <div v-if="show" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <div class="modal-header">
        <h2 class="modal-title">
          {{ serviceType ? 'Editar Servicio del Catálogo' : 'Nuevo Servicio / Ruta de Transporte' }}
        </h2>
        <button class="btn-close" @click="$emit('close')">&times;</button>
      </div>

      <form @submit.prevent="handleSubmit" class="modal-body">
        <div v-if="errorMessage" class="alert-error">
          <span>⚠️</span>
          <span>{{ errorMessage }}</span>
        </div>

        <div class="form-group">
          <label class="form-label">Nombre del Servicio o Ruta *</label>
          <input 
            v-model="formData.name"
            type="text" 
            class="form-control" 
            placeholder="Ej: Traslado Ejecutivo Santo Domingo - Santiago" 
            required
          />
        </div>

        <div class="form-row">
          <div class="form-group flex-1">
            <label class="form-label">Categoría *</label>
            <input 
              v-model="formData.category"
              type="text" 
              class="form-control" 
              placeholder="Ej: Viajes Interurbanos, Carga, Aeropuerto" 
              required
            />
          </div>
          <div class="form-group flex-1">
            <label class="form-label">Tarifa Base Sugerida ($) *</label>
            <input 
              v-model="formData.defaultPrice"
              type="number" 
              step="0.01" 
              min="0" 
              class="form-control" 
              placeholder="0.00" 
              required
            />
          </div>
        </div>

        <div class="form-group">
          <label class="form-label">Descripción del Servicio</label>
          <textarea 
            v-model="formData.description"
            class="form-control" 
            rows="3" 
            placeholder="Detalles sobre el tipo de vehículo, paradas, kilometraje estimado, etc."
          ></textarea>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" @click="$emit('close')">
            Cancelar
          </button>
          <button type="submit" class="btn btn-primary" :disabled="loading">
            <span v-if="loading">Guardando...</span>
            <span v-else>{{ serviceType ? 'Guardar Cambios' : 'Crear en el Catálogo' }}</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.modal-header {
  padding: 20px 24px;
  border-bottom: 1px solid var(--brand-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.modal-title {
  font-size: 1.2rem;
  color: #ffffff;
}

.btn-close {
  background: transparent;
  border: none;
  font-size: 1.6rem;
  color: var(--text-muted);
  cursor: pointer;
}

.modal-body {
  padding: 20px 24px;
}

.alert-error {
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid #ef4444;
  color: #fca5a5;
  padding: 10px 14px;
  border-radius: var(--radius-md);
  margin-bottom: 16px;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  gap: 8px;
}

.form-row {
  display: flex;
  gap: 14px;
}

.flex-1 { flex: 1; }

.modal-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px solid var(--brand-border);
}

@media (max-width: 600px) {
  .form-row {
    flex-direction: column;
    gap: 0;
  }
}
</style>
